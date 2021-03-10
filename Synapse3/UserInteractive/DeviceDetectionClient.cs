#define TRACE
using System;
using System.Diagnostics;
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
                Trace.TraceInformation("DeviceDetectionClient: Reconnected");
            }
        }

        private void ResetConnectionTimer()
        {
            Trace.TraceInformation("DeviceDetectionClient: ResetConnectionTimer");
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
            try
            {
                await _hub.Connection.Start();
            }
            catch (Exception ex)
            {
                Trace.TraceError("InitConnection: " + ex?.Message);
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
            Trace.TraceInformation($"DeviceDetectionClient: old {obj.OldState} new {obj.NewState}");
        }

        private void Connection_Closed()
        {
            Trace.TraceInformation("DeviceDetectionClient: Disconnected, retrying to reconnect...");
            ResetConnectionTimer();
        }

        public void SendDeviceAdded(uint pid, uint eid, long handle)
        {
            try
            {
                Trace.TraceInformation("SendDeviceAdded: invoke.");
                if (_hubProx != null && _hub.Connection.State == ConnectionState.Connected)
                {
                    Trace.TraceInformation("SendDeviceAdded: Sending.");
                    _hubProx.Invoke("DeviceAddedFromClient", pid, eid, handle);
                }
            }
            catch (Exception arg)
            {
                Trace.TraceError($"SendDeviceAdded: exception occurred {arg}");
            }
            finally
            {
                Trace.TraceInformation("SendDeviceAdded: done.");
            }
        }

        public void SendDeviceRemoved(uint pid, uint eid, long handle)
        {
            try
            {
                Trace.TraceInformation("SendDeviceRemoved: invoke.");
                if (_hubProx != null && _hub.Connection.State == ConnectionState.Connected)
                {
                    Trace.TraceInformation("SendDeviceRemoved: Sending.");
                    _hubProx.Invoke("DeviceRemovedFromClient", pid, eid, handle);
                }
            }
            catch (Exception arg)
            {
                Trace.TraceError($"SendDeviceRemoved: exception occurred {arg}");
            }
            finally
            {
                Trace.TraceInformation("SendDeviceRemoved: done.");
            }
        }
    }
}
