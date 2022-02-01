#define TRACE
using System;
using System.Configuration;
using System.Diagnostics;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.Win32;

namespace Synapse3.UserInteractive
{
    public class HubConnectionHelper : IDisposable
    {
        public HubConnection Connection { get; set; }

        public HubConnectionHelper()
        {
            string format = ConfigurationManager.AppSettings["uri"];
            string text = ConfigurationManager.AppSettings["signalr_route"];
            Connection = new HubConnection(string.Format(format, GetPort()) + text, useDefaultUrl: false);
            Connection.Error += Connection_Error;
        }

        private void Connection_Error(Exception obj)
        {
            Trace.TraceError($"HubConnectionHelper: {obj?.Message}");
        }

        public void Dispose()
        {
            Connection.Dispose();
        }

        private int GetPort()
        {
            string name = "SOFTWARE\\Razer\\Synapse3\\RazerSynapse";
            string name2 = "Port";
            using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(name))
            {
                if (registryKey != null && registryKey.GetValue(name2) != null)
                {
                    int num = (int)registryKey.GetValue(name2, 5426);
                    if (num != 0)
                    {
                        return num;
                    }
                    return 5426;
                }
            }
            return 5426;
        }
    }
}
