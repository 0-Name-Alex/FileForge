using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace FileForge.Platform.Windows.Hotkeys;

public sealed class WindowsHotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;
    private const uint ModNoRepeat = 0x4000;
    private static readonly nint HwndMessage = -3;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(nint hWnd, int id);

    private readonly Dictionary<string, int> _idsByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, string> _namesById = new();
    private HwndSource? _source;
    private int _nextId = 0x0100;
    private bool _disposed;

    public event EventHandler<string>? HotkeyPressed;

    public bool Register(string name, ModifierKeys modifiers, Key key)
    {
        Unregister(name);

        var hwnd = EnsureWindow();
        var id = _nextId++;
        uint nativeModifiers = ModNoRepeat;

        if (modifiers.HasFlag(ModifierKeys.Alt)) nativeModifiers |= ModAlt;
        if (modifiers.HasFlag(ModifierKeys.Control)) nativeModifiers |= ModControl;
        if (modifiers.HasFlag(ModifierKeys.Shift)) nativeModifiers |= ModShift;
        if (modifiers.HasFlag(ModifierKeys.Windows)) nativeModifiers |= ModWin;

        int virtualKey = KeyInterop.VirtualKeyFromKey(key);

        if (!RegisterHotKey(hwnd, id, nativeModifiers, (uint)virtualKey))
        {
            return false;
        }

        _idsByName[name] = id;
        _namesById[id] = name;
        return true;
    }

    public void Unregister(string name)
    {
        if (!_idsByName.Remove(name, out var id)) return;
        _namesById.Remove(id);

        if (_source != null)
        {
            UnregisterHotKey(_source.Handle, id);
        }
    }

    private nint EnsureWindow()
    {
        if (_source == null)
        {
            var parameters = new HwndSourceParameters("FileForge.Hotkeys")
            {
                Width = 0,
                Height = 0,
                WindowStyle = 0,
                ParentWindow = HwndMessage // Message-only invisible window
            };
            _source = new HwndSource(parameters);
            _source.AddHook(WndProc);
        }
        return _source.Handle;
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == WmHotkey && _namesById.TryGetValue((int)wParam, out var name))
        {
            handled = true;
            HotkeyPressed?.Invoke(this, name);
        }
        return nint.Zero;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_source != null)
        {
            foreach (var id in _idsByName.Values)
            {
                UnregisterHotKey(_source.Handle, id);
            }
            _source.RemoveHook(WndProc);
            _source.Dispose();
            _source = null;
        }
    }
}
