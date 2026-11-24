using System.Drawing;
using System.Windows.Forms;
using FileForge.Platform.Windows.Memory;

namespace FileForge.Platform.Windows.Tray;

public sealed class SystemTrayService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private bool _disposed;

    public event EventHandler? ShowRequested;
    public event EventHandler? QuickLauncherRequested;
    public event EventHandler? ExitRequested;

    public SystemTrayService()
    {
        _notifyIcon = new NotifyIcon
        {
            Text = "FileForge — Universal File Converter & Editor",
            Icon = SystemIcons.Application,
            Visible = true
        };

        var contextMenu = new ContextMenuStrip();

        var openItem = new ToolStripMenuItem("Show FileForge", null, (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty));
        openItem.Font = new Font(openItem.Font, System.Drawing.FontStyle.Bold);

        var quickItem = new ToolStripMenuItem("Quick Launcher (Ctrl+Alt+F)", null, (_, _) => QuickLauncherRequested?.Invoke(this, EventArgs.Empty));

        var trimItem = new ToolStripMenuItem("Trim Memory", null, (_, _) =>
        {
            var before = MemoryOptimizer.GetCurrentMemoryMb();
            MemoryOptimizer.TrimWorkingSet();
            var after = MemoryOptimizer.GetCurrentMemoryMb();
            _notifyIcon.ShowBalloonTip(1500, "FileForge Memory Trimmed", $"Memory: {before:F1} MB -> {after:F1} MB", ToolTipIcon.Info);
        });

        var exitItem = new ToolStripMenuItem("Exit", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));

        contextMenu.Items.Add(openItem);
        contextMenu.Items.Add(quickItem);
        contextMenu.Items.Add(trimItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (_, _) => ShowRequested?.Invoke(this, EventArgs.Empty);
    }

    public void SetIcon(Icon icon) => _notifyIcon.Icon = icon;

    public void ShowNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info, int timeoutMs = 2500)
    {
        _notifyIcon.ShowBalloonTip(timeoutMs, title, message, icon);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
