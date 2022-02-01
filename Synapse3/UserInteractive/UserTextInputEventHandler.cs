#define TRACE
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Synapse3.UserInteractive
{
    public class UserTextInputEventHandler
    {
        private IUserTextEvent _userTextEvent;

        private static ConcurrentBag<string> _userTexts;

        private readonly object _lock = new object();

        private const int WM_PASTE = 770;

        public UserTextInputEventHandler(IUserTextEvent userTextEvent)
        {
            _userTextEvent = userTextEvent;
            _userTextEvent.OnUserTextInputEvent += _userTextEvent_OnUserTextInputEvent;
            _userTexts = new ConcurrentBag<string>();
        }

        private void _userTextEvent_OnUserTextInputEvent(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            ConcurrentBag<string> userTexts = _userTexts;
            if (userTexts != null && !userTexts.Contains(text))
            {
                _userTexts?.Add(text);
            }
            try
            {
                lock (_lock)
                {
                    Thread thread = new Thread((ThreadStart)delegate
                    {
                        SwapExecute(text);
                    });
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();
                    thread.Join();
                }
            }
            catch (Exception arg)
            {
                Trace.TraceError($"Exception occured. {arg}");
            }
        }

        private static void SwapExecute(string text)
        {
            string text2 = string.Empty;
            string text3 = string.Empty;
            try
            {
                if (Clipboard.ContainsText())
                {
                    while (string.IsNullOrEmpty(text3))
                    {
                        text3 = Clipboard.GetText();
                        Thread.Sleep(1);
                    }
                }
                ConcurrentBag<string> userTexts = _userTexts;
                if ((userTexts != null && !userTexts.Contains(text3)) || text3.Equals(text))
                {
                    text2 = string.Copy(text3);
                }
                Clipboard.SetText(text);
                SendKeys.SendWait("^v");
                if (!string.IsNullOrEmpty(text2))
                {
                    Clipboard.SetText(text2);
                }
                else
                {
                    Clipboard.Clear();
                }
            }
            catch (Exception arg)
            {
                Trace.TraceError($"Swap: Exception occured. {arg}");
            }
        }

        private string GetTextThreadProc()
        {
            string empty = string.Empty;
            try
            {
                return Clipboard.GetText();
            }
            catch (Exception arg)
            {
                Trace.TraceError($"GetTextThreadProc: Exception occured. {arg}");
                return string.Empty;
            }
        }

        private void SetTextThreadProc(string text)
        {
            try
            {
                Clipboard.SetText(text);
            }
            catch (Exception arg)
            {
                Trace.TraceError($"SetTextThreadProc: Exception occured. {arg}");
            }
        }

        private void ClearThreadProc()
        {
            try
            {
                Clipboard.Clear();
            }
            catch (Exception arg)
            {
                Trace.TraceError($"ClearThreadProc: Exception occured. {arg}");
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PostMessage(HandleRef hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
    }
}
