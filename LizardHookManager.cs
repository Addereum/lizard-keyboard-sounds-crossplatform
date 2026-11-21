using System;
using System.Threading;
using SharpHook;
using SharpHook.Data;

namespace LizardCrossPlatform;

public sealed class LizardHookManager : IDisposable
{
    private readonly TaskPoolGlobalHook _hook;
    private int _captureNextKey;
    private bool _disposed;

    public event Action<KeyCode>? KeyPressed;
    public event Action<KeyCode>? KeyReleased;
    public event Action<KeyCode>? KeyCaptured;
    public event Action? CaptureCancelled;

    public LizardHookManager()
    {
        _hook = new TaskPoolGlobalHook();
        _hook.KeyPressed += OnKeyPressed;
        _hook.KeyReleased += OnKeyReleased;
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        var keyCode = e.Data.KeyCode;
        if (Interlocked.Exchange(ref _captureNextKey, 0) == 1)
        {
            if (keyCode == KeyCode.VcEscape)
            {
                CaptureCancelled?.Invoke();
            }
            else if (keyCode != KeyCode.VcUndefined)
            {
                KeyCaptured?.Invoke(keyCode);
            }

            return;
        }

        if (keyCode != KeyCode.VcUndefined)
        {
            KeyPressed?.Invoke(keyCode);
        }
    }

    private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
    {
        var keyCode = e.Data.KeyCode;
        if (keyCode != KeyCode.VcUndefined)
        {
            KeyReleased?.Invoke(keyCode);
        }
    }

    public void Start() => _hook.RunAsync();

    public void CaptureNextKey() => Interlocked.Exchange(ref _captureNextKey, 1);

    public void CancelCapture() => Interlocked.Exchange(ref _captureNextKey, 0);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _hook.Dispose();
    }
}
