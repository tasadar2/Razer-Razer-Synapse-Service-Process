#define TRACE
using System;
using System.Diagnostics;
using System.Windows.Forms;
using Contract.Common;
using Contract.MacroLib;
using Gma.System.MouseKeyHook;
using Synapse3.Macros.Common.Utilities;

namespace Synapse3.UserInteractive
{
    public class OTFMacroRecorder
    {
        private Macro _tempMacro;

        private IKeyboardMouseEvents _globalHook;

        private Keys _lastPressedKey;

        private Stopwatch _stopWatch;

        public Device ActiveDevice { get; set; }

        public bool IsDone { get; private set; }

        public void Start()
        {
            string name = $"New Macro ({DateTime.Now.ToShortTimeString()})";
            _tempMacro = new Macro
            {
                Guid = Guid.NewGuid(),
                Name = name
            };
            _stopWatch = new Stopwatch();
            IsDone = false;
            _lastPressedKey = Keys.None;
            Trace.TraceInformation("GlobalHook initialization - start");
            if (_globalHook != null)
            {
                _globalHook.KeyDownExt -= GlobalHook_KeyDownExt;
                _globalHook.KeyUpExt -= GlobalHook_KeyUpExt;
                _globalHook.MouseDown -= GlobalHook_MouseDown;
                _globalHook.MouseUp -= GlobalHook_MouseUp;
                _globalHook.Dispose();
            }
            _globalHook = Hook.GlobalEvents();
            _globalHook.KeyDownExt += GlobalHook_KeyDownExt;
            _globalHook.KeyUpExt += GlobalHook_KeyUpExt;
            _globalHook.MouseDown += GlobalHook_MouseDown;
            _globalHook.MouseUp += GlobalHook_MouseUp;
            Trace.TraceInformation("GlobalHook initialization - done");
        }

        public void Stop(bool isCancelled)
        {
            _globalHook.KeyDownExt -= GlobalHook_KeyDownExt;
            _globalHook.KeyUpExt -= GlobalHook_KeyUpExt;
            _globalHook.MouseDown -= GlobalHook_MouseDown;
            _globalHook.MouseUp -= GlobalHook_MouseUp;
            _globalHook.Dispose();
            if (isCancelled)
            {
                Trace.TraceInformation("Macro Recording is cancelled. Clearing all temp macros");
                _tempMacro = null;
            }
            IsDone = true;
        }

        public Macro GetMacro()
        {
            return _tempMacro;
        }

        private int GetDelay()
        {
            int result = 0;
            if (!_stopWatch.IsRunning)
            {
                _stopWatch.Restart();
            }
            else
            {
                result = (int)_stopWatch.ElapsedMilliseconds;
                _stopWatch.Restart();
            }
            return result;
        }

        private void AddMacroEvent(MacroEvent me)
        {
            me.Delay = (uint)GetDelay();
            _tempMacro.MacroEvents.Add(me);
        }

        private void GlobalHook_KeyDownExt(object sender, KeyEventArgsExt e)
        {
            if (_lastPressedKey != e.KeyCode)
            {
                _lastPressedKey = e.KeyCode;
                KeyBoardEvent keyEvent = new KeyBoardEvent
                {
                    Makecode = (ushort)e.ScanCode,
                    State = 0,
                    IsExtended = e.IsExtended
                };
                MacroEvent me = new MacroEvent
                {
                    Type = 1u,
                    KeyEvent = keyEvent
                };
                AddMacroEvent(me);
            }
        }

        private void GlobalHook_KeyUpExt(object sender, KeyEventArgsExt e)
        {
            _lastPressedKey = Keys.None;
            KeyBoardEvent keyEvent = new KeyBoardEvent
            {
                Makecode = (ushort)e.ScanCode,
                State = 1,
                IsExtended = e.IsExtended
            };
            MacroEvent me = new MacroEvent
            {
                Type = 1u,
                KeyEvent = keyEvent
            };
            AddMacroEvent(me);
        }

        private void GlobalHook_MouseDown(object sender, MouseEventArgs e)
        {
            MouseButtonEvent mouseEvent = new MouseButtonEvent
            {
                State = 0,
                MouseButton = (ushort)RzMouseButtonConverter.GetRzButton(e.Button)
            };
            MacroEvent me = new MacroEvent
            {
                Type = 2u,
                MouseEvent = mouseEvent
            };
            AddMacroEvent(me);
        }

        private void GlobalHook_MouseUp(object sender, MouseEventArgs e)
        {
            MouseButtonEvent mouseEvent = new MouseButtonEvent
            {
                State = 1,
                MouseButton = (ushort)RzMouseButtonConverter.GetRzButton(e.Button)
            };
            MacroEvent me = new MacroEvent
            {
                Type = 2u,
                MouseEvent = mouseEvent
            };
            AddMacroEvent(me);
        }
    }
}
