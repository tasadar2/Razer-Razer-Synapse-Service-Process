#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Contract.Common;
using Contract.MacroLib;
using Microsoft.AspNet.SignalR.Client;

namespace Synapse3.UserInteractive
{
    public class ApplicationEventsClient : ISendLastInputInfo, IUserTextEvent, IUserAccelerationEvent, IGetForegroundWindowRectEvent, IScreenRefreshRateEvent, IOTFMacroRecorderEvent, ISetForegroundWindow, IDisplayChangedEvent, IApplicationNotificationEvent
    {
        private HubConnectionHelper _hub;

        private IHubProxy _hubProx;

        private object _threadLock = new object();

        private System.Timers.Timer _connectionTimer;

        private const string FanForceAutoMessage = "FanForceAutoMessage";

        private const string ResolutionMismatchMessage = "ResolutionMismatchMessage";

        public event OnUserTextInput OnUserTextInputEvent;

        public event OnUserAcceleration OnUserAccelerationEvent;

        public event OnGetForegroundWindowRect OnGetForegroundWindowRectEvent;

        public event OnGetScreenRefreshRate OnGetScreenRefreshRateEvent;

        public event OnSetScreenRefreshRate OnSetScreenRefreshRateEvent;

        public event OnGetScreenRefreshRateList OnGetScreenRefreshRateListEvent;

        public event OnMacroStartOTF StartOTFEvent;

        public event OnMacroStopOTF StopOTFEvent;

        public event OnMacroCancelOTF CancelOTFEvent;

        public event OnGetDisplaySetting GetDisplaySettingEvent;

        public event OnApplicationNotification OnApplicationNotificationEvent;

        public ApplicationEventsClient()
        {
            _connectionTimer = new System.Timers.Timer();
            _connectionTimer.AutoReset = false;
            _connectionTimer.Interval = 5000.0;
            _connectionTimer.Elapsed += ConnectionTimerHandler;
        }

        private async void ConnectionTimerHandler(object sender, ElapsedEventArgs e)
        {
            if (await InitConnection())
            {
                Trace.TraceInformation("ApplicationEventsClient: Reconnected");
            }
        }

        private void ResetConnectionTimer()
        {
            Trace.TraceInformation("ApplicationEventsClient: ResetConnectionTimer");
            _connectionTimer?.Stop();
            _connectionTimer.Start();
        }

