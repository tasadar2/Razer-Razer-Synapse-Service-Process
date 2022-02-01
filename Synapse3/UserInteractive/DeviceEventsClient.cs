#define TRACE
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;
using Contract.Audio.ApplicationStreamsLib;
using Contract.Common;
using Contract.MonitorLib;
using Microsoft.AspNet.SignalR.Client;

namespace Synapse3.UserInteractive
{
    public class DeviceEventsClient : IApplicationStreamsEvent, IMessageEvent, IMonitorSettingsChangedCallback, IMonitorSettingsFetchedCallback
    {
        private HubConnectionHelper _hub;

        private IHubProxy _hubProx;

        private Timer _connectionTimer;

        public event OnApplicationStreamDevice OnApplicationStreamsDeviceAddedEvent;

        public event OnApplicationStreamDevice OnApplicationStreamsDeviceRemovedEvent;

        public event OnApplicationStreamDevice OnApplicationStreamsDeviceGetStreamsEvent;

        public event OnApplicationStreamsSet OnApplicationStreamsSetEvent;

        public event OnApplicationStreamSet OnApplicationStreamSetEvent;

        public event OnMessage OnMessageEvent;

        public event OnMonitorSettingsChangedBrightness OnMonitorSettingsChangedBrightnessEvent;

        public event OnMonitorSettingsChangedContrast OnMonitorSettingsChangedContrastEvent;

        public event OnMonitorSettingsChangedColorPreset OnMonitorSettingsChangedColorPresetEvent;

        public event OnMonitorSettingsChangedDisplayMode OnMonitorSettingsChangedDisplayModeEvent;

        public event OnMonitorSettingsChangedTHXMode OnMonitorSettingsChangedTHXModeEvent;

        public event OnMonitorSettingsChangedColorGamut OnMonitorSettingsChangedColorGamutEvent;

        public event OnMonitorSettingsChangedFreeSync OnMonitorSettingsChangedFreeSyncEvent;

        public event OnMonitorSettingsFetchedFreeSync OnMonitorSettingsFetchedFreeSyncEvent;

        public event OnMonitorSettingsChangedImageScaling OnMonitorSettingsChangedImageScalingEvent;

        public event OnMonitorSettingsChangedOverdrive OnMonitorSettingsChangedOverdriveEvent;

        public event OnMonitorSettingsChangedPiPSettings OnMonitorSettingsChangedPiPSettingsEvent;

        public event OnMonitorSettingsFetchedPiPSettings OnMonitorSettingsFetchedPiPSettingsEvent;

        public event OnMonitorSettingsChangedFPS OnMonitorSettingsChangedFPSEvent;

        public event OnMonitorSettingsFetchedFPS OnMonitorSettingsFetchedFPSEvent;

        public event OnMonitorSettingsChangedHDR OnMonitorSettingsChangedHDREvent;

        public event OnMonitorSettingsFetchedHDR OnMonitorSettingsFetchedHDREvent;

        public event OnMonitorSettingsChangedMotionBlur OnMonitorSettingsChangedMotionBlurEvent;

        public event OnMonitorSettingsChangedInputSource OnMonitorSettingsChangedInputSourceEvent;

        public event OnMonitorSettingsFetchedInputSource OnMonitorSettingsFetchedInputSourceEvent;

        public event OnMonitorSettingsChangedInputAutoSwitch OnMonitorSettingsChangedInputAutoSwitchEvent;

        public event OnMonitorSettingsFetchedInputAutoSwitch OnMonitorSettingsFetchedInputAutoSwitchEvent;

        public event OnMonitorSettingsChangedGamma OnMonitorSettingsChangedGammaEvent;

        public event OnMonitorSettingsChangedDeviceMode OnMonitorSettingsChangedDeviceModeEvent;

        public event OnMonitorSettingsChangedWindowsHDR OnMonitorSettingsChangedWindowsHDREvent;

        public event OnMonitorSettingsFetchedWindowsHDR OnMonitorSettingsFetchedWindowsHDREvent;

        public event OnMonitorSettingsChangedICCProfiles OnMonitorSettingsChangedICCProfilesEvent;

        public event OnMonitorSettingsFetchedICCProfiles OnMonitorSettingsFetchedICCProfilesEvent;

        public event OnMonitorSettingsChangedRefreshRate OnMonitorSettingsChangedRefreshRateEvent;

        public event OnMonitorSettingsFetchedRefreshRate OnMonitorSettingsFetchedRefreshRateEvent;

