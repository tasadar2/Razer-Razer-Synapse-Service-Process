namespace Synapse3.UserInteractive
{
    public interface IScreenRefreshRateEvent
    {
        event OnGetScreenRefreshRate OnGetScreenRefreshRateEvent;

        event OnSetScreenRefreshRate OnSetScreenRefreshRateEvent;

        event OnGetScreenRefreshRateList OnGetScreenRefreshRateListEvent;
    }
}
