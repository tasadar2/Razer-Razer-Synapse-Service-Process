#define TRACE
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Synapse3.UserInteractive
{
    public class MessageEventHandler
    {
        private IMessageEvent _messageEvent;

        private readonly IntPtr HWND_BROADCAST = new IntPtr(65535);

        private readonly int WM_SYSCOMMAND = 274;

        private readonly int SC_MONITORPOWER = 61808;

        private readonly IntPtr OFF = new IntPtr(2);

        public MessageEventHandler(IMessageEvent messageEvent)
        {
            _messageEvent = messageEvent;
            _messageEvent.OnMessageEvent += OnMessageEvent;
        }

        private void OnMessageEvent(string msg)
        {
            msg = msg.ToLower();
            if (msg == "monitoroff")
            {
                Trace.TraceInformation($"OnMessageEvent: SendMessage {HWND_BROADCAST}, {WM_SYSCOMMAND}, {WM_SYSCOMMAND}, {OFF}");
                SendMessage(HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, OFF);
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);
    }
}
