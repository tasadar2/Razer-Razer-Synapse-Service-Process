using System;
using System.Threading.Tasks;
using System.Timers;
using Contract.Central;
using Microsoft.AspNet.SignalR.Client;

namespace Synapse3.UserInteractive
{
    public class AccountsClient : IAccountsClient
    {
        private HubConnectionHelper _hub;

        private IHubProxy _hubProx;

        private bool _bLoginCompleted;

        private string _token = string.Empty;

        private Timer _connectionTimer;

        public event OnLoginComplete OnLoginCompleteEvent;

        public event OnLogoutComplete OnLogoutCompleteEvent;

        public event OnAccountInitialized OnAccountInitializedEvent;

        public event OnPostFirstSyncComplete OnPostFirstSyncCompleteEvent;

        public event OnAppExit OnAppExitEvent;

        public AccountsClient()
        {
            _connectionTimer = new Timer();
            _connectionTimer.AutoReset = false;
            _connectionTimer.Interval = 5000.0;
            _connectionTimer.Elapsed += ConnectionTimerHandler;
        }

        private async void ConnectionTimerHandler(object sender, ElapsedEventArgs e)
        {
            if (await InitConnection())
            {
                Logger.Instance.Debug("AccountsClient: Reconnected");
            }
        }

        private void ResetConnectionTimer()
        {
            Logger.Instance.Debug("AccountsClient: ResetConnectionTimer");
            _connectionTimer?.Stop();
            _connectionTimer.Start();
        }

        public async Task<bool> InitConnection()
        {
            _hub = new HubConnectionHelper();
            _hubProx = _hub.Connection.CreateHubProxy("AccountsHub");
            _hubProx.On("OnLoginComplete", delegate(SynapseLoginResult args)
            {
                _OnLoginComplete(args);
            });
            _hubProx.On("OnLogoutComplete", delegate(SynapseLogoutReason args)
            {
                _OnLogoutComplete(args);
            });
            _hubProx.On("OnAccountInitialized", delegate(UserSettingsChangedEnum args)
            {
                this.OnAccountInitializedEvent?.Invoke(args);
            });
            _hubProx.On("OnPostFirstSyncComplete", (Action)delegate
            {
                this.OnPostFirstSyncCompleteEvent?.Invoke();
            });
            _hubProx.On("OnAppExit", (Action)delegate
            {
                this.OnAppExitEvent?.Invoke();
            });
            try
            {
                await _hub.Connection.Start();
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"InitConnection: {ex?.Message}");
            }
            if (_hub.Connection.State == ConnectionState.Connected)
            {
                _hub.Connection.Closed += Connection_Closed;
                _hub.Connection.StateChanged += Connection_StateChanged;
                return true;
            }
            ResetConnectionTimer();
            return false;
        }

        private void Connection_StateChanged(StateChange obj)
        {
            Logger.Instance.Debug($"AccountsClient: old {obj.OldState} new {obj.NewState}");
        }

        private void Connection_Closed()
        {
            Logger.Instance.Debug("AccountsClient: Disconnected, retrying to reconnect...");
            ResetConnectionTimer();
        }

        private void _OnLoginComplete(SynapseLoginResult args)
        {
            _bLoginCompleted = true;
            _token = string.Empty;
            this.OnLoginCompleteEvent?.Invoke(args);
        }

        private void _OnLogoutComplete(SynapseLogoutReason args)
        {
            _bLoginCompleted = false;
            _token = string.Empty;
            this.OnLogoutCompleteEvent?.Invoke(args);
        }

        public string GetRazerUserLoginToken()
        {
            Logger.Instance.Debug("GetRazerUserLogin: enter");
            try
            {
                if (!string.IsNullOrEmpty(_token))
                {
                    return _token;
                }
                if (_hubProx != null && _hub.Connection.State == ConnectionState.Connected)
                {
                    Logger.Instance.Debug("GetRazerUserLogin: invoke");
                    RazerUserInfo result = _hubProx.Invoke<RazerUserInfo>("GetRazerUser", new object[0]).Result;
                    if (result != null)
                    {
                        if (!string.IsNullOrEmpty(result.Token))
                        {
                            _token = result.Token;
                        }
                        Logger.Instance.Debug("GetRazerUserLogin: exit");
                        return result.Token;
                    }
                }
            }
            catch (Exception arg)
            {
                Logger.Instance.Error($"GetRazerUserLogin: exception occurred {arg}");
            }
            Logger.Instance.Debug("GetRazerUserLogin: exit");
            return "";
        }

        public bool IsRazerCentralLoggedIn()
        {
            if (!_bLoginCompleted)
            {
                _bLoginCompleted = IsRazerCentralLoggedInInService();
            }
            return _bLoginCompleted;
        }

        private bool IsRazerCentralLoggedInInService()
        {
            try
            {
                Logger.Instance.Debug("IsRazerCentralLoggedIn: invoke.");
                if (_hubProx != null && _hub.Connection.State == ConnectionState.Connected)
                {
                    return _hubProx.Invoke<bool>("IsRazerCentralLoggedIn", new object[0]).Result;
                }
            }
            catch (Exception arg)
            {
                Logger.Instance.Error($"IsRazerCentralLoggedIn: exception occurred {arg}");
            }
            finally
            {
                Logger.Instance.Debug("IsRazerCentralLoggedIn: done.");
            }
            return false;
        }
    }
}
