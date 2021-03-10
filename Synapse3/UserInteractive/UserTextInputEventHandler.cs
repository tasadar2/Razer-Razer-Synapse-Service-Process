#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Synapse3.UserInteractive
{
    public class UserTextInputEventHandler
    {
        private IUserTextEvent _userTextEvent;

        private string previous = string.Empty;

        private List<string> _userTexts;

        private const int WM_PASTE = 770;

        public UserTextInputEventHandler(IUserTextEvent userTextEvent)
        {
            _userTextEvent = userTextEvent;
            _userTextEvent.OnUserTextInputEvent += _userTextEvent_OnUserTextInputEvent;
            _userTexts = new List<string>();
        }

        private void _userTextEvent_OnUserTextInputEvent(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            List<string> userTexts = _userTexts;
            if (userTexts != null && !userTexts.Contains(text))
            {
                _userTexts?.Add(text);
            }
            try
            {
                string clip = string.Empty;
                Thread thread = new Thread((ThreadStart)delegate
                {
                    clip = GetTextThreadProc();
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
                List<string> userTexts2 = _userTexts;
                if ((userTexts2 != null && !userTexts2.Contains(clip)) || clip.Equals(text))
                {
                    previous = clip;
                }
                Thread thread2 = new Thread((ThreadStart)delegate
                {
                    SetTextThreadProc(text);
                });
                thread2.SetApartmentState(ApartmentState.STA);
                thread2.Start();
                thread2.Join();
                SendKeys.SendWait("^v");
            }
            catch (Exception arg)
            {
                Trace.TraceError($"Exception occured. {arg}");
            }
            finally
            {
                try
                {
                    Thread.Sleep(100);
                    if (previous != string.Empty)
                    {
                        Thread thread3 = new Thread((ThreadStart)delegate
                        {
                            SetTextThreadProc(previous);
                        });
                        thread3.SetApartmentState(ApartmentState.STA);
                        thread3.Start();
                        thread3.Join();
                    }
                    else
                    {
                        Thread thread4 = new Thread((ThreadStart)delegate
                        {
                            ClearThreadProc();
                        });
                        thread4.SetApartmentState(ApartmentState.STA);
                        thread4.Start();
                        thread4.Join();
                    }
                }
                catch (Exception arg2)
                {
                    Trace.TraceError($"Exception occured. {arg2}");
                }
            }
        }

        private string GetTextThreadProc()
        {
            _ = string.Empty;
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
