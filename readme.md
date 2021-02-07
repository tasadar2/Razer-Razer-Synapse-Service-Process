# Ongoing fixes for `Razer Synapse Service Process.exe`

## Installation

> Note: It is a good idea to make a backup of any directory or files before overwriting

1. Download the archive from the appropriate [release](https://github.com/tasadar2/Razer-Razer-Synapse-Service-Process/releases)
1. Exit `Razer Synapse`
    - Due to some cross-app communication, this needs to be restarted with the service in order for the devices to be found correctly
    - This can be done from the Razer Central icon in the taskbar notification area, or by terminating the `Razer Synapse 3.exe` process
1. Stop the windows service `Razer Synapse Service`
1. Extract the downloaded files to `C:\Program Files (x86)\Razer\Synapse3\UserProcess`, overwriting what is there
1. Start the windows service `Razer Synapse Service`
1. Start `Razer Synapse`
    - This can be run with the start menu entry

## Fixes

- Greatly reduced the hardcoded delay when auto-switching between profiles based on focused window
