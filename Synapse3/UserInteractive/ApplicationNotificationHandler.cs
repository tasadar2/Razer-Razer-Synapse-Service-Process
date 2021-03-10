using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace Synapse3.UserInteractive
{
    public class ApplicationNotificationHandler
    {
        private IApplicationNotificationEvent _appNotification;

        public const int KEYEVENTF_EXTENTEDKEY = 1;

        public const int KEYEVENTF_KEYDOWN = 0;

        public const int KEYEVENTF_KEYUP = 2;

        public const int VK_MEDIA_NEXT_TRACK = 176;

        public const int VK_MEDIA_PLAY_PAUSE = 179;

        public const int VK_MEDIA_PREV_TRACK = 177;

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte virtualKey, byte scanCode, uint flags, IntPtr extraInfo);

        public ApplicationNotificationHandler(IApplicationNotificationEvent appNotification)
        {
            _appNotification = appNotification;
            _appNotification.OnApplicationNotificationEvent += _appNotification_OnApplicationNotificationEvent;
        }

        private void _appNotification_OnApplicationNotificationEvent(string msg)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                DoNotification(msg);
            });
        }

        private void DoNotification(string msg)
        {
            if (msg == "StartChromaParty")
            {
                DoStartChromaParty();
            }
        }

        private void DoStartChromaParty()
        {
            keybd_event(179, 0, 0u, IntPtr.Zero);
            Thread.Sleep(100);
            keybd_event(179, 0, 2u, IntPtr.Zero);
        }
    }
}
