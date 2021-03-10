using System.Collections.Generic;
using System.Net.Http;
using Contract.Audio.ApplicationStreamsLib;
using Contract.Common;
using WebApiContrib.Formatting;

namespace Synapse3.UserInteractive
{
    public class ApplicationStreamsClient
    {
        private HttpConnectionHelper _http;

        public ApplicationStreamsClient(IAccountsClient client)
        {
            _http = new HttpConnectionHelper(client);
        }

        public List<Device> GetAppStreamDevices()
        {
            string requestUri = "api/AudioApplicationStreams/getappstreamdevices";
            HttpResponseMessage result = _http.Client.GetAsync(requestUri).Result;
            if (result.IsSuccessStatusCode)
            {
                return result.Content.ReadAsAsync<List<Device>>(new ProtoBufFormatter[1]
                {
                    new ProtoBufFormatter()
                }).Result;
            }
            return null;
        }

        public bool SetAppStream(ApplicationStreams item)
        {
            string requestUri = "api/AudioApplicationStreams/setappstreamsfromprocess";
            HttpResponseMessage result = _http.Client.PostAsync(requestUri, item, new ProtoBufFormatter()).Result;
            if (result.IsSuccessStatusCode)
            {
                return result.Content.ReadAsAsync<bool>(new ProtoBufFormatter[1]
                {
                    new ProtoBufFormatter()
                }).Result;
            }
            return false;
        }

        public bool UpdateAppStream(long handle)
        {
            string requestUri = "api/AudioApplicationStreams/putappstreamschanged/?handle=" + handle;
            HttpResponseMessage result = _http.Client.PutAsync(requestUri, null).Result;
            if (result.IsSuccessStatusCode)
            {
                return result.Content.ReadAsAsync<bool>(new ProtoBufFormatter[1]
                {
                    new ProtoBufFormatter()
                }).Result;
            }
            return false;
        }
    }
}
