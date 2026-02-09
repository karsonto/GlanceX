using System.Windows;
using System.Windows.Interop;
using DesktopTranslator.Helpers;

namespace DesktopTranslator.Services;

public class HotkeyService : IDisposable
{
    private IntPtr _windowHandle;
    private HwndSource? _hwndSource;
    private readonly Dictionary<int, Action> _hotkeyActions = new();
    private int _nextId = 9000;
    private bool _disposed;

    public event Action? SelectRegionPressed;
    public event Action? ToggleTranslationPressed;

    public void Initialize(Window window)
    {
        var helper = new WindowInteropHelper(window);
        _windowHandle = helper.Handle;

        if (_windowHandle == IntPtr.Zero)
        {
            // Window hasn't been shown yet, defer initialization
            window.SourceInitialized += (s, e) =>
            {
                _windowHandle = new WindowInteropHelper(window).Handle;
                SetupMessageHook();
                RegisterDefaultHotkeys();
            };
        }
        else
        {
            SetupMessageHook();
            RegisterDefaultHotkeys();
        }
    }

    private void SetupMessageHook()
    {
        _hwndSource = HwndSource.FromHwnd(_windowHandle);
        _hwndSource?.AddHook(WndProc);
    }

    private void RegisterDefaultHotkeys()
    {
        // Ctrl+Alt+S - Select Region
        RegisterHotkey(
            NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT | NativeMethods.MOD_NOREPEAT,
            NativeMethods.VK_S,
            () => SelectRegionPressed?.Invoke());

        // Ctrl+Alt+T - Toggle Translation
        RegisterHotkey(
            NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT | NativeMethods.MOD_NOREPEAT,
            NativeMethods.VK_T,
            () => ToggleTranslationPressed?.Invoke());
    }

    public bool RegisterHotkey(uint modifiers, uint key, Action callback)
    {
        var id = _nextId++;
        if (NativeMethods.RegisterHotKey(_windowHandle, id, modifiers, key))
        {
            _hotkeyActions[id] = callback;
            return true;
        }
        return false;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            if (_hotkeyActions.TryGetValue(id, out var action))
            {
                action.Invoke();
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var id in _hotkeyActions.Keys)
        {
            NativeMethods.UnregisterHotKey(_windowHandle, id);
        }
        _hotkeyActions.Clear();

        _hwndSource?.RemoveHook(WndProc);
        _hwndSource = null;
    }
}
