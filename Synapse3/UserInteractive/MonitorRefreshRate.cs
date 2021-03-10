#define TRACE
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Synapse3.UserInteractive
{
    internal class MonitorRefreshRate
    {
        public struct DEVMODE
        {
            private const int CCHDEVICENAME = 32;

            private const int CCHFORMNAME = 32;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;

            public short dmSpecVersion;

            public short dmDriverVersion;

            public short dmSize;

            public short dmDriverExtra;

            public int dmFields;

            public int dmPositionX;

            public int dmPositionY;

            public ScreenOrientation dmDisplayOrientation;

            public int dmDisplayFixedOutput;

            public short dmColor;

            public short dmDuplex;

            public short dmYResolution;

            public short dmTTOption;

            public short dmCollate;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;

            public short dmLogPixels;

            public int dmBitsPerPel;

            public int dmPelsWidth;

            public int dmPelsHeight;

            public int dmDisplayFlags;

            public int dmDisplayFrequency;

            public int dmICMMethod;

            public int dmICMIntent;

            public int dmMediaType;

            public int dmDitherType;

            public int dmReserved1;

            public int dmReserved2;

            public int dmPanningWidth;

            public int dmPanningHeight;
        }

        [Flags]
        public enum ChangeDisplaySettingsFlags : uint
        {
            CDS_NONE = 0x0u,
            CDS_UPDATEREGISTRY = 0x1u,
            CDS_TEST = 0x2u,
            CDS_FULLSCREEN = 0x4u,
            CDS_GLOBAL = 0x8u,
            CDS_SET_PRIMARY = 0x10u,
            CDS_VIDEOPARAMETERS = 0x20u,
            CDS_ENABLE_UNSAFE_MODES = 0x100u,
            CDS_DISABLE_UNSAFE_MODES = 0x200u,
            CDS_RESET = 0x40000000u,
            CDS_RESET_EX = 0x20000000u,
            CDS_NORESET = 0x10000000u
        }

        private const int DISP_CHANGE_SUCCESSFUL = 0;

        private const int ENUM_CURRENT_SETTINGS = -1;

        [DllImport("User32.dll")]
        public static extern int ChangeDisplaySettings(ref DEVMODE devMode, ChangeDisplaySettingsFlags flags);

        [DllImport("user32.dll")]
        public static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

        public static bool SetScreenRefreshRate(int item)
        {
            DEVMODE devMode = default(DEVMODE);
            devMode.dmSize = (short)Marshal.SizeOf((object)devMode);
            try
            {
                if (EnumDisplaySettings(null, -1, ref devMode))
                {
                    devMode.dmDisplayFrequency = item;
                    if (ChangeDisplaySettings(ref devMode, ChangeDisplaySettingsFlags.CDS_UPDATEREGISTRY) == 0)
                    {
                        return true;
                    }
                }
            }
            catch (Exception arg)
            {
                Trace.TraceError($"OnGetScreenRefreshRateListEvent: Exception occured: {arg}");
            }
            return false;
        }
    }
}
