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
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

using static IoTCommandLine.CommandLine;

namespace IoTHubDeviceConfiguration
{
    static class DeviceConfiguration
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
                config.HostName = args.StringArg("hostName", AppSetting("HostName"));
                config.SharedAccessKeyName = args.StringArg("sharedAccessKeyName", AppSetting("SharedAccessKeyName"));
                config.SharedAccessKey = args.StringArg("sharedAccessKey", AppSetting("SharedAccessKey"));

                // create the client object
                var registryManager = RegistryManager.CreateFromConnectionString($"HostName={config.HostName};SharedAccessKeyName={config.SharedAccessKeyName};SharedAccessKey={config.SharedAccessKey}");

                // run the command
                switch (commandName)
                {
                    case "gettwin":
                        WaitFor(GetTwinAsync(registryManager, config, args));
                        break;
                   case "setdesiredproperty":
                        WaitFor(SetDesiredPropertyAsync(registryManager, config, args));
                        break;
                    case "tag":
                        WaitFor(Tag(registryManager, config, args));
                        break;
                    case "querytwin":
                        WaitFor(QueryTwinAsync(registryManager, config, args));
                        break;
                    default:
                        NotSupported("Command unknown");
                        break;
                }

                WaitFor(registryManager.CloseAsync());
            }
            catch (Exception ex)
            {
                IoTCommandLine.CommandLine.WriteLine($"{typeof(DeviceConfiguration).Assembly.GetName().Name}.exe (gettwin|desiredproperty|tag|querytwin)");
                IoTCommandLine.CommandLine.WriteLine($"Error: {ex.Message}");
            }

            // if wait parameter specified, then accept a return key
            var wait = args.ExistArg("wait") && args.BoolArg("wait", true).Value;
            if (wait) ReadLine();
        }

        static async Task GetTwinAsync(RegistryManager registryManager, Config config, string[] args)
        {
            var deviceId = args.StringArg("deviceId");
            var clipboard = args.ClipboardArg();

            IoTCommandLine.CommandLine.WriteLine($"Getting twin of {deviceId}...");

            var twin = await registryManager.GetTwinAsync(deviceId);

            WriteLineTwin(twin);
        }

        private static void WriteLineTwin(Twin twin)
        {
            WriteLine();
            WriteLine($"Tags version {twin.Tags.Version}");
            foreach (KeyValuePair<string, object> p in twin.Tags)
            {
                WriteLine($"{p.Key}={p.Value}");
            }

            WriteLine();
            WriteLine($"Desired properties version {twin.Properties.Desired.Version}");
            foreach (KeyValuePair<string, object> p in twin.Properties.Desired)
            {
                WriteLine($"{p.Key}={p.Value}");
            }

            WriteLine();
            WriteLine($"Reported properties version {twin.Properties.Reported.Version}");
            foreach (KeyValuePair<string, object> p in twin.Properties.Reported)
            {
                WriteLine($"{p.Key}={p.Value}");
            }
        }

        static async Task QueryTwinAsync(RegistryManager registryManager, Config config, string[] args)
        {
            WriteLine($"Querying twins on {config.HostName}");

            var queryName = args.StringArg("queryName", "all").ToLower();
            switch (queryName)
            {
                case "all":
                    await QueryTwinAsync(registryManager, config, $"SELECT * FROM devices");
                    return;
                case "bylocation":
                    var location = args.StringArg("location");
                    await QueryTwinAsync(registryManager, config, $"SELECT * FROM devices WHERE tags.location = '{location}'");
                    return;
                default:
                    break;
            }
        }

        static async Task QueryTwinAsync(RegistryManager registryManager, Config config, string querySql)
        {
            var query = registryManager.CreateQuery(querySql);
            var twins = await query.GetNextAsTwinAsync();
            foreach (var twin in twins)
            {
                WriteLine($"DeviceId {twin.DeviceId}");
                WriteLineTwin(twin);
            }
        }

        static async Task SetDesiredPropertyAsync(RegistryManager registryManager, Config config, string[] args)
        {
            var deviceId = args.StringArg("deviceId");
            var propertyName = args.StringArg("propertyName");
            var propertyValue = args.StringArg("propertyValue");

            Write($"Desired Property {propertyName}={propertyValue}...");

            var twinPatch = new Twin();
            twinPatch.Properties.Desired[propertyName] = propertyValue;
            var updated = await registryManager.UpdateTwinAsync(deviceId, twinPatch, "*");
        }

        static async Task Tag(RegistryManager registryManager, Config config, string[] args)
        {
            var deviceId = args.StringArg("deviceId");
            var tagName = args.StringArg("tagName");
            var tagValue = args.StringArg("tagValue");

            Write($"Tag {tagName}={tagValue}...");

            var twinPatch = new Twin();
            twinPatch.Tags[tagName] = tagValue;
            var updated = await registryManager.UpdateTwinAsync(deviceId, twinPatch, "*");
        }
    }
}
