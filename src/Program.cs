namespace Backburner;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();

        // Set by the Task Scheduler fallback task registered in RestoreFallback — fires on
        // next logon if the app crashed and was never relaunched. Restore-only, no tray UI.
        bool isFallbackRestore = Array.IndexOf(args, "--restore-fallback") >= 0;

        // Crash Watchdog in case if a snapshot file exists from a previous session that was never cleared (e.g., due to a crash, got killed while active, and/or restore never ran).
        var orphanedSnapshot = ServiceSnapshot.LoadLastSnapshot();
        if (orphanedSnapshot != null)
        {
            ServiceSnapshot.Restore(orphanedSnapshot);
            ServiceSnapshot.RestoreStartMode(orphanedSnapshot);
            ServiceSnapshot.ClearSnapshot();
        }
        RestoreFallback.RemoveTask();

        if (isFallbackRestore)
        {
            return;
        }

        Application.Run(new TrayContext());
    }
}