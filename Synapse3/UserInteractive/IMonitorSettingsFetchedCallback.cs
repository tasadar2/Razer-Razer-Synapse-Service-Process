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

        event OnMonitorSettingsFetchedICCProfiles OnMonitorSettingsFetchedICCProfilesEvent;

        event OnMonitorSettingsFetchedRefreshRate OnMonitorSettingsFetchedRefreshRateEvent;

        event OnMonitorSettingsFetchedWindowsHDR OnMonitorSettingsFetchedWindowsHDREvent;

        event OnMonitorSettingsFetchedFWVersion OnMonitorSettingsFetchedFWVersionEvent;

        void Response(MonitorFreeSync item);

        void Response(MonitorPiPSettings item);

        void Response(MonitorFPS item);

        void Response(MonitorHDR item);

        void Response(MonitorWindowsHDR item);

        void Response(MonitorInputSource item);

        void Response(MonitorInputAutoSwitch item);

        void Response(MonitorICCProfiles item);

        void Response(Contract.MonitorLib.MonitorRefreshRate item);

        void Response(MonitorDeviceInfo item);
    }
}
