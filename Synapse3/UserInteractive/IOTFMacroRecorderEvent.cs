namespace Synapse3.UserInteractive
{
    public interface IOTFMacroRecorderEvent
    {
        event OnMacroStartOTF StartOTFEvent;

        event OnMacroStopOTF StopOTFEvent;

        event OnMacroCancelOTF CancelOTFEvent;
    }
}
