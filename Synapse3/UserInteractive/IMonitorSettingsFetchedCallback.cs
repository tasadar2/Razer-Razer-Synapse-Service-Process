using Contract.MonitorLib;

namespace Synapse3.UserInteractive
{
    public interface IMonitorSettingsFetchedCallback
    {
        event OnMonitorSettingsFetchedFreeSync OnMonitorSettingsFetchedFreeSyncEvent;

        event OnMonitorSettingsFetchedPiPSettings OnMonitorSettingsFetchedPiPSettingsEvent;

        event OnMonitorSettingsFetchedFPS OnMonitorSettingsFetchedFPSEvent;

        event OnMonitorSettingsFetchedHDR OnMonitorSettingsFetchedHDREvent;

        event OnMonitorSettingsFetchedInputSource OnMonitorSettingsFetchedInputSourceEvent;

        event OnMonitorSettingsFetchedInputAutoSwitch OnMonitorSettingsFetchedInputAutoSwitchEvent;

        void Response(MonitorFreeSync item);

        void Response(MonitorPiPSettings item);

        void Response(MonitorFPS item);

        void Response(MonitorHDR item);

        void Response(MonitorInputSource item);

        void Response(MonitorInputAutoSwitch item);
    }
}
