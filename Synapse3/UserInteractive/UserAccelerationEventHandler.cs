#define TRACE
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Synapse3.UserInteractive
{
    public class UserAccelerationEventHandler
    {
        private IUserAccelerationEvent _userAccelerationEvent;

        public UserAccelerationEventHandler(IUserAccelerationEvent userAccelerationEvent)
        {
            _userAccelerationEvent = userAccelerationEvent;
            _userAccelerationEvent.OnUserAccelerationEvent += _userAccelerationEvent_OnUserAccelerationEvent;
        }

        private void _userAccelerationEvent_OnUserAccelerationEvent(uint value)
        {
            SetAccelerationLevel(value);
            SetAccelerationState(value != 0);
        }

        private void SetAccelerationLevel(uint value)
        {
            if ((int)value < 0 || (int)value > 10)
            {
                Trace.TraceError($"Invalid acceleration value: {(int)value}");
                return;
            }
            int[] array = new int[3];
            GCHandle gCHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
            try
            {
                switch (value)
                {
                case 0u:
                    array[2] = 1;
                    array[0] = (array[1] = 0);
                    break;
                case 1u:
                case 2u:
                case 3u:
                case 4u:
                case 5u:
                    array[2] = 1;
                    array[0] = (int)(12 - value * 2);
                    array[1] = 0;
                    break;
                case 6u:
                case 7u:
                case 8u:
                case 9u:
                case 10u:
                    array[2] = 2;
                    array[0] = 2;
                    array[1] = (int)(12 - (value - 5) * 2) + array[0];
                    break;
                }
                Marshal.Copy(array, 0, gCHandle.AddrOfPinnedObject(), array.Length);
                if (!Win32PInvoke.SystemParametersInfo(Win32PInvoke.SPI.SPI_SETMOUSE, 0u, gCHandle.AddrOfPinnedObject(), Win32PInvoke.SPIF.SPIF_UPDATEINIFILE | Win32PInvoke.SPIF.SPIF_SENDCHANGE))
                {
                    Trace.TraceError($"Failed to set acceleration value: {(int)value}");
                }
            }
            catch (Exception arg)
            {
                Trace.TraceError($"SetAccelerationLevel exception: {arg}");
            }
            finally
            {
                if (gCHandle.IsAllocated)
                {
                    gCHandle.Free();
                }
            }
        }

        private void SetAccelerationState(bool bEnabled)
        {
            int[] array = new int[3];
            GCHandle gCHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
            try
            {
                Marshal.Copy(array, 0, gCHandle.AddrOfPinnedObject(), array.Length);
                if (Win32PInvoke.SystemParametersInfo(Win32PInvoke.SPI.SPI_GETMOUSE, 0u, gCHandle.AddrOfPinnedObject(), Win32PInvoke.SPIF.SPIF_UPDATEINIFILE | Win32PInvoke.SPIF.SPIF_SENDCHANGE))
                {
                    Marshal.Copy(gCHandle.AddrOfPinnedObject(), array, 0, array.Length);
                    if (bEnabled)
                    {
                        array[2] = 1;
                        if (array[1] > 0)
                        {
                            array[2] = 2;
                        }
                        else
                        {
                            array[2] = 1;
                        }
                    }
                    else
                    {
                        array[2] = 0;
                    }
                    Marshal.Copy(array, 0, gCHandle.AddrOfPinnedObject(), array.Length);
                    if (!Win32PInvoke.SystemParametersInfo(Win32PInvoke.SPI.SPI_SETMOUSE, 0u, gCHandle.AddrOfPinnedObject(), Win32PInvoke.SPIF.SPIF_UPDATEINIFILE | Win32PInvoke.SPIF.SPIF_SENDCHANGE))
                    {
                        Trace.TraceError($"Failed to enable/disable acceleration: {bEnabled}");
                    }
                }
                else
                {
                    Trace.TraceError($"SPI_GETMOUSE failed: {bEnabled}");
                }
            }
            catch (Exception arg)
            {
                Trace.TraceError($"SetAccelerationState exception: {arg}");
            }
            finally
            {
                if (gCHandle.IsAllocated)
                {
                    gCHandle.Free();
                }
            }
        }
    }
}
