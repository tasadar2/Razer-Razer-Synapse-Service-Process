namespace Synapse3.UserInteractive
{
    public interface IAccountsClient
    {
        event OnLoginComplete OnLoginCompleteEvent;

        event OnLogoutComplete OnLogoutCompleteEvent;

        event OnAccountInitialized OnAccountInitializedEvent;

        event OnPostFirstSyncComplete OnPostFirstSyncCompleteEvent;

        event OnAppExit OnAppExitEvent;

        string GetRazerUserLoginToken();

        bool IsRazerCentralLoggedIn();
    }
}
