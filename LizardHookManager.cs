using System;
using SharpHook;
using SharpHook.Native;

namespace LizardCrossPlatform
{
    public class LizardHookManager : IDisposable
    {
        private TaskPoolGlobalHook _hook;
        public event Action? KeyPressed;

        public LizardHookManager()
        {
            _hook = new TaskPoolGlobalHook();
            _hook.KeyPressed += OnKeyPressed;
        }

        private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
        {
            // Trigger the sound event
            KeyPressed?.Invoke();
        }

        public void Start()
        {
            // Run the hook on a background thread
            _hook.RunAsync();
        }

        public void Dispose()
        {
            _hook.Dispose();
        }
    }
}
