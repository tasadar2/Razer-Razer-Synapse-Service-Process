using System;
using System.Configuration;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Timers;

namespace Synapse3.UserInteractive
{
    public class UserInputMonitor
    {
        private struct LASTINPUTINFO
        {
            public static readonly int SizeOf = Marshal.SizeOf(typeof(LASTINPUTINFO));

            [MarshalAs(UnmanagedType.U4)]
            public uint cbSize;

            [MarshalAs(UnmanagedType.U4)]
            public uint dwTime;
        }

        private Timer _lastInputTimer;

        private ISendLastInputInfo _lastInputInfoImpl;

        public UserInputMonitor(ISendLastInputInfo lastInputInfoImpl)
        {
            _lastInputInfoImpl = lastInputInfoImpl;
            _lastInputTimer = new Timer();
            _lastInputTimer.Interval = 5000.0;
            _lastInputTimer.Elapsed += _lastInputTimer_Elapsed;
        }

        public void Start()
        {
            _lastInputTimer.Start();
        }

        private async void _lastInputTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (IsSynapseServiceRunning())
            {
                await _lastInputInfoImpl.SetLastInputInfo(GetLastInputTime());
            }
        }

        private bool IsSynapseServiceRunning()
        {
            string service = ConfigurationManager.AppSettings["service_name"];
            if (!ServiceController.GetServices().Any((ServiceController serviceController) => serviceController.ServiceName.Equals(service)))
            {
                return false;
            }
            if (new ServiceController(service).Status != ServiceControllerStatus.Running)
            {
                return false;
            }
            return true;
        }

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        private static uint GetLastInputTime()
        {
            uint num = 0u;
            LASTINPUTINFO plii = default(LASTINPUTINFO);
            plii.cbSize = (uint)Marshal.SizeOf((object)plii);
            plii.dwTime = 0u;
            uint tickCount = (uint)Environment.TickCount;
            if (GetLastInputInfo(ref plii))
            {
                uint dwTime = plii.dwTime;
                num = tickCount - dwTime;
            }
            if (num == 0)
            {
                return 0u;
            }
            return num / 1000u;
        }
    }
}