        public event OnMonitorSettingsFetchedFWVersion OnMonitorSettingsFetchedFWVersionEvent;

        public DeviceEventsClient()
        {
            _connectionTimer = new Timer();
            _connectionTimer.AutoReset = false;
            _connectionTimer.Interval = 5000.0;
            _connectionTimer.Elapsed += ConnectionTimerHandler;
        }

        private async void ConnectionTimerHandler(object sender, ElapsedEventArgs e)
        {
            if (await InitConnection())
            {
                Trace.TraceInformation("DeviceEventsClient: Reconnected");
            }
        }

        private void ResetConnectionTimer()
        {
            Trace.TraceInformation("DeviceEventsClient: ResetConnectionTimer");
            _connectionTimer?.Stop();
            _connectionTimer.Start();
        }

        public async Task<bool> InitConnection()
        {
            _hub = new HubConnectionHelper();
            _hubProx = _hub.Connection.CreateHubProxy("DeviceEventsHub");
            _hubProx.On("OnApplicationStreamsDeviceAdded", delegate(Device device)
            {
                this.OnApplicationStreamsDeviceAddedEvent?.Invoke(device);
            });
            _hubProx.On("OnApplicationStreamsDeviceRemoved", delegate(Device device)
            {
                this.OnApplicationStreamsDeviceRemovedEvent?.Invoke(device);
            });
            _hubProx.On("OnApplicationStreamsDeviceGetStreams", delegate(Device device)
            {
                this.OnApplicationStreamsDeviceGetStreamsEvent?.Invoke(device);
            });
            _hubProx.On("OnApplicationStreamsSet", delegate(ApplicationStreams streams)
            {
                this.OnApplicationStreamsSetEvent?.Invoke(streams);
            });
            _hubProx.On("OnApplicationStreamSet", delegate(Device device, ApplicationStream stream)
            {
                this.OnApplicationStreamSetEvent?.Invoke(device, stream);
            });
            _hubProx.On("OnMonitorSettingsChangedBrightness", delegate(MonitorBrightness brightness)
            {
                this.OnMonitorSettingsChangedBrightnessEvent?.Invoke(brightness);
            });
            _hubProx.On("OnMonitorSettingsChangedContrast", delegate(MonitorContrast contrast)
            {
                this.OnMonitorSettingsChangedContrastEvent?.Invoke(contrast);
            });
            _hubProx.On("OnMonitorSettingsChangedColorPreset", delegate(MonitorColorPreset colorPreset)
            {
                this.OnMonitorSettingsChangedColorPresetEvent?.Invoke(colorPreset);
            });
            _hubProx.On("OnMonitorSettingsChangedDisplayMode", delegate(MonitorDisplayMode displayMode)
            {
                this.OnMonitorSettingsChangedDisplayModeEvent?.Invoke(displayMode);
            });
            _hubProx.On("OnMonitorSettingsChangedTHXMode", delegate(MonitorTHXMode thxMode)
            {
                this.OnMonitorSettingsChangedTHXModeEvent?.Invoke(thxMode);
            });
            _hubProx.On("OnMonitorSettingsChangedColorGamut", delegate(MonitorColorGamut colorGamut)
            {
                this.OnMonitorSettingsChangedColorGamutEvent?.Invoke(colorGamut);
            });
            _hubProx.On("OnMonitorSettingsChangedFreeSync", delegate(MonitorFreeSync freeSync)
            {
                this.OnMonitorSettingsChangedFreeSyncEvent?.Invoke(freeSync);
            });
            _hubProx.On("OnMonitorSettingsFetchedFreeSync", delegate(MonitorFreeSync freeSync)
            {
                this.OnMonitorSettingsFetchedFreeSyncEvent?.Invoke(freeSync);
            });
            _hubProx.On("OnMonitorSettingsChangedImageScaling", delegate(MonitorImageScaling imageScaling)
            {
                this.OnMonitorSettingsChangedImageScalingEvent?.Invoke(imageScaling);
            });
            _hubProx.On("OnMonitorSettingsChangedOverdrive", delegate(MonitorOverdrive overdrive)
            {
                this.OnMonitorSettingsChangedOverdriveEvent?.Invoke(overdrive);
            });
            _hubProx.On("OnMonitorSettingsChangedPiPSettings", delegate(MonitorPiPSettings pipSettings)
            {
                this.OnMonitorSettingsChangedPiPSettingsEvent?.Invoke(pipSettings);
            });
            _hubProx.On("OnMonitorSettingsFetchedPiPSettings", delegate(MonitorPiPSettings pipSettings)
            {
                this.OnMonitorSettingsFetchedPiPSettingsEvent?.Invoke(pipSettings);
            });
            _hubProx.On("OnMonitorSettingsChangedFPS", delegate(MonitorFPS fps)
            {
                this.OnMonitorSettingsChangedFPSEvent?.Invoke(fps);
            });
            _hubProx.On("OnMonitorSettingsFetchedFPS", delegate(MonitorFPS fps)
            {
                this.OnMonitorSettingsFetchedFPSEvent?.Invoke(fps);
            });
            _hubProx.On("OnMonitorSettingsChangedHDR", delegate(MonitorHDR hdr)
            {
                this.OnMonitorSettingsChangedHDREvent?.Invoke(hdr);
            });
            _hubProx.On("OnMonitorSettingsFetchedHDR", delegate(MonitorHDR hdr)
            {
                this.OnMonitorSettingsFetchedHDREvent?.Invoke(hdr);
            });
            _hubProx.On("OnMonitorSettingsChangedMotionBlur", delegate(MonitorMotionBlur motionblur)
            {
                this.OnMonitorSettingsChangedMotionBlurEvent?.Invoke(motionblur);
            });
            _hubProx.On("OnMonitorSettingsChangedInputSource", delegate(MonitorInputSource inputsource)
            {
                this.OnMonitorSettingsChangedInputSourceEvent?.Invoke(inputsource);
            });
            _hubProx.On("OnMonitorSettingsFetchedInputSource", delegate(MonitorInputSource inputsource)
            {
                this.OnMonitorSettingsFetchedInputSourceEvent?.Invoke(inputsource);
            });
            _hubProx.On("OnMonitorSettingsChangedInputAutoSwitch", delegate(MonitorInputAutoSwitch inputautoswitch)
            {
                this.OnMonitorSettingsChangedInputAutoSwitchEvent?.Invoke(inputautoswitch);
            });
            _hubProx.On("OnMonitorSettingsFetchedInputAutoSwitch", delegate(MonitorInputAutoSwitch inputautoswitch)
            {
                this.OnMonitorSettingsFetchedInputAutoSwitchEvent?.Invoke(inputautoswitch);
            });
            _hubProx.On("OnMonitorSettingsChangedGamma", delegate(MonitorGamma gamma)
            {
                this.OnMonitorSettingsChangedGammaEvent?.Invoke(gamma);
            });
            _hubProx.On("OnMonitorSettingsChangedDeviceMode", delegate(MonitorDeviceMode devicemode)
            {
                this.OnMonitorSettingsChangedDeviceModeEvent?.Invoke(devicemode);
            });
            _hubProx.On("OnMonitorSettingsChangedICCProfiles", delegate(MonitorICCProfiles iccProfiles)
            {
                this.OnMonitorSettingsChangedICCProfilesEvent?.Invoke(iccProfiles);
            });
            _hubProx.On("OnMonitorSettingsFetchedICCProfiles", delegate(MonitorICCProfiles iccProfiles)
            {
                this.OnMonitorSettingsFetchedICCProfilesEvent?.Invoke(iccProfiles);
            });
            _hubProx.On("OnMonitorSettingsChangedRefreshRate", delegate(Contract.MonitorLib.MonitorRefreshRate refreshRate)
            {
                this.OnMonitorSettingsChangedRefreshRateEvent?.Invoke(refreshRate);
            });
            _hubProx.On("OnMonitorSettingsFetchedRefreshRate", delegate(Contract.MonitorLib.MonitorRefreshRate refreshRate)
            {
                this.OnMonitorSettingsFetchedRefreshRateEvent?.Invoke(refreshRate);
            });
            _hubProx.On("OnMonitorSettingsChangedWindowsHDR", delegate(MonitorWindowsHDR windowshdr)
            {
                this.OnMonitorSettingsChangedWindowsHDREvent?.Invoke(windowshdr);
            });
            _hubProx.On("OnMonitorSettingsFetchedWindowsHDR", delegate(MonitorWindowsHDR windowshdr)
            {
                this.OnMonitorSettingsFetchedWindowsHDREvent?.Invoke(windowshdr);
            });
            _hubProx.On("OnMessage", delegate(string msg)
            {
                this.OnMessageEvent?.Invoke(msg);
            });
            _hubProx.On("OnMonitorSettingsFetchedFWVersion", delegate(MonitorDeviceInfo deviceInfo)
            {
                this.OnMonitorSettingsFetchedFWVersionEvent?.Invoke(deviceInfo);
            });
            try
            {
                await _hub.Connection.Start();
            }
            catch (Exception ex)
            {
                Trace.TraceError($"InitConnection: {ex?.Message}");
            }
            if (_hub.Connection.State == ConnectionState.Connected)
            {
                _hub.Connection.Closed += Connection_Closed;
                _hub.Connection.StateChanged += Connection_StateChanged;
                return true;
            }
            ResetConnectionTimer();
            return false;
        }

