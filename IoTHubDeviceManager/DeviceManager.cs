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

namespace IoTHubDeviceManager
{
    static class DeviceManager
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
                    case "add":
                        WaitFor(AddAsync(registryManager, config, args));
                        break;
                    case "get":
                        WaitFor(GetAsync(registryManager, config, args));
                        break;
                    case "list":
                        WaitFor(ListAsync(registryManager, config, args));
                        break;
                    case "remove":
                        WaitFor(RemoveAsync(registryManager, config, args));
                        break;
                    case "enable":
                        WaitFor(EnableAsync(registryManager, config, args));
                        break;
                    case "disable":
                        WaitFor(DisableAsync(registryManager, config, args));
                        break;
                    default:
                        NotSupported("Command unknown");
                        break;
                }

                WaitFor(registryManager.CloseAsync());
            }
            catch (Exception ex)
            {
                IoTCommandLine.CommandLine.WriteLine($"{typeof(DeviceManager).Assembly.GetName().Name}.exe (add|get|list|remove|enable|disable)");
                IoTCommandLine.CommandLine.WriteLine($"Error: {ex.Message}");
            }

            // if wait parameter specified, then accept a return key
            var wait = args.ExistArg("wait") && args.BoolArg("wait", true).Value;
            if (wait) ReadLine();
        }

        static async Task AddAsync(RegistryManager registryManager, Config config, string[] args)
        {
            var deviceId = args.StringArg("deviceId");
            var configformat = args.StringArg("configformat");
            var clipboard = args.ClipboardArg();

            var device = await registryManager.AddDeviceAsync(new Device(deviceId));
            IoTCommandLine.CommandLine.WriteLine($"Added: {device.Id}");
            var deviceConfig = DeviceConfig(device, config, configformat);
            if (clipboard) Clipboard(deviceConfig); // add the connection string to the clipboard
            IoTCommandLine.CommandLine.WriteLine(deviceConfig);
        }

        static async Task GetAsync(RegistryManager registryManager, Config config, string[] args)
        {
            var deviceId = args.StringArg("deviceId");
            var configformat = args.StringArg("configformat");
            var clipboard = args.ClipboardArg();

            var device = await registryManager.GetDeviceAsync(deviceId);
            IoTCommandLine.CommandLine.WriteLine($"Get: {deviceId}");
            IoTCommandLine.CommandLine.WriteLine($"Status={device.Status} [{device.StatusReason}]");

            var deviceConfig = DeviceConfig(device, config, configformat);
            if (clipboard) Clipboard(deviceConfig); // add the connection string to the clipboard
            IoTCommandLine.CommandLine.WriteLine(deviceConfig);
        }

        static async Task DisableAsync(RegistryManager registryManager, Config config, string[] args)
        {
            var deviceId = args.StringArg("deviceId");
            var reason = args.StringArg("reason");

            var device = await registryManager.GetDeviceAsync(deviceId);
            IoTCommandLine.CommandLine.WriteLine($"Get: {deviceId}");
            if (device.Status == DeviceStatus.Disabled)
            {
                IoTCommandLine.CommandLine.WriteLine($"{deviceId} already disabled");
            }
            else
            {
                device.Status = DeviceStatus.Disabled;
                device.StatusReason = reason;
                await registryManager.UpdateDeviceAsync(device);
                IoTCommandLine.CommandLine.WriteLine($"{deviceId} disabled");
            }
        }

        static async Task EnableAsync(RegistryManager registryManager, Config config, string[] args)
        {
            var deviceId = args.StringArg("deviceId");
            var reason = args.StringArg("reason");

            var device = await registryManager.GetDeviceAsync(deviceId);
            IoTCommandLine.CommandLine.WriteLine($"Get: {deviceId}");
            if (device.Status == DeviceStatus.Enabled)
            {
                IoTCommandLine.CommandLine.WriteLine($"{deviceId} already enabled");
            }
            else
            {
                device.Status = DeviceStatus.Enabled;
                device.StatusReason = string.Empty;
                await registryManager.UpdateDeviceAsync(device);
                IoTCommandLine.CommandLine.WriteLine($"{deviceId} enabled");
            }
        }

        static async Task ListAsync(RegistryManager registryManager, Config config, string[] args)
        {
            var devices = await registryManager.GetDevicesAsync(1000);
            IoTCommandLine.CommandLine.WriteLine($"List");
            devices.ToList().ForEach(xx => { IoTCommandLine.CommandLine.WriteLine($"{xx.GenerationId} {xx.Id.PadRight(16)} {xx.Authentication.SymmetricKey.PrimaryKey}"); });
        }

        static async Task RemoveAsync(RegistryManager registryManager, Config config, string[] args)
        {
            var deviceId = args.StringArg("deviceId");

            var device = await registryManager.GetDeviceAsync(deviceId);
            await registryManager.RemoveDeviceAsync(deviceId);
            IoTCommandLine.CommandLine.WriteLine($"Removed: {deviceId}");
        }

        /// <summary>
        /// Build the Device connection string
        /// </summary>
        /// <param name="device">The device object</param>
        /// <returns></returns>
        static string DeviceConnectionString(Device device, Config config)
        {
            return $"HostName={config.HostName};DeviceId={device.Id};SharedAccessKey={device.Authentication.SymmetricKey.PrimaryKey}";
        }

        /// <summary>
        /// Build the Device connection string
        /// </summary>
        /// <param name="device">The device object</param>
        /// <returns></returns>
        static string DeviceConfig(Device device, Config config, string configformat = "view")
        {
            switch (configformat)
            {
                case "commandline":
                    return $"-hostName{config.HostName} -deviceId{device.Id} -sharedAccessKey{device.Authentication.SymmetricKey.PrimaryKey}";
                case "appsettings":
                    return $"<add key=\"HostName\" value=\"{config.HostName}\"/>\r\n<add key=\"DeviceId\" value=\"{device.Id}\"/>\r\n<add key=\"SharedAccessKey\" value=\"{device.Authentication.SymmetricKey.PrimaryKey}\"/>";
                case "view":
                default:
                    return $"HostName={config.HostName}\r\nDeviceId={device.Id}\r\nSharedAccessKey={device.Authentication.SymmetricKey.PrimaryKey}";
            }
        }
    }
}