        public async Task<bool> InitConnection()
        {
            _hub = new HubConnectionHelper();
            _hubProx = _hub.Connection.CreateHubProxy("ApplicationEventsHub");
            _hubProx.On("OnTextEvent", delegate(string s)
            {
                _OnTextEvent(s);
            });
            _hubProx.On("OnAccelerationEvent", delegate(uint a)
            {
                _OnAccelerationEvent(a);
            });
            _hubProx.On("OnGetForegroundWindowRect", _OnGetForegroundWindowRect);
            _hubProx.On("OnGetScreenRefreshRate", _OnGetScreenRefreshRate);
            _hubProx.On("OnSetScreenRefreshRate", delegate(int a)
            {
                _OnSetScreenRefreshRate(a);
            });
            _hubProx.On("OnGetScreenRefreshRateList", _OnGetScreenRefreshRateList);
            _hubProx.On("OnStartOTFMacro", delegate(Device d)
            {
                _OnMacroStartOTF(d);
            });
            _hubProx.On("OnStopOTFMacro", delegate(Device d)
            {
                _OnMacroStopOTF(d);
            });
            _hubProx.On("OnCancelOTFMacro", delegate(Device d)
            {
                _OnMacroCancelOTF(d);
            });
            _hubProx.On("OnGetDisplaySetting", _OnGetDisplaySetting);
            _hubProx.On("OnApplicationNotify", delegate(string s)
            {
                _OnApplicationNotify(s);
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
            Trace.TraceInformation($"ApplicationEventsClient: old {obj.OldState} new {obj.NewState}");
        }

        private void Connection_Closed()
        {
            Trace.TraceInformation("ApplicationEventsClient: Disconnected, retrying to reconnect...");
            ResetConnectionTimer();
        }

        public async Task SetLastInputInfo(uint time)
        {
            try
            {
                if (_hub.Connection.State == ConnectionState.Connected)
                {
                    await (_hubProx?.Invoke("SetLastInputInfo", time));
                }
                else
                {
                    Trace.TraceError($"SetLasInputInfo: Connection state {_hub.Connection.State}");
                }
            }
            catch (Exception arg)
            {
                Trace.TraceError($"SetLastInputInfo: Exception occured: {arg}");
            }
        }

        public async Task SetForegroundWindow(string exe)
        {
            try
            {
                if (_hub.Connection.State == ConnectionState.Connected)
                {
                    await (_hubProx?.Invoke("SetForegroundWindow", exe));
                }
                else
                {
                    Trace.TraceError($"SetForegroundWindow: Connection state {_hub.Connection.State}");
                }
            }
            catch (Exception arg)
            {
                Trace.TraceError($"SetForegroundWindow: Exception occured: {arg}");
            }
        }

        public async void OnDisplayChange(uint setting, uint width, uint height)
        {
            try
            {
                if (_hub.Connection.State == ConnectionState.Connected)
                {
                    await (_hubProx?.Invoke("OnDisplayChange", setting, width, height));
                }
                else
                {
                    Trace.TraceError($"OnDisplayChange: Connection state {_hub.Connection.State}");
                }
            }
            catch (Exception arg)
            {
                Trace.TraceError($"OnDisplayChange: Exception occured: {arg}");
            }
        }

        private void _OnTextEvent(string text)
        {
            this.OnUserTextInputEvent?.Invoke(text);
            try
            {
                _hubProx?.Invoke("OnSendTextEventCompleted");
            }
            catch (Exception arg)
            {
                Trace.TraceError($"OnSendTextEventCompleted: Exception occured: {arg}");
            }
        }

        private void _OnAccelerationEvent(uint value)
        {
            this.OnUserAccelerationEvent?.Invoke(value);
        }

        private void _OnGetForegroundWindowRect()
        {
            int left = 0;
            int right = 0;
            int top = 0;
            int bottom = 0;
            int cx_screen = 0;
            int cy_screen = 0;
            this.OnGetForegroundWindowRectEvent?.Invoke(ref left, ref top, ref right, ref bottom, ref cx_screen, ref cy_screen);
            try
            {
                _hubProx?.Invoke("SetForegroundWindowRect", left, top, right, bottom, cx_screen, cy_screen);
            }
            catch (Exception arg)
            {
                Trace.TraceError($"SetForegroundWindowRect: Exception occured: {arg}");
            }
        }

        private void _OnGetScreenRefreshRate()
        {
            int item = 0;
            this.OnGetScreenRefreshRateEvent?.Invoke(ref item);
            try
            {
                _hubProx?.Invoke("GetScreenRefreshRate", item);
            }
            catch (Exception arg)
            {
                Trace.TraceError($"GetScreenRefreshRate: Exception occured: {arg}");
            }
        }

        private void _OnSetScreenRefreshRate(int value)
        {
            bool flag = false;
            if (this.OnSetScreenRefreshRateEvent != null)
            {
                flag = this.OnSetScreenRefreshRateEvent(value);
            }
            try
            {
                _hubProx?.Invoke("SetScreenRefreshRate", flag);
            }
            catch (Exception arg)
            {
                Trace.TraceError($"SetScreenRefreshRate: Exception occured: {arg}");
            }
        }

        private void _OnGetScreenRefreshRateList()
        {
            List<uint> items = new List<uint>();
            this.OnGetScreenRefreshRateListEvent?.Invoke(ref items);
            try
            {
                _hubProx?.Invoke("GetScreenRefreshRateList", items);
            }
            catch (Exception arg)
            {
                Trace.TraceError($"GetScreenRefreshRateList: Exception occured: {arg}");
            }
        }

        private void _OnMacroStartOTF(Device device)
        {
            Trace.TraceInformation("OnMacroStartOTF");
            this.StartOTFEvent?.Invoke(device);
        }

        private void _OnMacroStopOTF(Device device)
        {
            Trace.TraceInformation("OnMacroStopOTF");
            new Thread((ThreadStart)delegate
            {
                lock (_threadLock)
                {
                    Macro macro = new Macro();
                    Device device2 = device.Clone();
                    this.StopOTFEvent?.Invoke(device, ref macro);
                    try
                    {
                        Trace.TraceInformation($"OnOTFMacroStopped: Macro data count {macro.MacroEvents.Count}");
                        _hubProx?.Invoke("OnOTFMacroStopped", device2, macro);
                    }
                    catch (Exception arg)
                    {
                        Trace.TraceError($"OnOTFMacroStopped: Exception occured: {arg}");
                    }
                }
            }).Start();
        }

        private void _OnMacroCancelOTF(Device device)
        {
            Trace.TraceInformation("OnMacroCancelOTF");
            this.CancelOTFEvent?.Invoke(device);
        }

        private void _OnGetDisplaySetting()
        {
            uint setting = 0u;
            this.GetDisplaySettingEvent?.Invoke(ref setting);
            try
            {
                _hubProx?.Invoke("SetDisplaySetting", setting);
            }
            catch (Exception arg)
            {
                Trace.TraceError($"SetDisplaySetting: Exception occured: {arg}");
            }
        }

        private void _OnApplicationNotify(string msg)
        {
            this.OnApplicationNotificationEvent?.Invoke(msg);
        }
    }
}
