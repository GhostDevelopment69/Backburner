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

        // Hardcoded engine-level safety net — independent of any profile or user config.
        // A service on this list can never be suspended, no matter what list is passed in.
        private static readonly HashSet<string> Denylist = new(StringComparer.OrdinalIgnoreCase)
        {
            // Core OS / security
            "WinDefend",        // Windows Defender Antivirus Service
            "wscsvc",           // Security Center
            "EventLog",         // Windows Event Log
            "RpcSs",            // Remote Procedure Call (RPC)
            "RpcEptMapper",     // RPC Endpoint Mapper
            "DcomLaunch",       // DCOM Server Process Launcher
            "LSM",              // Local Session Manager
            "Winmgmt",          // Windows Management Instrumentation
            "PlugPlay",         // Plug and Play
            "Power",            // Power service
            "BFE",              // Base Filtering Engine (firewall dependency)
            "MpsSvc",           // Windows Defender Firewall
            "gpsvc",            // Group Policy Client
            "ProfSvc",          // User Profile Service
            "SamSs",            // Security Accounts Manager
            "Schedule",         // Task Scheduler (used for our own crash-recovery fallback)
            // Networking essentials
            "Dnscache",         // DNS Client
            "Dhcp",             // DHCP Client
            "nsi",              // Network Store Interface Service
            "NlaSvc",           // Network Location Awareness
            "netprofm",         // Network List Service
            // Shell / session
            "Themes",           // Themes
            "AudioSrv",         // Windows Audio
            "AudioEndpointBuilder",
            "UserManager",      // User Manager
            "SystemEventsBroker",
        };

        public static bool IsDenied(string serviceName) => Denylist.Contains(serviceName);

        public static List<ServiceState> Capture(List<string> serviceNames)
        {
            var states = new List<ServiceState>();

            foreach (var name in serviceNames)
            {
                if (IsDenied(name))
                {
                    Console.WriteLine($"Skipping {name}: on engine denylist");
                    continue;
                }

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
        // For Crash Watchdog listed in Program.cs
        public static void ClearSnapshot()
        {
            if (File.Exists(SnapshotPath))
                File.Delete(SnapshotPath);
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
        public static void RestoreStartMode(List<ServiceState> snapshot)
        {
            foreach (var state in snapshot)
            {
                if (state.OriginalStartMode == "Unknown") continue;

                try
                {
                    using var searcher = new ManagementObjectSearcher(
                        $"SELECT * FROM Win32_Service WHERE Name = '{state.ServiceName}'");

                    foreach (ManagementObject service in searcher.Get())
                    {
                        var wmiStartMode = state.OriginalStartMode switch
                        {
                            "Auto" => "Automatic",
                            "Manual" => "Manual",
                            "Disabled" => "Disabled",
                            _ => null
                        };

                        if (wmiStartMode != null)
                        {
                            service.InvokeMethod("ChangeStartMode", new object[] { wmiStartMode });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not restore start mode for {state.ServiceName}: {ex.Message}");
                }
            }
        }
        public static void Suspend(List<string> serviceNames)
        {
            foreach (var name in serviceNames)
            {
                if (IsDenied(name))
                {
                    Console.WriteLine($"Refusing to stop {name}: on engine denylist");
                    continue;
                }

                try
                {
                    using var sc = new ServiceController(name);

                    if (sc.Status == ServiceControllerStatus.Running &&
                        sc.CanStop)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not stop {name}: {ex.Message}");
                }
            }
        }
        public static void Restore(List<ServiceState> snapshot)
        {
            foreach (var state in snapshot)
            {
                try
                {
                    using var sc = new ServiceController(state.ServiceName);

                    if (state.OriginalStatus == "Running" &&
                        sc.Status != ServiceControllerStatus.Running)
                    {
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not restore {state.ServiceName}: {ex.Message}");
                }
            }
        }
    }
}