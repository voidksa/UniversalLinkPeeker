using Gma.System.MouseKeyHook;
using System;
using System.Windows.Forms;

namespace UniversalLinkPeeker.Services
{
    public enum TriggerKey
    {
        Shift,
        Ctrl,
        Alt
    }

    public class InputHookService : IDisposable
    {
        private IKeyboardMouseEvents _globalHook;
        public event EventHandler ActivationKeyPressed;
        public event EventHandler ActivationKeyReleased;
        public event MouseEventHandler MouseMoved;
        public event MouseEventHandler MouseWheel;
        public event EventHandler CopyCommandTriggered;

        public TriggerKey CurrentTriggerKey { get; set; } = TriggerKey.Shift;

        public InputHookService()
        {
            _globalHook = Hook.GlobalEvents();
            _globalHook.KeyDown += OnKeyDown;
            _globalHook.KeyUp += OnKeyUp;
            _globalHook.MouseMove += OnMouseMove;
            _globalHook.MouseWheel += OnMouseWheel;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (IsTriggerKey(e.KeyCode))
            {
                ActivationKeyPressed?.Invoke(this, EventArgs.Empty);
            }
            else if (e.KeyCode == Keys.C)
            {
                CopyCommandTriggered?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (IsTriggerKey(e.KeyCode))
            {
                ActivationKeyReleased?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool IsTriggerKey(Keys key)
        {
            switch (CurrentTriggerKey)
            {
                case TriggerKey.Shift:
                    return key == Keys.LShiftKey || key == Keys.RShiftKey || key == Keys.ShiftKey;
                case TriggerKey.Ctrl:
                    return key == Keys.LControlKey || key == Keys.RControlKey || key == Keys.ControlKey;
                case TriggerKey.Alt:
                    return key == Keys.LMenu || key == Keys.RMenu || key == Keys.Menu || key == Keys.Alt;
                default:
                    return false;
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            MouseMoved?.Invoke(this, e);
        }

        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            MouseWheel?.Invoke(this, e);
        }

        public void Dispose()
        {
            if (_globalHook != null)
            {
                _globalHook.KeyDown -= OnKeyDown;
                _globalHook.KeyUp -= OnKeyUp;
                _globalHook.MouseMove -= OnMouseMove;
                _globalHook.MouseWheel -= OnMouseWheel;
                _globalHook.Dispose();
                _globalHook = null;
            }
        }
    }
}