        private void Connection_StateChanged(StateChange obj)
        {
            Trace.TraceInformation($"DeviceEventsClient: old {obj.OldState} new {obj.NewState}");
        }

        private void Connection_Closed()
        {
            Trace.TraceInformation("DeviceEventsClient: Disconnected, retrying to reconnect...");
            ResetConnectionTimer();
        }

        public void Response(MonitorFreeSync item)
        {
            try
            {
                if (_hubProx != null && _hub != null && _hub.Connection.State == ConnectionState.Connected)
                {
                    _hubProx.Invoke("ResponseMonitorFreeSync", item);
                }
            }
            catch (Exception)
            {
            }
        }

        public void Response(MonitorPiPSettings item)
        {
            try
            {
                if (_hubProx != null && _hub != null && _hub.Connection.State == ConnectionState.Connected)
                {
                    _hubProx.Invoke("ResponseMonitorPiPSettings", item);
                }
            }
            catch (Exception)
            {
            }
        }

        public void Response(MonitorFPS item)
        {
            try
            {
                if (_hubProx != null && _hub != null && _hub.Connection.State == ConnectionState.Connected)
                {
                    _hubProx.Invoke("ResponseMonitorFPS", item);
                }
            }
            catch (Exception)
            {
            }
        }

        public void Response(MonitorHDR item)
        {
            try
            {
                if (_hubProx != null && _hub != null && _hub.Connection.State == ConnectionState.Connected)
                {
                    _hubProx.Invoke("ResponseMonitorHDR", item);
                }
            }
            catch (Exception)
            {
            }
        }

