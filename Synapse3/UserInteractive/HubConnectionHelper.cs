#define TRACE
using System;
using System.Configuration;
using System.Diagnostics;
using Microsoft.AspNet.SignalR.Client;

namespace Synapse3.UserInteractive
{
    public class HubConnectionHelper : IDisposable
    {
        public HubConnection Connection { get; set; }

        public HubConnectionHelper()
        {
            string text = ConfigurationManager.AppSettings["uri"];
            string text2 = ConfigurationManager.AppSettings["signalr_route"];
            Connection = new HubConnection(text + text2, useDefaultUrl: false);
            Connection.Error += Connection_Error;
        }

        private void Connection_Error(Exception obj)
        {
            Trace.TraceError("HubConnectionHelper: " + obj?.Message);
        }

        public void Dispose()
        {
            Connection.Dispose();
        }
    }
}
