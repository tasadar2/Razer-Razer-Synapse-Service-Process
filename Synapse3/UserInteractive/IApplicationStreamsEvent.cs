namespace Synapse3.UserInteractive
{
    public interface IApplicationStreamsEvent
    {
        event OnApplicationStreamDevice OnApplicationStreamsDeviceAddedEvent;

        event OnApplicationStreamDevice OnApplicationStreamsDeviceRemovedEvent;

        event OnApplicationStreamDevice OnApplicationStreamsDeviceGetStreamsEvent;

        event OnApplicationStreamsSet OnApplicationStreamsSetEvent;

        event OnApplicationStreamSet OnApplicationStreamSetEvent;
    }
}
