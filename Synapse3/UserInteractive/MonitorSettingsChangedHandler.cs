using System;
using System.Collections.Generic;
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

        private Dictionary<long, Device> _devices;

        private readonly IMonitorSettingsChangedCallback _monitorSettingsEvent;

        private readonly IMonitorSettingsFetchedCallback _monitorSettingsFetchEvent;

        private readonly IDeviceDetection _deviceDetection;

        private readonly IWndProc _wndProc;

        private readonly object _lock;

        private System.Timers.Timer _refreshHandleTimer;

        public MonitorSettingsChangedHandler(IDeviceDetection deviceDetection, IMonitorSettingsChangedCallback monitorSettingsEvent, IMonitorSettingsFetchedCallback monitorSettingsFetchEvent, IWndProc wndProc)
        {
            _wrappers = new Dictionary<long, RzCtl_ManagedWrapper>();
            _devices = new Dictionary<long, Device>();
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
            _monitorSettingsEvent.OnMonitorSettingsChangedTHXModeEvent += OnMonitorSettingsChangedTHXModeEvent;
            _monitorSettingsEvent.OnMonitorSettingsChangedColorGamutEvent += OnMonitorSettingsChangedColorGamutEvent;
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
            _monitorSettingsEvent.OnMonitorSettingsChangedWindowsHDREvent += OnMonitorSettingsChangedWindowsHDREvent;
            _monitorSettingsEvent.OnMonitorSettingsChangedICCProfilesEvent += OnMonitorSettingsChangedICCProfilesEvent;
            _monitorSettingsEvent.OnMonitorSettingsChangedRefreshRateEvent += OnMonitorSettingsChangedRefreshRateEvent;
            _monitorSettingsFetchEvent.OnMonitorSettingsFetchedFreeSyncEvent += OnMonitorSettingsFetchedFreeSyncEvent;
            _monitorSettingsFetchEvent.OnMonitorSettingsFetchedPiPSettingsEvent += OnMonitorSettingsFetchedPiPSettingsEvent;
            _monitorSettingsFetchEvent.OnMonitorSettingsFetchedFPSEvent += OnMonitorSettingsFetchedFPSEvent;
            _monitorSettingsFetchEvent.OnMonitorSettingsFetchedHDREvent += OnMonitorSettingsFetchedHDREvent;
            _monitorSettingsFetchEvent.OnMonitorSettingsFetchedInputSourceEvent += OnMonitorSettingsFetchedInputSourceEvent;
            _monitorSettingsFetchEvent.OnMonitorSettingsFetchedInputAutoSwitchEvent += OnMonitorSettingsFetchedInputAutoSwitchEvent;
            _monitorSettingsFetchEvent.OnMonitorSettingsFetchedWindowsHDREvent += OnMonitorSettingsFetchedWindowsHDREvent;
            _monitorSettingsFetchEvent.OnMonitorSettingsFetchedICCProfilesEvent += OnMonitorSettingsFetchedICCProfilesEvent;
            _monitorSettingsFetchEvent.OnMonitorSettingsFetchedRefreshRateEvent += OnMonitorSettingsFetchedRefreshRateEvent;
            _monitorSettingsFetchEvent.OnMonitorSettingsFetchedFWVersionEvent += OnMonitorSettingsFetchedFWVersionEvent;
            _wndProc.OnWndProcEvent += OnWndProcEvent;
            _refreshHandleTimer = new System.Timers.Timer();
            _refreshHandleTimer.AutoReset = false;
            _refreshHandleTimer.Interval = 2000.0;
            _refreshHandleTimer.Elapsed += HandleRefreshHandler;
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
        }

        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            PowerModes mode = e.Mode;
            if (mode != PowerModes.Suspend)
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
                Logger.Instance.Debug("SystemEvents_PowerModeChanged: middleware wrappers cleared.");
                _wrappers.Clear();
                _devices.Clear();
            }
        }

        private void ResetHandleTimer()
        {
            Logger.Instance.Debug($"ResetHandleTimer: HandleRefreshHandler will execute on {DateTime.Now.AddMilliseconds(_refreshHandleTimer.Interval)}");
            _refreshHandleTimer.Stop();
            _refreshHandleTimer.Start();
        }

        private void HandleRefreshHandler(object sender, ElapsedEventArgs e)
        {
            Logger.Instance.Debug($"HandleRefreshHandler: Refreshing Handles count: {_wrappers.Count}");
            try
            {
                lock (_lock)
                {
                    foreach (KeyValuePair<long, RzCtl_ManagedWrapper> wrapper in _wrappers)
                    {
                        if (wrapper.Value != null)
                        {
                            bool flag = MWSetImpl(wrapper.Value, "Monitor_Reset", data: true);
                        }
                    }
                }
            }
            catch (Exception arg)
            {
                Logger.Instance.Error($"HandleRefreshHandler: Exception: {arg}");
            }
            Logger.Instance.Debug("HandleRefreshHandler: Refresh done.");
        }

        private void OnWndProcEvent(Message message)
        {
            if (message.Msg == 26 && message.WParam.ToInt32() == 47)
            {
                Logger.Instance.Debug("OnWndProcEvent: Received WM_SETTINGCHANGE & SPI_SETWORKAREA, Refreshing handles.");
                ResetHandleTimer();
            }
        }

        private void OnDeviceAddedEvent(Device item)
        {
            if (item.Type == 15)
            {
                RzCtl_ManagedWrapper rzCtl_ManagedWrapper = Wrapper(item);
            }
        }

        private void OnDeviceRemovedEvent(Device item)
        {
            if (item.Type == 15)
            {
                RzCtl_ManagedWrapper rzCtl_ManagedWrapper = Wrapper(item, bCleanUp: true);
            }
        }

        private RzCtl_ManagedWrapper Wrapper(Device device, bool bCleanUp = false)
        {
            lock (_lock)
            {
                Logger.Instance.Debug($"Wrapper: Enter cleanup? {bCleanUp}");
                if (bCleanUp)
                {
                    if (_wrappers.ContainsKey(device.Handle))
                    {
                        if (_devices.ContainsKey(device.Handle))
                        {
                            _devices.Remove(device.Handle);
                        }
                        _wrappers[device.Handle].Dispose();
                        _wrappers.Remove(device.Handle);
                        Logger.Instance.Debug($"Wrapper: {device.Product_ID} removed from collection.");
                        return null;
                    }
                    Logger.Instance.Warn($"Wrapper: mw for {device.Product_ID} not found. Cleanup failed.");
                    return null;
                }
                if (!_wrappers.ContainsKey(device.Handle))
                {
                    _wrappers[device.Handle] = new RzCtl_ManagedWrapper((ushort)device.Vendor_ID, (ushort)device.Product_ID, (ulong)device.Handle, Constants.RAZER_BIN + "\\Devices\\Mw\\");
                    _devices[device.Handle] = device;
                    if (!_wrappers[device.Handle].Initialise())
                    {
                        _wrappers.Remove(device.Handle);
                        Logger.Instance.Error($"Wrapper: Initialise failed {device.Product_ID}. Undoing add.");
                    }
                    else
                    {
                        Logger.Instance.Debug($"Wrapper: Added {device.Product_ID} to collection.");
                    }
                }
                if (_wrappers.ContainsKey(device.Handle))
                {
                    Logger.Instance.Debug($"Wrapper: Found mw instance for {device.Product_ID} from collection. Returning mw instance.");
                    return _wrappers[device.Handle];
                }
                Logger.Instance.Error($"Wrapper: mw instance for {device.Product_ID} now found in collection. Returning null.");
                return null;
            }
        }

        private void OnMonitorSettingsChangedBrightnessEvent(MonitorBrightness item)
        {
            ulong data = item.Brightness;
            if (!MWSetImpl(Wrapper(item.Device), "Monitor_Brightness", data) && RefreshMWWrapper(item.Device))
            {
                bool flag = MWSetImpl(Wrapper(item.Device), "Monitor_Brightness", data);
            }
        }

        private void OnMonitorSettingsChangedContrastEvent(MonitorContrast item)
        {
            ulong data = item.Contrast;
            if (!MWSetImpl(Wrapper(item.Device), "Monitor_Contrast", data) && RefreshMWWrapper(item.Device))
            {
                bool flag = MWSetImpl(Wrapper(item.Device), "Monitor_Contrast", data);
            }
        }

        private void OnMonitorSettingsChangedColorPresetEvent(MonitorColorPreset item)
        {
            if (item.Mode != MonitorColorPresetMode.Invalid)
            {
                mcMode mcMode = new mcMode();
                mcMode.mode = (byte)item.Mode;
                if (!MWSetImpl(Wrapper(item.Device), "Monitor_ColorPreset", mcMode) && RefreshMWWrapper(item.Device))
                {
                    bool flag = MWSetImpl(Wrapper(item.Device), "Monitor_ColorPreset", mcMode);
                }
            }
            if (item.Mode == MonitorColorPresetMode.Custom)
            {
                mcLedColor mcLedColor = new mcLedColor();
                mcLedColor.color.red = item.VideoGain.Red;
                mcLedColor.color.blue = item.VideoGain.Blue;
                mcLedColor.color.green = item.VideoGain.Green;
                if (!MWSetImpl(Wrapper(item.Device), "Monitor_VideoGain", mcLedColor) && RefreshMWWrapper(item.Device))
                {
                    bool flag2 = MWSetImpl(Wrapper(item.Device), "Monitor_VideoGain", mcLedColor);
                }
            }
        }

        private void OnMonitorSettingsChangedDisplayModeEvent(MonitorDisplayMode item)
        {
            mcMode mcMode = new mcMode();
            mcMode.mode = (byte)item.Mode;
            if (!MWSetImpl(Wrapper(item.Device), "Monitor_DisplayMode", mcMode) && RefreshMWWrapper(item.Device))
            {
                bool flag = MWSetImpl(Wrapper(item.Device), "Monitor_DisplayMode", mcMode);
            }
        }

        private void OnMonitorSettingsChangedTHXModeEvent(MonitorTHXMode item)
        {
            mcMode mcMode = new mcMode();
            mcMode.mode = (byte)item.Mode;
            if (!MWSetImpl(Wrapper(item.Device), "Monitor_THXMode", mcMode) && RefreshMWWrapper(item.Device))
            {
                bool flag = MWSetImpl(Wrapper(item.Device), "Monitor_THXMode", mcMode);
            }
        }

        private void OnMonitorSettingsChangedColorGamutEvent(MonitorColorGamut item)
        {
            mcMode mcMode = new mcMode();
            mcMode.mode = (byte)item.Mode;
            if (!MWSetImpl(Wrapper(item.Device), "Monitor_ColorGamut", mcMode) && RefreshMWWrapper(item.Device))
            {
                bool flag = MWSetImpl(Wrapper(item.Device), "Monitor_ColorGamut", mcMode);
            }
        }

        private void OnMonitorSettingsChangedFreeSyncEvent(MonitorFreeSync item)
        {
            mcMode mcMode = new mcMode();
            mcMode.mode = (byte)item.Mode;
            if (!MWSetImpl(Wrapper(item.Device), "Monitor_FreeSync", mcMode) && RefreshMWWrapper(item.Device))
            {
                bool flag = MWSetImpl(Wrapper(item.Device), "Monitor_FreeSync", mcMode);
            }
        }

        private void OnMonitorSettingsFetchedFreeSyncEvent(MonitorFreeSync item)
        {
            mcMode data = new mcMode();
            if (!MWGetImpl(Wrapper(item.Device), "Monitor_FreeSync", ref data) && RefreshMWWrapper(item.Device))
            {
                bool flag = MWGetImpl(Wrapper(item.Device), "Monitor_FreeSync", ref data);
            }
            item.Mode = (MonitorFreeSyncMode)data.mode;
            _monitorSettingsFetchEvent.Response(item);
        }

        private void OnMonitorSettingsChangedImageScalingEvent(MonitorImageScaling item)
        {
            mcMode mcMode = new mcMode();
            mcMode.mode = (byte)item.Mode;
            if (!MWSetImpl(Wrapper(item.Device), "Monitor_ImageScaling", mcMode) && RefreshMWWrapper(item.Device))
            {
                bool flag = MWSetImpl(Wrapper(item.Device), "Monitor_ImageScaling", mcMode);
            }
        }

        private void OnMonitorSettingsChangedOverdriveEvent(MonitorOverdrive item)
        {
            mcMode mcMode = new mcMode();
            mcMode.mode = (byte)item.Mode;
            if (!MWSetImpl(Wrapper(item.Device), "Monitor_Overdrive", mcMode) && RefreshMWWrapper(item.Device))
            {
                bool flag = MWSetImpl(Wrapper(item.Device), "Monitor_Overdrive", mcMode);
            }
        }

        private void OnMonitorSettingsChangedPiPSettingsEvent(MonitorPiPSettings item)
        {
            if (item.Mode != MonitorPiPMode.Invalid)
            {
                mcMode mcMode = new mcMode();
                mcMode.mode = (byte)item.Mode;
                if (!MWSetImpl(Wrapper(item.Device), "Monitor_PiPMode", mcMode) && RefreshMWWrapper(item.Device))
                {
                    bool flag = MWSetImpl(Wrapper(item.Device), "Monitor_PiPMode", mcMode);
                }
            }
            if (item.Source != MonitorPiPSource.Invalid)
            {
                mcMode mcMode2 = new mcMode();
                mcMode2.mode = (byte)item.Source;
                if (!MWSetImpl(Wrapper(item.Device), "Monitor_PiPSource", mcMode2) && RefreshMWWrapper(item.Device))
                {
                    bool flag2 = MWSetImpl(Wrapper(item.Device), "Monitor_PiPSource", mcMode2);
                }
            }
            if (item.Size != MonitorPiPSize.Invalid)
            {
                mcMode mcMode3 = new mcMode();
                mcMode3.mode = (byte)item.Size;
                if (!MWSetImpl(Wrapper(item.Device), "Monitor_PiPSize", mcMode3) && RefreshMWWrapper(item.Device))
                {
                    bool flag3 = MWSetImpl(Wrapper(item.Device), "Monitor_PiPSize", mcMode3);
                }
            }
            if (item.Position != MonitorPiPPosition.Invalid)
            {
                mcMode mcMode4 = new mcMode();
                mcMode4.mode = (byte)item.Position;
                if (!MWSetImpl(Wrapper(item.Device), "Monitor_PiPPosition", mcMode4) && RefreshMWWrapper(item.Device))
                {
                    bool flag4 = MWSetImpl(Wrapper(item.Device), "Monitor_PiPPosition", mcMode4);
                }
            }
        }

        private void OnMonitorSettingsFetchedPiPSettingsEvent(MonitorPiPSettings item)
        {
            mcMode data = new mcMode();
            if (!MWGetImpl(Wrapper(item.Device), "Monitor_PiPMode", ref data) && RefreshMWWrapper(item.Device))
            {
                bool flag = MWGetImpl(Wrapper(item.Device), "Monitor_PiPMode", ref data);
            }
            item.Mode = (MonitorPiPMode)data.mode;
            mcMode data2 = new mcMode();
            if (!MWGetImpl(Wrapper(item.Device), "Monitor_PiPSource", ref data2) && RefreshMWWrapper(item.Device))
            {
                bool flag2 = MWGetImpl(Wrapper(item.Device), "Monitor_PiPSource", ref data2);
            }
            item.Source = (MonitorPiPSource)data2.mode;
            mcMode data3 = new mcMode();
            if (!MWGetImpl(Wrapper(item.Device), "Monitor_PiPSize", ref data3) && RefreshMWWrapper(item.Device))
            {
                bool flag3 = MWGetImpl(Wrapper(item.Device), "Monitor_PiPSize", ref data3);
            }
            item.Size = (MonitorPiPSize)((data3.mode == 0) ? (-1) : ((int)data3.mode));
            mcMode data4 = new mcMode();
            if (!MWGetImpl(Wrapper(item.Device), "Monitor_PiPPosition", ref data4) && RefreshMWWrapper(item.Device))
            {
                bool flag4 = MWGetImpl(Wrapper(item.Device), "Monitor_PiPPosition", ref data4);
            }
            item.Position = (MonitorPiPPosition)data4.mode;
            _monitorSettingsFetchEvent.Response(item);
        }

        private void OnMonitorSettingsChangedFPSEvent(MonitorFPS item)
        {
            mcMode mcMode = new mcMode();
            mcMode.mode = (byte)((item.State != 0) ? ((byte)item.Position) : 0);
            if (!MWSetImpl(Wrapper(item.Device), "Monitor_FPSPosition", mcMode) && RefreshMWWrapper(item.Device))
            {
                bool flag = MWSetImpl(Wrapper(item.Device), "Monitor_FPSPosition", mcMode);
            }
        }

        private void OnMonitorSettingsFetchedFPSEvent(MonitorFPS item)
        {
            mcMode data = new mcMode();
            if (!MWGetImpl(Wrapper(item.Device), "Monitor_FPSPosition", ref data) && RefreshMWWrapper(item.Device))
            {
                bool flag = MWGetImpl(Wrapper(item.Device), "Monitor_FPSPosition", ref data);
            }
            item.State = ((data.mode != 0) ? 1u : 0u);
            item.Position = (MonitorFPSPosition)((data.mode == 0) ? (-1) : ((int)data.mode));
            _monitorSettingsFetchEvent.Response(item);
        }

        private void OnMonitorSettingsChangedHDREvent(MonitorHDR item)
        {
            mcMode mcMode = new mcMode();
            mcMode.mode = (byte)item.Mode;
            if (!MWSetImpl(Wrapper(item.Device), "Monitor_HDR", mcMode) && RefreshMWWrapper(item.Device))
            {
                bool flag = MWSetImpl(Wrapper(item.Device), "Monitor_HDR", mcMode);
            }
        }

        private void OnMonitorSettingsFetchedHDREvent(MonitorHDR item)
        {
            mcMode data = new mcMode();
            if (!MWGetImpl(Wrapper(item.Device), "Monitor_HDR", ref data) && RefreshMWWrapper(item.Device))
            {
                bool flag = MWGetImpl(Wrapper(item.Device), "Monitor_HDR", ref data);
            }
            item.Mode = (MonitorHDRMode)data.mode;
            _monitorSettingsFetchEvent.Response(item);
        }

        private void OnMonitorSettingsChangedWindowsHDREvent(MonitorWindowsHDR item)
        {
            mcMode mcMode = new mcMode();
            mcMode.mode = (byte)item.Mode;
            if (!MWSetImpl(Wrapper(item.Device), "Monitor_WindowsHDR", mcMode) && RefreshMWWrapper(item.Device))
            {
                bool flag = MWSetImpl(Wrapper(item.Device), "Monitor_WindowsHDR", mcMode);
            }
        }

        private void OnMonitorSettingsFetchedWindowsHDREvent(MonitorWindowsHDR item)
        {
            mcMode data = new mcMode();
            if (!MWGetImpl(Wrapper(item.Device), "Monitor_WindowsHDR", ref data) && RefreshMWWrapper(item.Device))
            {
                bool flag = MWGetImpl(Wrapper(item.Device), "Monitor_WindowsHDR", ref data);
            }
            item.Mode = (MonitorWindowsHDRMode)data.mode;
            _monitorSettingsFetchEvent.Response(item);
        }

        private void OnMonitorSettingsChangedMotionBlurEvent(MonitorMotionBlur item)
        {
            mcMode mcMode = new mcMode();
            mcMode.mode = (byte)((item.State != 0) ? 1 : 0);
            if (!MWSetImpl(Wrapper(item.Device), "Monitor_MotionBlur", mcMode) && RefreshMWWrapper(item.Device))
            {
                bool flag = MWSetImpl(Wrapper(item.Device), "Monitor_MotionBlur", mcMode);
            }
        }

        private void OnMonitorSettingsChangedInputSourceEvent(MonitorInputSource item)
        {
            if (item.Source == MonitorPiPSource.Invalid)
            {
                mcMode mcMode = new mcMode();
                mcMode.mode = (byte)((item.Auto != 0) ? 1 : 0);
                if (!MWSetImpl(Wrapper(item.Device), "Monitor_InputAutoSwitch", mcMode) && RefreshMWWrapper(item.Device))
                {
                    bool flag = MWSetImpl(Wrapper(item.Device), "Monitor_InputAutoSwitch", mcMode);
                }
                return;
            }
            mcMode mcMode2 = new mcMode();
            mcMode2.mode = 0;
            if (!MWSetImpl(Wrapper(item.Device), "Monitor_InputAutoSwitch", mcMode2) && RefreshMWWrapper(item.Device))
            {
                bool flag2 = MWSetImpl(Wrapper(item.Device), "Monitor_InputAutoSwitch", mcMode2);
            }
            mcMode mcMode3 = new mcMode();
            mcMode3.mode = (byte)item.Source;
            if (!MWSetImpl(Wrapper(item.Device), "Monitor_InputSource", mcMode3) && RefreshMWWrapper(item.Device))
            {
                bool flag3 = MWSetImpl(Wrapper(item.Device), "Monitor_InputSource", mcMode3);
            }
        }

        private void OnMonitorSettingsFetchedInputSourceEvent(MonitorInputSource item)
        {
            mcMode data = new mcMode();
            if (!MWGetImpl(Wrapper(item.Device), "Monitor_InputAutoSwitch", ref data) && RefreshMWWrapper(item.Device))
            {
                bool flag = MWGetImpl(Wrapper(item.Device), "Monitor_InputAutoSwitch", ref data);
            }
            item.Auto = ((data.mode != 0) ? 1u : 0u);
            mcMode data2 = new mcMode();
            if (!MWGetImpl(Wrapper(item.Device), "Monitor_InputSource", ref data2) && RefreshMWWrapper(item.Device))
            {
                bool flag2 = MWGetImpl(Wrapper(item.Device), "Monitor_InputSource", ref data2);
            }
            item.Source = (MonitorPiPSource)data2.mode;
            _monitorSettingsFetchEvent.Response(item);
        }

        private void OnMonitorSettingsChangedInputAutoSwitchEvent(MonitorInputAutoSwitch item)
        {
            mcMode mcMode = new mcMode();
            mcMode.mode = (byte)item.Mode;
            if (!MWSetImpl(Wrapper(item.Device), "Monitor_InputAutoSwitch", mcMode) && RefreshMWWrapper(item.Device))
            {
                bool flag = MWSetImpl(Wrapper(item.Device), "Monitor_InputAutoSwitch", mcMode);
            }
        }

        private void OnMonitorSettingsFetchedInputAutoSwitchEvent(MonitorInputAutoSwitch item)
        {
            mcMode data = new mcMode();
            if (!MWGetImpl(Wrapper(item.Device), "Monitor_InputAutoSwitch", ref data) && RefreshMWWrapper(item.Device))
            {
                bool flag = MWGetImpl(Wrapper(item.Device), "Monitor_InputAutoSwitch", ref data);
            }
            item.Mode = (MonitorInputAutoSwitchMode)data.mode;
            _monitorSettingsFetchEvent.Response(item);
        }

        private void OnMonitorSettingsChangedGammaEvent(MonitorGamma item)
        {
            mcMode mcMode = new mcMode();
            mcMode.mode = (byte)item.Mode;
            if (!MWSetImpl(Wrapper(item.Device), "Monitor_Gamma", mcMode) && RefreshMWWrapper(item.Device))
            {
                bool flag = MWSetImpl(Wrapper(item.Device), "Monitor_Gamma", mcMode);
            }
        }

        private void OnMonitorSettingsChangedDeviceModeEvent(MonitorDeviceMode item)
        {
            mcDeviceMode mcDeviceMode = new mcDeviceMode();
            mcDeviceMode.deviceMode = (uint)item.Mode;
            if (!MWSetImpl(Wrapper(item.Device), "Monitor_DeviceMode", mcDeviceMode) && RefreshMWWrapper(item.Device))
            {
                bool flag = MWSetImpl(Wrapper(item.Device), "Monitor_DeviceMode", mcDeviceMode);
            }
        }

        private void OnMonitorSettingsChangedICCProfilesEvent(MonitorICCProfiles item)
        {
            if (item.ICCProfile != "")
            {
                mcParamString mcParamString = new mcParamString();
                mcParamString.String = item.ICCProfile;
                if (!MWSetImpl(Wrapper(item.Device), "Monitor_ColorProfile", mcParamString) && RefreshMWWrapper(item.Device))
                {
                    bool flag = MWSetImpl(Wrapper(item.Device), "Monitor_ColorProfile", mcParamString);
                }
            }
        }

        private void OnMonitorSettingsFetchedICCProfilesEvent(MonitorICCProfiles item)
        {
            mcParamString data = new mcParamString();
            if (!MWGetImpl(Wrapper(item.Device), "Monitor_ColorProfile", ref data) && RefreshMWWrapper(item.Device))
            {
                bool flag = MWGetImpl(Wrapper(item.Device), "Monitor_ColorProfile", ref data);
            }
            item.ICCProfile = data.String;
            mcColorProfileList data2 = new mcColorProfileList();
            if (!MWGetImpl(Wrapper(item.Device), "Monitor_EnumColorProfile", ref data2) && RefreshMWWrapper(item.Device))
            {
                bool flag2 = MWGetImpl(Wrapper(item.Device), "Monitor_EnumColorProfile", ref data2);
            }
            item.MonitorICCProfilesList = new List<string>();
            for (int i = 0; i < data2.wTotalCount; i++)
            {
                item.MonitorICCProfilesList.Add(data2.ColorProfileList[i]);
            }
            _monitorSettingsFetchEvent.Response(item);
        }

        private void OnMonitorSettingsChangedRefreshRateEvent(Contract.MonitorLib.MonitorRefreshRate item)
        {
            if (item.ActiveRefreshRate != 0)
            {
                ulong data = (ulong)item.ActiveRefreshRate;
                if (!MWSetImpl(Wrapper(item.Device), "Monitor_RefreshRate", data) && RefreshMWWrapper(item.Device))
                {
                    bool flag = MWSetImpl(Wrapper(item.Device), "Monitor_RefreshRate", data);
                }
            }
        }

        private void OnMonitorSettingsFetchedRefreshRateEvent(Contract.MonitorLib.MonitorRefreshRate item)
        {
            lock (_lock)
            {
                ulong num = 0uL;
                RzCtl_ManagedWrapper rzCtl_ManagedWrapper = Wrapper(item.Device);
                if (rzCtl_ManagedWrapper != null)
                {
                    object paramValue = num;
                    if (rzCtl_ManagedWrapper.GetParamValue("Monitor_RefreshRate", ref paramValue))
                    {
                        num = (ulong)paramValue;
                    }
                }
                item.ActiveRefreshRate = (int)num;
            }
            mcSupportedRefreshRate data = new mcSupportedRefreshRate();
            if (!MWGetImpl(Wrapper(item.Device), "Monitor_SupportedRefreshRate", ref data) && RefreshMWWrapper(item.Device))
            {
                bool flag = MWGetImpl(Wrapper(item.Device), "Monitor_SupportedRefreshRate", ref data);
            }
            item.RefreshRateList = new List<int>();
            for (int i = 0; i < data.wTotalCount; i++)
            {
                item.RefreshRateList.Add(data.RefreshRateList[i]);
            }
            _monitorSettingsFetchEvent.Response(item);
        }

        private void OnMonitorSettingsFetchedFWVersionEvent(MonitorDeviceInfo item)
        {
            lock (_lock)
            {
                RzCtl_ManagedWrapper rzCtl_ManagedWrapper = Wrapper(item.Device);
                if (rzCtl_ManagedWrapper != null)
                {
                    mcParamString mcParamString = new mcParamString();
                    object paramValue = mcParamString;
                    if (rzCtl_ManagedWrapper.GetParamValue("Monitor_FirmwareVersion", ref paramValue))
                    {
                        item.FirmwareVersion = ((mcParamString)paramValue).String;
                    }
                }
            }
            _monitorSettingsFetchEvent.Response(item);
        }

        private bool MWSetImpl<T>(RzCtl_ManagedWrapper wrapper, string paramId, T data)
        {
            lock (_lock)
            {
                Logger.Instance.Debug("MWSetImpl -Start-");
                Logger.Instance.Debug($"SET Info:: Device PID: {wrapper.PID}, Handle: {wrapper.Handle}, paramID: {paramId}");
                if (wrapper == null)
                {
                    return false;
                }
                object paramValue = data;
                if (!wrapper.SetParamValue(paramId, ref paramValue))
                {
                    Logger.Instance.Error($"MWSetImpl: Failed for paramId: {paramId}");
                    return false;
                }
                Logger.Instance.Debug("MWSetImpl -End-");
                return true;
            }
        }

        private bool MWGetImpl<T>(RzCtl_ManagedWrapper wrapper, string paramId, ref T data)
        {
            lock (_lock)
            {
                Logger.Instance.Debug("MWGetImpl -Start-");
                Logger.Instance.Debug($"GET Info:: Device PID: {wrapper.PID}, Handle: {wrapper.Handle}, paramID: {paramId}");
                if (wrapper == null)
                {
                    return false;
                }
                object paramValue = data;
                if (!wrapper.GetParamValue(paramId, ref paramValue))
                {
                    return false;
                }
                Logger.Instance.Debug("MWGetImpl -End-");
                return true;
            }
        }

        private bool RefreshMWWrapper(Device device)
        {
            bool flag = false;
            try
            {
                Logger.Instance.Debug("RefreshMWWrapper: Enter retry");
                if (_wrappers.ContainsKey(device.Handle))
                {
                    if (_devices.ContainsKey(device.Handle))
                    {
                        _devices.Remove(device.Handle);
                    }
                    _wrappers[device.Handle].Dispose();
                    _wrappers.Remove(device.Handle);
                }
                _wrappers[device.Handle] = new RzCtl_ManagedWrapper((ushort)device.Vendor_ID, (ushort)device.Product_ID, (ulong)device.Handle, Constants.RAZER_BIN + "\\Devices\\Mw\\");
                if (_wrappers[device.Handle] == null || !_wrappers[device.Handle].Initialise())
                {
                    flag = false;
                    if (_wrappers.ContainsKey(device.Handle))
                    {
                        _wrappers.Remove(device.Handle);
                    }
                    Logger.Instance.Error($"RefreshMWWrapper: Initialise failed {device.Product_ID}. Undoing add.");
                    return flag;
                }
                flag = true;
                _devices[device.Handle] = device;
                Logger.Instance.Debug($"RefreshMWWrapper: Added {device.Product_ID} to collection.");
                return flag;
            }
            catch (Exception arg)
            {
                flag = false;
                Logger.Instance.Error($"RefreshMWWrapper exception: {arg}");
                return flag;
            }
        }
    }
}
