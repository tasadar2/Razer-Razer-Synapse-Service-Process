#define TRACE
using System.Diagnostics;
using Contract.Central;
using Synapse3UserInteractiveDeviceDetection.UserInteractive.UserInteractiveDeviceDetectionWrapper;

namespace Synapse3.UserInteractive
{
    public class DeviceDetectionHandler
    {
        private readonly UserInteractiveDeviceDetectionWrapper _deviceDetectionNative;

        private readonly IAccountsClient _accounts;

        private readonly IDeviceDetection _deviceDetectionClient;

        private volatile bool _bStarted;

        public DeviceDetectionHandler(IAccountsClient accounts, IDeviceDetection deviceDetectionClient)
        {
            _accounts = accounts;
            _deviceDetectionClient = deviceDetectionClient;
            _accounts.OnLoginCompleteEvent += OnLoginCompleteEvent;
            _accounts.OnLogoutCompleteEvent += OnLogoutCompleteEvent;
            _deviceDetectionNative = new UserInteractiveDeviceDetectionWrapper();
            _deviceDetectionNative.DeviceAddedUserInteractive += DeviceAddedUserInteractive;
            _deviceDetectionNative.DeviceRemovedUserInteractive += DeviceRemovedUserInteractive;
        }

        private void OnLoginCompleteEvent(SynapseLoginResult result)
        {
            Start();
        }

        private void OnLogoutCompleteEvent(SynapseLogoutReason reason)
        {
            Stop();
        }

        ~DeviceDetectionHandler()
        {
            Stop();
        }

        public void Start()
        {
            if (!_bStarted)
            {
                _bStarted = _deviceDetectionNative.Start();
                Trace.TraceInformation($"DeviceDetectionNative Start returned {_bStarted}");
            }
        }

        public void Stop()
        {
            if (_bStarted)
            {
                bool flag = _deviceDetectionNative.Stop();
                if (flag)
                {
                    _bStarted = false;
                }
                Trace.TraceInformation($"DeviceDetectionNative Stop returned {flag}");
            }
        }

        private void DeviceAddedUserInteractive(uint pid, uint eid, long handle)
        {
            Trace.TraceInformation($"DeviceDetectionHandler add sending {pid} {eid} {handle}");
            _deviceDetectionClient?.SendDeviceAdded(pid, eid, handle);
        }

        private void DeviceRemovedUserInteractive(uint pid, uint eid, long handle)
        {
            Trace.TraceInformation($"DeviceDetectionHandler remove sending {pid} {eid} {handle}");
            _deviceDetectionClient?.SendDeviceRemoved(pid, eid, handle);
        }
    }
}
