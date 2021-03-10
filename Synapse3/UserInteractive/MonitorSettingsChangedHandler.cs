#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using System.Windows.Forms;
using Common.Internal;
using Contract.Common;
using Contract.MonitorLib;
using Microsoft.Win32;
using RzCtlMW;

namespace Synapse3.UserInteractive
{
    public class MonitorSettingsChangedHandler
    {
        private Dictionary<long, RzCtl_ManagedWrapper> _wrappers;

        private readonly IMonitorSettingsChangedCallback _monitorSettingsEvent;

        private readonly IMonitorSettingsFetchedCallback _monitorSettingsFetchEvent;

        private readonly IDeviceDetection _deviceDetection;

        private readonly IWndProc _wndProc;

        private readonly object _lock;

        private System.Timers.Timer _refreshHandleTimer;

        public MonitorSettingsChangedHandler(IDeviceDetection deviceDetection, IMonitorSettingsChangedCallback monitorSettingsEvent, IMonitorSettingsFetchedCallback monitorSettingsFetchEvent, IWndProc wndProc)
        {
            _wrappers = new Dictionary<long, RzCtl_ManagedWrapper>();
            _wndProc = wndProc;
            _lock = new object();
            _deviceDetection = deviceDetection;
            _monitorSettingsEvent = monitorSettingsEvent;
            _monitorSettingsFetchEvent = monitorSettingsFetchEvent;
            _deviceDetection.OnDeviceAddedEvent += OnDeviceAddedEvent;
            _deviceDetection.OnDeviceRemovedEvent += OnDeviceRemovedEvent;
            _monitorSettingsEvent.OnMonitorSettingsChangedBrightnessEvent += OnMonitorSettingsChangedBrightnessEvent;
            _monitorSettingsEvent.OnMonitorSettingsChangedContrastEvent += OnMonitorSettingsChangedContrastEvent;
            _monitorSettingsEvent.OnMonitorSettingsChangedColorPresetEvent += OnMonitorSettingsChangedColorPresetEvent;
            _monitorSettingsEvent.OnMonitorSettingsChangedDisplayModeEvent += OnMonitorSettingsChangedDisplayModeEvent;
            _monitorSettingsEvent.OnMonitorSettingsChangedFreeSyncEvent += OnMonitorSettingsChangedFreeSyncEvent;
            _monitorSettingsEvent.OnMonitorSettingsChangedImageScalingEvent += OnMonitorSettingsChangedImageScalingEvent;
            _monitorSettingsEvent.OnMonitorSettingsChangedOverdriveEvent += OnMonitorSettingsChangedOverdriveEvent;
            _monitorSettingsEvent.OnMonitorSettingsChangedPiPSettingsEvent += OnMonitorSettingsChangedPiPSettingsEvent;
            _monitorSettingsEvent.OnMonitorSettingsChangedFPSEvent += OnMonitorSettingsChangedFPSEvent;
            _monitorSettingsEvent.OnMonitorSettingsChangedHDREvent += OnMonitorSettingsChangedHDREvent;
            _monitorSettingsEvent.OnMonitorSettingsChangedMotionBlurEvent += OnMonitorSettingsChangedMotionBlurEvent;
            _monitorSettingsEvent.OnMonitorSettingsChangedInputSourceEvent += OnMonitorSettingsChangedInputSourceEvent;
            _monitorSettingsEvent.OnMonitorSettingsChangedInputAutoSwitchEvent += OnMonitorSettingsChangedInputAutoSwitchEvent;
            _monitorSettingsEvent.OnMonitorSettingsChangedGammaEvent += OnMonitorSettingsChangedGammaEvent;
            _monitorSettingsEvent.OnMonitorSettingsChangedDeviceModeEvent += OnMonitorSettingsChangedDeviceModeEvent;
            _monitorSettingsFetchEvent.OnMonitorSettingsFetchedFreeSyncEvent += OnMonitorSettingsFetchedFreeSyncEvent;
            _monitorSettingsFetchEvent.OnMonitorSettingsFetchedPiPSettingsEvent += OnMonitorSettingsFetchedPiPSettingsEvent;
            _monitorSettingsFetchEvent.OnMonitorSettingsFetchedFPSEvent += OnMonitorSettingsFetchedFPSEvent;
            _monitorSettingsFetchEvent.OnMonitorSettingsFetchedHDREvent += OnMonitorSettingsFetchedHDREvent;
            _monitorSettingsFetchEvent.OnMonitorSettingsFetchedInputSourceEvent += OnMonitorSettingsFetchedInputSourceEvent;
            _monitorSettingsFetchEvent.OnMonitorSettingsFetchedInputAutoSwitchEvent += OnMonitorSettingsFetchedInputAutoSwitchEvent;
            _wndProc.OnWndProcEvent += OnWndProcEvent;
            _refreshHandleTimer = new System.Timers.Timer();
            _refreshHandleTimer.AutoReset = false;
            _refreshHandleTimer.Interval = 2000.0;
            _refreshHandleTimer.Elapsed += HandleRefreshHandler;
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
        }

        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode != PowerModes.Suspend)
            {
                return;
            }
            Dictionary<long, RzCtl_ManagedWrapper> wrappers = _wrappers;
            if (wrappers == null || wrappers.Count <= 0)
            {
                return;
            }
            lock (_lock)
            {
                foreach (KeyValuePair<long, RzCtl_ManagedWrapper> wrapper in _wrappers)
                {
                    wrapper.Value.Dispose();
                }
                Trace.TraceInformation("SystemEvents_PowerModeChanged: middleware wrappers cleared.");
                _wrappers.Clear();
            }
        }

        private void ResetHandleTimer()
        {
            Trace.TraceInformation($"ResetHandleTimer: HandleRefreshHandler will execute on {DateTime.Now.AddMilliseconds(_refreshHandleTimer.Interval)}");
            _refreshHandleTimer.Stop();
            _refreshHandleTimer.Start();
        }

        private void HandleRefreshHandler(object sender, ElapsedEventArgs e)
        {
            Trace.TraceInformation($"HandleRefreshHandler: Refreshing Handles count: {_wrappers.Count}");
            lock (_lock)
            {
                foreach (KeyValuePair<long, RzCtl_ManagedWrapper> wrapper in _wrappers)
                {
                    MWSetImpl(wrapper.Value, "Monitor_Reset", data: true);
                }
            }
            Trace.TraceInformation("HandleRefreshHandler: Refresh done.");
        }

        private void OnWndProcEvent(Message message)
        {
            if (message.Msg == 26 && message.WParam.ToInt32() == 47)
            {
                Trace.TraceInformation("OnWndProcEvent: Received WM_SETTINGCHANGE & SPI_SETWORKAREA, Refreshing handles.");
                ResetHandleTimer();
            }
        }

        private void OnDeviceAddedEvent(Device item)
        {
            if (item.Type == 15)
            {
                Wrapper(item);
            }
        }

        private void OnDeviceRemovedEvent(Device item)
        {
            if (item.Type == 15)
            {
                Wrapper(item, bCleanUp: true);
            }
        }

        private RzCtl_ManagedWrapper Wrapper(Device device, bool bCleanUp = false)
        {
            lock (_lock)
            {
                Trace.TraceInformation($"Wrapper: Enter cleanup? {bCleanUp}");
                if (bCleanUp)
                {
                    if (_wrappers.ContainsKey(device.Handle))
                    {
                        _wrappers[device.Handle].Dispose();
                        _wrappers.Remove(device.Handle);
                        Trace.TraceInformation($"Wrapper: {device.Product_ID} removed from collection.");
                        return null;
                    }
                    Trace.TraceWarning($"Wrapper: mw for {device.Product_ID} not found. Cleanup failed.");
                    return null;
                }
                if (!_wrappers.ContainsKey(device.Handle))
                {
                    _wrappers[device.Handle] = new RzCtl_ManagedWrapper((ushort)device.Vendor_ID, (ushort)device.Product_ID, (ulong)device.Handle, Constants.RAZER_BIN + "\\Devices\\Mw\\");
                    if (!_wrappers[device.Handle].Initialise())
                    {
                        _wrappers.Remove(device.Handle);
                        Trace.TraceError($"Wrapper: Initialise failed {device.Product_ID}. Undoing add.");
                    }
                    else
                    {
                        Trace.TraceInformation($"Wrapper: Added {device.Product_ID} to collection.");
                    }
                }
                if (_wrappers.ContainsKey(device.Handle))
                {
                    Trace.TraceInformation($"Wrapper: Found mw instance for {device.Product_ID} from collection. Returning mw instance.");
                    return _wrappers[device.Handle];
                }
                Trace.TraceError($"Wrapper: mw instance for {device.Product_ID} now found in collection. Returning null.");
                return null;
            }
        }

        private void OnMonitorSettingsChangedBrightnessEvent(MonitorBrightness item)
        {
            ulong data = item.Brightness;
            MWSetImpl(Wrapper(item.Device), "Monitor_Brightness", data);
        }

        private void OnMonitorSettingsChangedContrastEvent(MonitorContrast item)
        {
            ulong data = item.Contrast;
            MWSetImpl(Wrapper(item.Device), "Monitor_Contrast", data);
        }

        private void OnMonitorSettingsChangedColorPresetEvent(MonitorColorPreset item)
        {
            if (item.Mode != MonitorColorPresetMode.Invalid)
            {
                mcMode mcMode = new mcMode();
                mcMode.mode = (byte)item.Mode;
                MWSetImpl(Wrapper(item.Device), "Monitor_ColorPreset", mcMode);
            }
            if (item.Mode == MonitorColorPresetMode.Custom)
            {
                mcLedColor mcLedColor = new mcLedColor();
                mcLedColor.color.red = item.VideoGain.Red;
                mcLedColor.color.blue = item.VideoGain.Blue;
                mcLedColor.color.green = item.VideoGain.Green;
                MWSetImpl(Wrapper(item.Device), "Monitor_VideoGain", mcLedColor);
            }
        }

        private void OnMonitorSettingsChangedDisplayModeEvent(MonitorDisplayMode item)
        {
            mcMode mcMode = new mcMode();
            mcMode.mode = (byte)item.Mode;
            MWSetImpl(Wrapper(item.Device), "Monitor_DisplayMode", mcMode);
        }

        private void OnMonitorSettingsChangedFreeSyncEvent(MonitorFreeSync item)
        {
            mcMode mcMode = new mcMode();
            mcMode.mode = (byte)item.Mode;
            MWSetImpl(Wrapper(item.Device), "Monitor_FreeSync", mcMode);
        }

        private void OnMonitorSettingsFetchedFreeSyncEvent(MonitorFreeSync item)
        {
            mcMode data = new mcMode();
            MWGetImpl(Wrapper(item.Device), "Monitor_FreeSync", ref data);
            item.Mode = (MonitorFreeSyncMode)data.mode;
            _monitorSettingsFetchEvent.Response(item);
        }

        private void OnMonitorSettingsChangedImageScalingEvent(MonitorImageScaling item)
        {
            mcMode mcMode = new mcMode();
            mcMode.mode = (byte)item.Mode;
            MWSetImpl(Wrapper(item.Device), "Monitor_ImageScaling", mcMode);
        }

        private void OnMonitorSettingsChangedOverdriveEvent(MonitorOverdrive item)
        {
            mcMode mcMode = new mcMode();
            mcMode.mode = (byte)item.Mode;
            MWSetImpl(Wrapper(item.Device), "Monitor_Overdrive", mcMode);
        }

        private void OnMonitorSettingsChangedPiPSettingsEvent(MonitorPiPSettings item)
        {
            if (item.Mode != MonitorPiPMode.Invalid)
            {
                mcMode mcMode = new mcMode();
                mcMode.mode = (byte)item.Mode;
                MWSetImpl(Wrapper(item.Device), "Monitor_PiPMode", mcMode);
            }
            if (item.Source != MonitorPiPSource.Invalid)
            {
                mcMode mcMode2 = new mcMode();
                mcMode2.mode = (byte)item.Source;
                MWSetImpl(Wrapper(item.Device), "Monitor_PiPSource", mcMode2);
            }
            if (item.Size != MonitorPiPSize.Invalid)
            {
                mcMode mcMode3 = new mcMode();
                mcMode3.mode = (byte)item.Size;
                MWSetImpl(Wrapper(item.Device), "Monitor_PiPSize", mcMode3);
            }
            if (item.Position != MonitorPiPPosition.Invalid)
            {
                mcMode mcMode4 = new mcMode();
                mcMode4.mode = (byte)item.Position;
                MWSetImpl(Wrapper(item.Device), "Monitor_PiPPosition", mcMode4);
            }
        }

        private void OnMonitorSettingsFetchedPiPSettingsEvent(MonitorPiPSettings item)
        {
            mcMode data = new mcMode();
            MWGetImpl(Wrapper(item.Device), "Monitor_PiPMode", ref data);
            item.Mode = (MonitorPiPMode)data.mode;
            mcMode data2 = new mcMode();
            MWGetImpl(Wrapper(item.Device), "Monitor_PiPSource", ref data2);
            item.Source = (MonitorPiPSource)data2.mode;
            mcMode data3 = new mcMode();
            MWGetImpl(Wrapper(item.Device), "Monitor_PiPSize", ref data3);
            item.Size = (MonitorPiPSize)((data3.mode == 0) ? (-1) : ((int)data3.mode));
            mcMode data4 = new mcMode();
            MWGetImpl(Wrapper(item.Device), "Monitor_PiPPosition", ref data4);
            item.Position = (MonitorPiPPosition)data4.mode;
            _monitorSettingsFetchEvent.Response(item);
        }

        private void OnMonitorSettingsChangedFPSEvent(MonitorFPS item)
        {
            mcMode mcMode = new mcMode();
            mcMode.mode = (byte)((item.State != 0) ? ((byte)item.Position) : 0);
            MWSetImpl(Wrapper(item.Device), "Monitor_FPSPosition", mcMode);
        }

        private void OnMonitorSettingsFetchedFPSEvent(MonitorFPS item)
        {
            mcMode data = new mcMode();
            MWGetImpl(Wrapper(item.Device), "Monitor_FPSPosition", ref data);
            item.State = ((data.mode != 0) ? 1u : 0u);
            item.Position = (MonitorFPSPosition)((data.mode == 0) ? (-1) : ((int)data.mode));
            _monitorSettingsFetchEvent.Response(item);
        }

        private void OnMonitorSettingsChangedHDREvent(MonitorHDR item)
        {
            mcMode mcMode = new mcMode();
            mcMode.mode = (byte)item.Mode;
            MWSetImpl(Wrapper(item.Device), "Monitor_HDR", mcMode);
        }

        private void OnMonitorSettingsFetchedHDREvent(MonitorHDR item)
        {
            mcMode data = new mcMode();
            MWGetImpl(Wrapper(item.Device), "Monitor_HDR", ref data);
            item.Mode = (MonitorHDRMode)data.mode;
            _monitorSettingsFetchEvent.Response(item);
        }

        private void OnMonitorSettingsChangedMotionBlurEvent(MonitorMotionBlur item)
        {
            mcMode mcMode = new mcMode();
            mcMode.mode = (byte)((item.State != 0) ? 1u : 0u);
            MWSetImpl(Wrapper(item.Device), "Monitor_MotionBlur", mcMode);
        }

        private void OnMonitorSettingsChangedInputSourceEvent(MonitorInputSource item)
        {
            if (item.Source == MonitorPiPSource.Invalid)
            {
                mcMode mcMode = new mcMode();
                mcMode.mode = (byte)((item.Auto != 0) ? 1u : 0u);
                MWSetImpl(Wrapper(item.Device), "Monitor_InputAutoSwitch", mcMode);
                return;
            }
            mcMode mcMode2 = new mcMode();
            mcMode2.mode = 0;
            MWSetImpl(Wrapper(item.Device), "Monitor_InputAutoSwitch", mcMode2);
            mcMode mcMode3 = new mcMode();
            mcMode3.mode = (byte)item.Source;
            MWSetImpl(Wrapper(item.Device), "Monitor_InputSource", mcMode3);
        }

        private void OnMonitorSettingsFetchedInputSourceEvent(MonitorInputSource item)
        {
            mcMode data = new mcMode();
            MWGetImpl(Wrapper(item.Device), "Monitor_InputAutoSwitch", ref data);
            item.Auto = ((data.mode != 0) ? 1u : 0u);
            mcMode data2 = new mcMode();
            MWGetImpl(Wrapper(item.Device), "Monitor_InputSource", ref data2);
            item.Source = (MonitorPiPSource)data2.mode;
            _monitorSettingsFetchEvent.Response(item);
        }

        private void OnMonitorSettingsChangedInputAutoSwitchEvent(MonitorInputAutoSwitch item)
        {
            mcMode mcMode = new mcMode();
            mcMode.mode = (byte)item.Mode;
            MWSetImpl(Wrapper(item.Device), "Monitor_InputAutoSwitch", mcMode);
        }

        private void OnMonitorSettingsFetchedInputAutoSwitchEvent(MonitorInputAutoSwitch item)
        {
            mcMode data = new mcMode();
            MWGetImpl(Wrapper(item.Device), "Monitor_InputAutoSwitch", ref data);
            item.Mode = (MonitorInputAutoSwitchMode)data.mode;
            _monitorSettingsFetchEvent.Response(item);
        }

        private void OnMonitorSettingsChangedGammaEvent(MonitorGamma item)
        {
            mcMode mcMode = new mcMode();
            mcMode.mode = (byte)item.Mode;
            MWSetImpl(Wrapper(item.Device), "Monitor_Gamma", mcMode);
        }

        private void OnMonitorSettingsChangedDeviceModeEvent(MonitorDeviceMode item)
        {
            mcDeviceMode mcDeviceMode = new mcDeviceMode();
            mcDeviceMode.deviceMode = (uint)item.Mode;
            MWSetImpl(Wrapper(item.Device), "Monitor_DeviceMode", mcDeviceMode);
        }

        private bool MWSetImpl<T>(RzCtl_ManagedWrapper wrapper, string paramId, T data)
        {
            lock (_lock)
            {
                if (wrapper == null)
                {
                    return false;
                }
                object paramValue = data;
                if (!wrapper.SetParamValue(paramId, ref paramValue))
                {
                    Trace.TraceError("MWSetImpl: Failed for paramId: " + paramId);
                    return false;
                }
                return true;
            }
        }

        private bool MWGetImpl<T>(RzCtl_ManagedWrapper wrapper, string paramId, ref T data)
        {
            lock (_lock)
            {
                if (wrapper == null)
                {
                    return false;
                }
                object paramValue = data;
                if (!wrapper.GetParamValue(paramId, ref paramValue))
                {
                    return false;
                }
                return true;
            }
        }
    }
}
