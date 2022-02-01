using System;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Win32;

namespace Synapse3.UserInteractive
{
    public class HttpConnectionHelper
    {
        private static readonly string _baseUri = ConfigurationManager.AppSettings["uri"];

        private const string _protobufApplicationheader = "application/x-protobuf";

        private readonly IAccountsClient _accounts;

        private HttpClient _client;

        public HttpClient Client
        {
            get
            {
                string text = _client?.DefaultRequestHeaders?.Authorization?.Parameter ?? string.Empty;
                if (string.IsNullOrEmpty(text) || !text.Equals(_accounts.GetRazerUserLoginToken()))
                {
                    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accounts.GetRazerUserLoginToken());
                }
                return _client;
            }
            set
            {
                _client = value;
            }
        }

        public HttpConnectionHelper(IAccountsClient accounts)
        {
            _accounts = accounts;
            _client = new HttpClient();
            _client.BaseAddress = new Uri(string.Format(_baseUri, GetPort()));
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-protobuf"));
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accounts.GetRazerUserLoginToken());
            Client = _client;
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
