namespace Synapse3.UserInteractive
{
    public interface IDisplayChangedEvent
    {
        event OnGetDisplaySetting GetDisplaySettingEvent;

        void OnDisplayChange(uint setting, uint width, uint height);
    }
}
