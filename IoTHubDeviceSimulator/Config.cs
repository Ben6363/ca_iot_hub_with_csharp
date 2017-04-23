using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTHubDeviceSimulator
{
    public class Config
    {

        /// <summary>
        /// the IoTHub URL
        /// </summary>
        public string HostName { get; set; }
        /// <summary>
        /// the authorization to IoTHub
        /// </summary>
        public string SharedAccessKey { get; set; }
        /// <summary>
        /// The current DeviceId from ConnectionString
        /// </summary>
        public string DeviceId { get; set; }
        /// <summary>
        /// tagging info
        /// </summary>
        public string[] Tags { get; set; }
        /// <summary>
        /// The Transport Type
        /// </summary>
        public TransportType TransportType { get; set; }

        public static Config Read()
        {
            var filename = $"{typeof(Config).Assembly.GetName().Name}.exe.json";
            if (!File.Exists(filename)) return new IoTHubDeviceSimulator.Config();
            var json = File.ReadAllText(filename);
            var config = JsonConvert.DeserializeObject<Config>(json);
            return config;
        }

        public void Write()
        {
            var json = JsonConvert.SerializeObject(this);
            var filename = $"{typeof(Config).Assembly.GetName().Name}.exe.json";
            File.WriteAllText(filename, json);
        }
    }
}
