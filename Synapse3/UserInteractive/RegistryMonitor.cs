using System;
using System.Configuration;
using System.Linq;
using System.ServiceProcess;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace Synapse3.UserInteractive
{
    public class RegistryMonitor
    {
        private IRegistryChangedInfo _registryMonitorImpl;

        private RegistryWatcher _registryWatcher;

        private const string TOUCHPAD_REGISTRY_KEYNAME = "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\PrecisionTouchPad\\Status";

        public RegistryMonitor(IRegistryChangedInfo registryMonitorImpl)
        {
            _registryMonitorImpl = registryMonitorImpl;
            _registryWatcher = new RegistryWatcher("HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\PrecisionTouchPad\\Status");
            _registryWatcher.RegChanged += OnRegChanged;
        }

        public void Start()
        {
            _registryWatcher.Start();
        }

        private void OnRegChanged(object sender, EventArgs e)
        {
            try
            {
                int num = 0;
                RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\PrecisionTouchPad\\Status");
                if (registryKey == null)
                {
                    return;
                }
                try
                {
                    string value = ((registryKey.GetValue("Enabled") == null) ? "0" : registryKey.GetValue("Enabled").ToString());
                    num = Convert.ToInt32(value);
                    if (IsSynapseServiceRunning())
                    {
                        JObject jObject = new JObject
                        {
                            { "settings", "PrecisionTouchPad" },
                            { "status", num }
                        };
                        HandleRegistryNotification(jObject.ToString());
                    }
                }
                catch (Exception)
                {
                }
            }
            catch (Exception)
            {
            }
        }

        private async void HandleRegistryNotification(string info)
        {
            await _registryMonitorImpl.SetRegistryChangedInfo(info);
        }

        private bool IsSynapseServiceRunning()
        {
            string service = ConfigurationManager.AppSettings["service_name"];
            if (!ServiceController.GetServices().Any((ServiceController serviceController) => serviceController.ServiceName.Equals(service)))
            {
                return false;
            }
            ServiceController serviceController2 = new ServiceController(service);
            if (serviceController2.Status != ServiceControllerStatus.Running)
            {
                return false;
            }
            return true;
        }
    }
}
