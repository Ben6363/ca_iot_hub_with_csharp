using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static IoTCommandLine.CommandLine;

namespace IoTHubDeviceSimulator
{
    public static class DeviceClientExtension
    {
        public static async Task SendEventAsync(this DeviceClient deviceClient, string eventBody, string type)
        {
            // serialization
            var eventBytes = Encoding.UTF8.GetBytes(eventBody);
            var eventMessage = new Message(eventBytes);
            eventMessage.Properties.Add("Type", type);

            try
            {
                Write($"Sending {eventBody}...");
                await deviceClient.SendEventAsync(eventMessage);
                WriteLine("ok");
            }
            catch (Exception ex)
            {
                WriteLine($"failed [{ex.Message}]");
            }
        }
    }
}
