using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Synapse3.UserInteractive
{
    public class HiddenForm : Form, IWndProc
    {
        private AccountsClient _accounts;

        private ApplicationEventsClient _applicationEventsClient;

        private DeviceEventsClient _deviceEventsClient;

        private DeviceDetectionClient _deviceDetectionClient;

        private UserInputMonitor _userInputMonitor;

        private ForegroundWindowMonitor _foregroundMonitor;

        private UserTextInputEventHandler _textInputHandler;

        private UserAccelerationEventHandler _accelerationEventHandler;

        private GetForegroundWindowRectEventHandler _foregroundWindowRectHandler;

        private OTFMacroRecorderEventHandler _oftMacroRecorderEventHandler;

        private ApplicationStreamsEventHandler _applicationStreamsEventHandler;

        private MessageEventHandler _messageEventHandler;

        private MonitorSettingsChangedHandler _monitorSettingsChangedHandler;

        private ApplicationNotificationHandler _appNotificationHandler;

        private DeviceDetectionHandler _deviceDetectionHandler;

        private IContainer components;

        public event WndProcDelegate OnWndProcEvent;

        public HiddenForm()
        {
            InitializeComponent();
            base.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            base.Opacity = 0.0;
            base.Visible = false;
            base.ShowInTaskbar = false;
            base.ShowIcon = false;
            if (System.Windows.Application.Current == null)
            {
                new System.Windows.Application().ShutdownMode = ShutdownMode.OnExplicitShutdown;
            }
        }

        private async void HiddenForm_Load(object sender, EventArgs e)
        {
            Size = new System.Drawing.Size(0, 0);
            _deviceEventsClient = new DeviceEventsClient();
            _deviceDetectionClient = new DeviceDetectionClient();
            _accounts = new AccountsClient();
            _applicationStreamsEventHandler = new ApplicationStreamsEventHandler(_accounts, _deviceEventsClient);
            _applicationEventsClient = new ApplicationEventsClient();
            _userInputMonitor = new UserInputMonitor(_applicationEventsClient);
            _foregroundMonitor = new ForegroundWindowMonitor(_applicationEventsClient);
            _textInputHandler = new UserTextInputEventHandler(_applicationEventsClient);
            _accelerationEventHandler = new UserAccelerationEventHandler(_applicationEventsClient);
            _foregroundWindowRectHandler = new GetForegroundWindowRectEventHandler(_applicationEventsClient);
            _oftMacroRecorderEventHandler = new OTFMacroRecorderEventHandler(_applicationEventsClient);
            _appNotificationHandler = new ApplicationNotificationHandler(_applicationEventsClient);
            _messageEventHandler = new MessageEventHandler(_deviceEventsClient);
            _monitorSettingsChangedHandler = new MonitorSettingsChangedHandler(_deviceDetectionClient, _deviceEventsClient, _deviceEventsClient, this);
            _deviceDetectionHandler = new DeviceDetectionHandler(_accounts, _deviceDetectionClient);
            await _accounts.InitConnection();
            await _applicationEventsClient.InitConnection();
            await _deviceEventsClient.InitConnection();
            await _deviceDetectionClient.InitConnection();
            if (_accounts?.IsRazerCentralLoggedIn() ?? false)
            {
                _deviceDetectionHandler?.Start();
            }
            _userInputMonitor.Start();
            UpdateRefreshRate();
        }

        private void UpdateRefreshRate()
        {
            if (GetRefreshRateFromRegistry(out var rate) && rate != 0 && MonitorRefreshRate.SetScreenRefreshRate(rate))
            {
                DeleteRefreshRateRegistry();
            }
        }

        private void DeleteRefreshRateRegistry()
        {
            try
            {
                using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Razer\\Synapse3\\RazerSynapse", writable: true);
                registryKey?.DeleteValue("SetRefreshRate");
            }
            catch (Exception)
            {
            }
        }

        private bool GetRefreshRateFromRegistry(out int rate)
        {
            bool result = false;
            rate = 0;
            try
            {
                using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Razer\\Synapse3\\RazerSynapse");
                if (registryKey != null)
                {
                    if (registryKey.GetValue("SetRefreshRate") != null)
                    {
                        rate = (int)registryKey.GetValue("SetRefreshRate");
                        result = true;
                        return result;
                    }
                    return result;
                }
                return result;
            }
            catch (Exception)
            {
                return result;
            }
        }

        protected override void WndProc(ref Message m)
        {
            this.OnWndProcEvent?.Invoke(m);
            base.WndProc(ref m);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
            base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            base.ClientSize = new System.Drawing.Size(233, 109);
            base.Name = "HiddenForm";
            base.Load += new System.EventHandler(HiddenForm_Load);
            ResumeLayout(false);
        }
    }
}
