namespace Synapse3.UserInteractive
{
    public interface IDeviceDetection
    {
        event OnDeviceChanged OnDeviceAddedEvent;

        event OnDeviceChanged OnDeviceRemovedEvent;

        event OnDeviceChanged OnDeviceSerialAddedEvent;

        void SendDeviceAdded(uint pid, uint eid, long handle);

        void SendDeviceRemoved(uint pid, uint eid, long handle);

        void SendDeviceSerialAdded(uint pid, uint eid, long handle, string serialNo);
    }
}
