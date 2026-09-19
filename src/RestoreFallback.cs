using System;
using System.Diagnostics;

namespace Backburner
{
    // Registers a Windows Scheduled Task that restores suspended services on the
    // next logon. This is the safety net for the case Program.cs's own crash
    // recovery can't cover: the app crashes and the user never relaunches it
    // (e.g. they just reboot). The task fires independently of Backburner ever
    // running again, reads the same snapshot.json, and exits without opening
    // any UI. It is armed only while a suspend is active and removed the
    // moment a restore succeeds through the normal app paths.
    public static class RestoreFallback
    {
        private const string TaskName = "BackburnerFallbackRestore";

        public static void RegisterTask()
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath))
            {
                Console.WriteLine("Could not resolve executable path; skipping fallback task registration.");
                return;
            }

            // /rl highest: required so the restore can start/stop services when the
            // task fires — matches the elevation the app itself already requires.
            // /f: overwrite silently if a stale registration already exists.
            RunSchtasks(
                $"/create /tn \"{TaskName}\" /tr \"\\\"{exePath}\\\" --restore-fallback\" " +
                "/sc onlogon /rl highest /f");
        }

        public static void RemoveTask()
        {
            RunSchtasks($"/delete /tn \"{TaskName}\" /f");
        }

        private static void RunSchtasks(string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };

                using var proc = Process.Start(psi);
                proc?.WaitForExit(5000);
            }
            catch (Exception ex)
            {
                // Non-fatal: worst case the fallback net isn't armed for this session,
                // but the existing Program.cs crash-recovery check still covers a relaunch.
                Console.WriteLine($"schtasks {arguments} failed: {ex.Message}");
            }
        }
    }
}
