namespace Synapse3.UserInteractive
{
    public interface IMonitorSettingsChangedCallback
    {
        event OnMonitorSettingsChangedBrightness OnMonitorSettingsChangedBrightnessEvent;

        event OnMonitorSettingsChangedContrast OnMonitorSettingsChangedContrastEvent;

        event OnMonitorSettingsChangedColorPreset OnMonitorSettingsChangedColorPresetEvent;

        event OnMonitorSettingsChangedDisplayMode OnMonitorSettingsChangedDisplayModeEvent;

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
    }
}
