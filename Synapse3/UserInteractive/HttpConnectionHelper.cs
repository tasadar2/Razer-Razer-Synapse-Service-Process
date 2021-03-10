using System;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;

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
            _client.BaseAddress = new Uri(_baseUri);
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-protobuf"));
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accounts.GetRazerUserLoginToken());
            Client = _client;
        }
    }
}
