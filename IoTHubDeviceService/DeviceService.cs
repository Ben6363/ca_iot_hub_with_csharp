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

using Microsoft.Azure.Devices;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using static IoTCommandLine.CommandLine;

namespace IoTHubDeviceService
{
    static class DeviceService
    {
        static void Main(string[] args)
        {
            var config = Config.Read();

            try
            {
                // select commandName from argument list and remove
                var commandName = args[0].ToLower();
                args = args.Skip(1).ToArray();

                // parsing generic args
                config.ConnectionString = args.StringArg("connectionString", AppSetting("connectionString"));
                config.HandleFileUploadNotificationUrl = args.StringArg("handleFileUploadNotificationUrl", AppSetting("handleFileUploadNotificationUrl"));

                // create the service client object object
                var serviceClient = ServiceClient.CreateFromConnectionString(config.ConnectionString);

                WriteLine($"Receiving from {config.ConnectionString}");

                // run the command
                switch (commandName)
                {
                    case "handlefileupload":
                        WaitFor(HandleFileUploadAsync(serviceClient, config, args));
                        break;
                    case "sendmessage":
                        WaitFor(SendMessageAsync(serviceClient, config, args));
                        break;
                    case "directmethod":
                        WaitFor(DirectMethodAsync(serviceClient, config, args));
                        break;
                    case "purgemessagequeue":
                        WaitFor(PurgeMessageQueueAsync(serviceClient, config, args));
                        break;
                    default:
                        NotSupported("Command unknown");
                        break;
                }
            }
            catch (Exception ex)
            {
                WriteLine($"{typeof(DeviceService).Assembly.GetName().Name}.exe telemetry|alerts");
                WriteLine($"Error: {ex.Message}");
            }

            // if wait parameter specified, then accept a return key
            var wait = args.ExistArg("wait") && args.BoolArg("wait", true).Value;
            if (wait) ReadLine();
        }

        static async Task HandleFileUploadAsync(ServiceClient serviceClient, Config config, string[] args)
        {
            var notificationReceiver = serviceClient.GetFileNotificationReceiver();
            WriteLine("\nReceiving file upload notification from service");

            while (true)
            {
                var fileUploadNotification = await notificationReceiver.ReceiveAsync();
                if (fileUploadNotification == null) continue;

                WriteLine($"File from {fileUploadNotification.DeviceId}: {fileUploadNotification.BlobName} [{fileUploadNotification.BlobSizeInBytes/1024}Kb]");

                await notificationReceiver.CompleteAsync(fileUploadNotification);

                // prepare a request to a Function

                //  save notification
                var notification = new
                {
                    Type = "fileupload",
                    BlobName = fileUploadNotification.BlobName,
                    BlobUri = fileUploadNotification.BlobUri,
                    DeviceId = fileUploadNotification.DeviceId,
                    EnqueuedTimeUtc = fileUploadNotification.EnqueuedTimeUtc
                };

                var httpClient = new HttpClient();
                var content = new StringContent(JsonConvert.SerializeObject(notification));
                await httpClient.PostAsync(config.HandleFileUploadNotificationUrl, content);
            }
        }

        static async Task DirectMethodAsync(ServiceClient serviceClient, Config config, string[] args)
        {
            var deviceId = args.StringArg("deviceId");
            var methodName = args.StringArg("methodName");
            var methodJson = args.StringArg("methodJson");
            var clipboard = args.ClipboardArg();

            var method = new CloudToDeviceMethod(methodName.ToLower());
            if (!string.IsNullOrWhiteSpace(methodJson))
            {
                method.SetPayloadJson(methodJson);
            }

            try
            {
                Write($"Invoking {method} on {deviceId}...");
                var result = await serviceClient.InvokeDeviceMethodAsync(deviceId, method);
                var jsonResult = result.GetPayloadAsJson();
                WriteLine($"OK {jsonResult}");
                if (clipboard) Clipboard(jsonResult);
            }
            catch (Exception ex)
            {
                WriteLine($"failed [{ex.Message}]");
            }
        }

        static async Task PurgeMessageQueueAsync(ServiceClient serviceClient, Config config, string[] args)
        {
            var deviceId = args.StringArg("deviceId");

            try
            {
                Write($"Purging messages on {deviceId} queue...");

                var result = await serviceClient.PurgeMessageQueueAsync(deviceId);

                WriteLine($"OK purged {result.TotalMessagesPurged} messages");
            }
            catch (Exception ex)
            {
                WriteLine($"failed [{ex.Message}]");
            }
        }

        static async Task SendMessageAsync(ServiceClient serviceClient, Config config, string[] args)
        {
            var deviceId = args.StringArg("deviceId");
            var messageBody = args.StringArg("messageBody");
            var ack = args.ExistArg("ack") && args.BoolArg("ack", true).Value; ;

            await SendMessageAsync(serviceClient, messageBody, deviceId, ack);
        }

        private static async Task SendMessageAsync(ServiceClient serviceClient, string messageBody, string deviceId, bool ack)
        {
            // serialization
            var messageBytes = Encoding.UTF8.GetBytes(messageBody);
            var message = new Message(messageBytes);

            try
            {
                if (ack)
                {
                    message.Ack = DeliveryAcknowledgement.Full;
                    var correlationId = Guid.NewGuid().ToString();
                    message.CorrelationId = correlationId;

                    Write($"Sending {messageBody} to {deviceId}...");
                    await serviceClient.SendAsync(deviceId, message);

                    while (true)
                    {
                        var feedbackReceiver = serviceClient.GetFeedbackReceiver();
                        var receive = await feedbackReceiver.ReceiveAsync();
                        try
                        {
                            foreach (var record in receive.Records)
                            {
                                WriteLine($"ACK: DeviceID={record.DeviceId} OriginalMessageId={record.OriginalMessageId} StatusCode={record.StatusCode}");
                            }
                            await feedbackReceiver.CompleteAsync(receive);
                        }
                        catch
                        {
                            await feedbackReceiver.AbandonAsync(receive);
                        }
                    }

                    WriteLine("ok");
                }
                else
                {
                    Write($"Sending {messageBody} to {deviceId}...");
                    await serviceClient.SendAsync(deviceId, message);
                    WriteLine("ok");
                }
            }
            catch (Exception ex)
            {
                WriteLine($"failed [{ex.Message}]");
            }
        }
    }
}
