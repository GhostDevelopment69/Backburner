using System;
using System.Drawing;
using System.Windows.Forms;

namespace Backburner
{
    public class TrayContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private bool _isActive = false;

        public TrayContext()
        {
            var menu = new ContextMenuStrip();
            var activateItem = new ToolStripMenuItem("Activate", null, OnActivateClicked);
            var exitItem = new ToolStripMenuItem("Exit", null, OnExitClicked);

            menu.Items.Add(activateItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exitItem);

            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application, // placeholder icon for now
                ContextMenuStrip = menu,
                Visible = true,
                Text = "Backburner"
            };

            _activateItem = activateItem;
        }

        private readonly ToolStripMenuItem _activateItem;

        private List<ServiceState>? _lastSnapshot;

        private void OnActivateClicked(object? sender, EventArgs e)
        {
            _isActive = !_isActive;
            _activateItem.Text = _isActive ? "Deactivate" : "Activate";
            _trayIcon.Text = _isActive ? "Backburner (Active)" : "Backburner";

            var testServices = new List<string> { "Spooler" };
            
            if (_isActive)
            {
                _lastSnapshot = ServiceSnapshot.Capture(testServices);
                ServiceSnapshot.Suspend(testServices);
                RestoreFallback.RegisterTask();
            }
            else if (_lastSnapshot != null)
            {
              ServiceSnapshot.Restore(_lastSnapshot);
              ServiceSnapshot.RestoreStartMode(_lastSnapshot);
              ServiceSnapshot.ClearSnapshot();
              RestoreFallback.RemoveTask();
            }
        }

        private void OnExitClicked(object? sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            Application.Exit();
        }
    }
}