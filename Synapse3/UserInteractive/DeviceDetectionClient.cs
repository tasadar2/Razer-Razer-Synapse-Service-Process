using System;
using System.Threading.Tasks;
using System.Timers;
using Contract.Common;
using Microsoft.AspNet.SignalR.Client;

namespace Synapse3.UserInteractive
{
    public class DeviceDetectionClient : IDeviceDetection
    {
        private HubConnectionHelper _hub;

        private IHubProxy _hubProx;

        private Timer _connectionTimer;

        public event OnDeviceChanged OnDeviceAddedEvent;

        public event OnDeviceChanged OnDeviceRemovedEvent;

        public event OnDeviceChanged OnDeviceSerialAddedEvent;

        public DeviceDetectionClient()
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
                Logger.Instance.Debug("DeviceDetectionClient: Reconnected");
            }
        }

        private void ResetConnectionTimer()
        {
            Logger.Instance.Debug("DeviceDetectionClient: ResetConnectionTimer");
            _connectionTimer?.Stop();
            _connectionTimer.Start();
        }

        public async Task<bool> InitConnection()
        {
            _hub = new HubConnectionHelper();
            _hubProx = _hub.Connection.CreateHubProxy("DeviceDetectionHub");
            _hubProx.On("OnDeviceLoading", delegate(Device device)
            {
                this.OnDeviceAddedEvent?.Invoke(device);
            });
            _hubProx.On("OnDeviceLoaded", delegate(Device device)
            {
                this.OnDeviceAddedEvent?.Invoke(device);
            });
            _hubProx.On("OnDeviceRemoved", delegate(Device device)
            {
                this.OnDeviceRemovedEvent?.Invoke(device);
            });
            _hubProx.On("OnDeviceSerialAdded", delegate(Device device)
            {
                this.OnDeviceSerialAddedEvent?.Invoke(device);
            });
            try
            {
                await _hub.Connection.Start();
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"InitConnection: {ex?.Message}");
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

        public void Start()
        {
            _hub.Connection.Start();
        }

        private void Connection_StateChanged(StateChange obj)
        {
            Logger.Instance.Debug($"DeviceDetectionClient: old {obj.OldState} new {obj.NewState}");
        }

        private void Connection_Closed()
        {
            Logger.Instance.Debug("DeviceDetectionClient: Disconnected, retrying to reconnect...");
            ResetConnectionTimer();
        }

        public void SendDeviceAdded(uint pid, uint eid, long handle)
        {
            try
            {
                Logger.Instance.Debug("SendDeviceAdded: invoke.");
                if (_hubProx != null && _hub.Connection.State == ConnectionState.Connected)
                {
                    Logger.Instance.Debug("SendDeviceAdded: Sending.");
                    _hubProx.Invoke("DeviceAddedFromClient", pid, eid, handle);
                }
            }
            catch (Exception arg)
            {
                Logger.Instance.Error($"SendDeviceAdded: exception occurred {arg}");
            }
            finally
            {
                Logger.Instance.Debug("SendDeviceAdded: done.");
            }
        }

        public void SendDeviceRemoved(uint pid, uint eid, long handle)
        {
            try
            {
                Logger.Instance.Debug("SendDeviceRemoved: invoke.");
                if (_hubProx != null && _hub.Connection.State == ConnectionState.Connected)
                {
                    Logger.Instance.Debug("SendDeviceRemoved: Sending.");
                    _hubProx.Invoke("DeviceRemovedFromClient", pid, eid, handle);
                }
            }
            catch (Exception arg)
            {
                Logger.Instance.Error($"SendDeviceRemoved: exception occurred {arg}");
            }
            finally
            {
                Logger.Instance.Debug("SendDeviceRemoved: done.");
            }
        }

        public void SendDeviceSerialAdded(uint pid, uint eid, long handle, string serialNo)
        {
            try
            {
                Logger.Instance.Debug("SendDeviceSerialAdded: invoke.");
                if (_hubProx != null && _hub.Connection.State == ConnectionState.Connected)
                {
                    Logger.Instance.Debug("SendDeviceSerialAdded: Sending.");
                    _hubProx.Invoke("DeviceSerialAddedromClient", pid, eid, handle, serialNo);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"SendDeviceSerialAdded: exception occurred {ex.Message}");
            }
            finally
            {
                Logger.Instance.Debug("SendDeviceSerialAdded: done.");
            }
        }
    }
}
