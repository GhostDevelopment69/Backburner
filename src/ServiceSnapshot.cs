using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceProcess;
using System.Text.Json;
using System.Management;

namespace Backburner
{
    public class ServiceState
    {
        public string ServiceName { get; set; } = "";
        public string OriginalStartMode { get; set; } = ""; // e.g. "Automatic", "Manual"
        public string OriginalStatus { get; set; } = "";     // e.g. "Running", "Stopped"
    }

    public static class ServiceSnapshot
    {
        private static readonly string SnapshotPath =
            Path.Combine(AppContext.BaseDirectory, "snapshot.json");

        public static List<ServiceState> Capture(List<string> serviceNames)
        {
            var states = new List<ServiceState>();

            foreach (var name in serviceNames)
            {
                try
                {
                    using var sc = new ServiceController(name);
                    states.Add(new ServiceState
                    {
                        ServiceName = name,
                        OriginalStatus = sc.Status.ToString(),
                        OriginalStartMode = GetStartMode(name)
                    });
                }
                catch (Exception ex)
                {
                    // Service might not exist on this machine — skip it, don't crash
                    Console.WriteLine($"Could not snapshot {name}: {ex.Message}");
                }
            }

            var json = JsonSerializer.Serialize(states, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SnapshotPath, json);

            return states;
        }

        public static List<ServiceState>? LoadLastSnapshot()
        {
            if (!File.Exists(SnapshotPath)) return null;
            var json = File.ReadAllText(SnapshotPath);
            return JsonSerializer.Deserialize<List<ServiceState>>(json);
        }

        private static string GetStartMode(string serviceName)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    $"SELECT StartMode FROM Win32_Service WHERE Name = '{serviceName}'");

                foreach (ManagementObject service in searcher.Get())
                {
                    return service["StartMode"]?.ToString() ?? "Unknown";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not get start mode for {serviceName}: {ex.Message}");
            }

            return "Unknown";
        }
    }
}