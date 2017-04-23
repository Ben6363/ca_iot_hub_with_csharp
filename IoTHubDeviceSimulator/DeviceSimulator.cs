/*
    MIT License

    Copyright (c) 2017 Marco Parenzan and Cloud Academy

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static IoTCommandLine.CommandLine;

namespace IoTHubDeviceSimulator
{
    static class DeviceSimulator
    {
        static DateTime StartupTime = DateTime.Now;

        [STAThread]
        static void Main(string[] args)
        {
            var config = Config.Read();
            // handle possible CTRL+C key press
            System.Console.CancelKeyPress += (s, e) =>
            {
                WriteLine("Writing config file...");
                config.Write();
            };

            try
            {
                // select commandName from argument list and remove
                var commandName = args[0].ToLower();
                args = args.Skip(1).ToArray();

                // parsing generic args
                config.HostName = args.StringArg("hostName", config.HostName);
                config.DeviceId = args.StringArg("deviceId", config.DeviceId);
                config.SharedAccessKey = args.StringArg("sharedAccessKey", config.SharedAccessKey);
                config.TransportType = args.EnumArg<TransportType>("transporttype", TransportType.Mqtt).Value;

                var authenticationMethod = AuthenticationMethodFactory.CreateAuthenticationWithRegistrySymmetricKey(config.DeviceId, config.SharedAccessKey);
                // create the client object
                var deviceClient = DeviceClient.Create(config.HostName, authenticationMethod, config.TransportType);

                WriteLine($"DeviceId {config.DeviceId}");
                WriteLine($"Shared Access Key {config.SharedAccessKey}");
                WriteLine($"Transport Type {config.TransportType}");

                // run the command
                switch (commandName)
                {
                    case "faketelemetry":
                        WaitFor(FakeTelemetryAsync(deviceClient, config, args));
                        break;
                    case "telemetry":
                        WaitFor(TelemetryAsync(deviceClient, config, args));
                        break;
                    case "fakealert":
                        WaitFor(FakeAlertAsync(deviceClient, config, args));
                        break;
                    case "alert":
                        WaitFor(AlertAsync(deviceClient, config, args));
                        break;
                    case "sendevent":
                        WaitFor(SendEventAsync(deviceClient, config, args));
                        break;
                    case "fileupload":
                        WaitFor(FileUploadAsync(deviceClient, config, args));
                        break;
                    case "handlemessage":
                        WaitFor(HandleMessageAsync(deviceClient, config, args));
                        break;
                    case "handledirectmethod":
                        WaitFor(HandleDirectMethodAsync(deviceClient, config, args));
                        break;
                    case "handleproperties":
                        WaitFor(HandlePropertiesAsync(deviceClient, config, args));
                        break;
                    default:
                        NotSupported("Command unknown");
                        break;
                }
            }
            catch (Exception ex)
            {
                WriteLine($"{typeof(DeviceSimulator).Assembly.GetName().Name}.exe telemetry|alert");
                WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                WriteLine("Writing config file...");
                config.Write();
            }

            // if wait parameter specified, then accept a return key
            var wait = args.ExistArg("wait") && args.BoolArg("wait", true).Value;
            if (wait) ReadLine();
        }

        static async Task FakeTelemetryAsync(DeviceClient deviceClient, Config config, string[] args)
        {
            WriteLine("CTRL+C to interrupt the read operation");
            
            // delay simulation
            var sleepmin = args.IntegerArg("sleepmin", 100);
            var sleepmax = args.IntegerArg("sleepmax", 300);

            // series simulation
            var initial = args.DoubleArg("initial", 25);
            var deltamin = args.DoubleArg("deltamin", -0.01);
            var deltamax = args.DoubleArg("deltamax", 0.01);
            var delta = new Func<double>(() => RandomDouble(deltamin, deltamax));

            var current = initial;
            while (true)
            {
                // introduce delay
                var sleep = RandomInteger(sleepmin, sleepmax);
                WriteLine($"Sleep for {sleep}ms");
                Sleep(sleep);

                await TelemetryAsync(deviceClient, config, args, current);

                // next value
                current += delta();
            }
        }

        static async Task TelemetryAsync(DeviceClient deviceClient, Config config, string[] args, double? current = 0)
        {
            var telemetryType = args.StringArg("telemetryType", "temperature");
            var tags = args.ArrayArg("tags", new string[] { telemetryType });
            current = args.DoubleArg("current", current);

            // send
            var eventObject = new
            {
                DeviceId = config.DeviceId,
                Type = "telemetry",
                TelemetryType = telemetryType,
                Tags = tags,
                Current = current
            };
            var eventJson = JsonConvert.SerializeObject(eventObject);
            await deviceClient.SendEventAsync(eventJson, eventObject.Type);
        }

        static async Task FakeAlertAsync(DeviceClient deviceClient, Config config, string[] args)
        {
            WriteLine("CTRL+C to interrupt the read operation");
            System.Console.CancelKeyPress += (s, e) =>
            {
                WriteLine("Writing config file...");
                config.Write();
            };

            // classify telemetry
            var alertTypes = args.ArrayArg("alertTypes", new string[] { });

            // delay simulation
            var sleepmin = args.IntegerArg("sleepmin", 10000);
            var sleepmax = args.IntegerArg("sleepmax", 30000);

            // series simulation
            var deltamin = args.IntegerArg("deltamin", 0);
            var deltamax = args.IntegerArg("deltamax", 100);
            var next = new Func<int>(() => RandomInteger(deltamin, deltamax));

            while (true)
            {
                // introduce delay
                var sleep = RandomInteger(sleepmin, sleepmax);
                WriteLine($"Sleep for {sleep}ms");
                Sleep(sleep);

                var severity = next();
                var alertType = alertTypes[RandomInteger(0, alertTypes.Length - 1)];

                await AlertAsync(deviceClient, config, args, severity, alertType);
            }
        }

        static async Task AlertAsync(DeviceClient deviceClient, Config config, string[] args, int? severity = null, string alertType = null)
        {
            var tags = args.ArrayArg("tags", new string[] { });

            alertType = args.StringArg("alerttype", alertType);
            severity = args.IntegerArg("severity", severity);

            // send
            var eventObject = new
            {
                DeviceId = config.DeviceId,
                Type = "alert",
                AlertType = alertType,
                Tags = tags,
                Severity = severity
            };
            var eventJson = JsonConvert.SerializeObject(eventObject);
            await deviceClient.SendEventAsync(eventJson, eventObject.Type);
        }

        static async Task SendEventAsync(DeviceClient deviceClient, Config config, string[] args)
        {
            var eventBody = args.StringArg("eventBody");
            var eventType = args.StringArg("eventType");

            await deviceClient.SendEventAsync(eventBody, eventType);
        }

        static async Task FileUploadAsync(DeviceClient deviceClient, Config config, string[] args)
        {
            var uploadType = args.StringArg("uploadType");
            var tags = args.ArrayArg("tags", new string[] { uploadType });

            var filename = args.StringArg("filename");

            try
            {
                Write($"Uploading {filename}...");
                using (var stream = new FileStream(filename, FileMode.Open))
                {
                    var uniqueFilename = $"{Path.GetFileNameWithoutExtension(filename)}-{Guid.NewGuid()}{Path.GetExtension(filename)}".ToLower();
                    await deviceClient.UploadToBlobAsync(uniqueFilename, stream);
                }
                WriteLine("ok");
            }
            catch (Exception ex)
            {
                WriteLine($"failed [{ex.Message}]");
            }
        }

        static async Task HandleMessageAsync(DeviceClient deviceClient, Config config, string[] args)
        {
            WriteLine("CTRL+C to interrupt the read operation");

            while (true)
            {
                var message = await deviceClient.ReceiveAsync();
                try
                {
                    var messageBytes = message.GetBytes();
                    var messageText = Encoding.UTF8.GetString(messageBytes);
                    WriteLine($"Received {messageText}");

                    await deviceClient.CompleteAsync(message);
                }
                catch (Exception ex)
                {
                    await deviceClient.AbandonAsync(message);
                }
            }
        }

        private static int Uptime()
        {
            return (int) (DateTime.Now - StartupTime).TotalSeconds;
        }

        static async Task<MethodResponse> DirectMethodUptimeAsync(MethodRequest request, object context)
        {
            var config = (Config)context;

            var response = new {
                DeviceId = config.DeviceId,
                StartupTime = StartupTime,
                Uptime = Uptime()
            };

            WriteLine($"Invoked Uptime direct method: {response.Uptime}s");

            var responseJson = JsonConvert.SerializeObject(response);
            var responseBytes = Encoding.UTF8.GetBytes(responseJson);

            var methodResponse = new MethodResponse(responseBytes, 0);

            return methodResponse;
        }

        static async Task HandleDirectMethodAsync(DeviceClient deviceClient, Config config, string[] args)
        {
            WriteLine("CTRL+C to interrupt the abort operation");
            System.Console.CancelKeyPress += (s, e) =>
            {
                WaitFor(deviceClient.CloseAsync());
            };

            await deviceClient.OpenAsync();
            await deviceClient.SetMethodHandlerAsync("uptime", DirectMethodUptimeAsync, config);
            await Task.Delay(TimeSpan.FromDays(1));
        }

        static async Task HandlePropertiesAsync(DeviceClient deviceClient, Config config, string[] args)
        {
            WriteLine("CTRL+C to interrupt the read operation");
            System.Console.CancelKeyPress += (s, e) =>
            {
                WaitFor(deviceClient.CloseAsync());
            };

            // delay simulation
            var sleepmin = args.IntegerArg("sleepmin", 600);
            var sleepmax = args.IntegerArg("sleepmax", 3000);

            var reportedCounter = 0;

            await deviceClient.OpenAsync();
            var twin = await deviceClient.GetTwinAsync();
            // update for the first time when device is disconnected
            UpdateDesiredProperties(twin.Properties.Desired, ref sleepmin, ref sleepmax, ref reportedCounter);
            // wait for updated
            await deviceClient.SetDesiredPropertyUpdateCallback((twinCollection, context) =>
            {
                UpdateDesiredProperties(twinCollection, ref sleepmin, ref sleepmax, ref reportedCounter);
                return Task.CompletedTask;
            }, config);

            while (true)
            {
                // introduce delay
                var sleep = RandomInteger(sleepmin, sleepmax);
                WriteLine($"Sleep for {sleep}ms [{sleepmin}-{sleepmax}]");
                Sleep(sleep);

                reportedCounter++;
                WriteLine($"ReportedCounter={reportedCounter}");
                var reportedProperties = new TwinCollection();
                reportedProperties["reportedCounter"] = reportedCounter;
                await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
            }
        }

        private static void UpdateDesiredProperties(TwinCollection twinCollection, ref int? sleepmin, ref int? sleepmax, ref int reportedCounter)
        {
            foreach (KeyValuePair<string, object> twin in twinCollection)
            {
                switch (twin.Key.ToLower())
                {
                    case "sleepmin":
                        sleepmin = int.Parse(twin.Value.ToString());
                        break;
                    case "sleepmax":
                        sleepmax = int.Parse(twin.Value.ToString());
                        break;
                    case "reportedcounter":
                        reportedCounter = int.Parse(twin.Value.ToString());
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
