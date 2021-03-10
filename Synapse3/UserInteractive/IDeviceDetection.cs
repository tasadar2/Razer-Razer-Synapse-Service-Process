namespace Synapse3.UserInteractive
{
    public interface IDeviceDetection
    {
        event OnDeviceChanged OnDeviceAddedEvent;

        event OnDeviceChanged OnDeviceRemovedEvent;

        void SendDeviceAdded(uint pid, uint eid, long handle);

        void SendDeviceRemoved(uint pid, uint eid, long handle);
    }
}
