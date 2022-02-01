namespace Synapse3.UserInteractive
{
    public interface IMonitorSettingsChangedCallback
    {
        event OnMonitorSettingsChangedBrightness OnMonitorSettingsChangedBrightnessEvent;

        event OnMonitorSettingsChangedContrast OnMonitorSettingsChangedContrastEvent;

        event OnMonitorSettingsChangedColorPreset OnMonitorSettingsChangedColorPresetEvent;

        event OnMonitorSettingsChangedDisplayMode OnMonitorSettingsChangedDisplayModeEvent;

        event OnMonitorSettingsChangedTHXMode OnMonitorSettingsChangedTHXModeEvent;

        event OnMonitorSettingsChangedColorGamut OnMonitorSettingsChangedColorGamutEvent;

        event OnMonitorSettingsChangedFreeSync OnMonitorSettingsChangedFreeSyncEvent;

        event OnMonitorSettingsChangedImageScaling OnMonitorSettingsChangedImageScalingEvent;

        event OnMonitorSettingsChangedOverdrive OnMonitorSettingsChangedOverdriveEvent;

        event OnMonitorSettingsChangedPiPSettings OnMonitorSettingsChangedPiPSettingsEvent;

        event OnMonitorSettingsChangedFPS OnMonitorSettingsChangedFPSEvent;

        event OnMonitorSettingsChangedHDR OnMonitorSettingsChangedHDREvent;

        event OnMonitorSettingsChangedMotionBlur OnMonitorSettingsChangedMotionBlurEvent;

        event OnMonitorSettingsChangedInputSource OnMonitorSettingsChangedInputSourceEvent;

        event OnMonitorSettingsChangedInputAutoSwitch OnMonitorSettingsChangedInputAutoSwitchEvent;

        event OnMonitorSettingsChangedGamma OnMonitorSettingsChangedGammaEvent;

        event OnMonitorSettingsChangedDeviceMode OnMonitorSettingsChangedDeviceModeEvent;

        event OnMonitorSettingsChangedICCProfiles OnMonitorSettingsChangedICCProfilesEvent;

        event OnMonitorSettingsChangedRefreshRate OnMonitorSettingsChangedRefreshRateEvent;

        event OnMonitorSettingsChangedWindowsHDR OnMonitorSettingsChangedWindowsHDREvent;
    }
}