        public void Response(MonitorWindowsHDR item)
        {
            try
            {
                if (_hubProx != null && _hub != null && _hub.Connection.State == ConnectionState.Connected)
                {
                    _hubProx.Invoke("ResponseMonitorWindowsHDR", item);
                }
            }
            catch (Exception)
            {
            }
        }

        public void Response(MonitorInputSource item)
        {
            try
            {
                if (_hubProx != null && _hub != null && _hub.Connection.State == ConnectionState.Connected)
                {
                    _hubProx.Invoke("ResponseMonitorInputSource", item);
                }
            }
            catch (Exception)
            {
            }
        }

        public void Response(MonitorInputAutoSwitch item)
        {
            try
            {
                if (_hubProx != null && _hub != null && _hub.Connection.State == ConnectionState.Connected)
                {
                    _hubProx.Invoke("ResponseMonitorInputAutoSwitch", item);
                }
            }
            catch (Exception)
            {
            }
        }

        public void Response(MonitorICCProfiles item)
        {
            try
            {
                if (_hubProx != null && _hub != null && _hub.Connection.State == ConnectionState.Connected)
                {
                    _hubProx.Invoke("ResponseMonitorICCProfiles", item);
                }
            }
            catch (Exception)
            {
            }
        }

        public void Response(Contract.MonitorLib.MonitorRefreshRate item)
        {
            try
            {
                if (_hubProx != null && _hub != null && _hub.Connection.State == ConnectionState.Connected)
                {
                    _hubProx.Invoke("ResponseMonitorRefreshRate", item);
                }
            }
            catch (Exception)
            {
            }
        }

        public void Response(MonitorDeviceInfo item)
        {
            try
            {
                if (_hubProx != null && _hub != null && _hub.Connection.State == ConnectionState.Connected)
                {
                    _hubProx.Invoke("ResponseMonitorDeviceInfo", item);
                }
            }
            catch (Exception)
            {
            }
        }

        public void MonitorSettingsChangedCallback(Device device, int message, int param)
        {
            try
            {
                if (_hubProx != null && _hub != null && _hub.Connection.State == ConnectionState.Connected)
                {
                    _hubProx.Invoke("OnMonitorSettingsChanged", device, message, param);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
