using System;
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
            try
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
            }
            catch (Exception arg)
            {
                Logger.Instance.Error($"GetAppStreamDevices: Exception occured: {arg}");
            }
            return null;
        }

        public bool SetAppStream(ApplicationStreams item)
        {
            try
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
            }
            catch (AggregateException ex)
            {
                foreach (Exception innerException in ex.InnerExceptions)
                {
                    Logger.Instance.Error($"SetAppStream: AggregateException occured: {innerException}");
                }
            }
            catch (Exception arg)
            {
                Logger.Instance.Error($"SetAppStream: Exception occured: {arg}");
            }
            return false;
        }

        public bool UpdateAppStream(long handle)
        {
            try
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
            }
            catch (AggregateException ex)
            {
                foreach (Exception innerException in ex.InnerExceptions)
                {
                    Logger.Instance.Error($"SetAppStream: AggregateException occured: {innerException}");
                }
            }
            catch (Exception arg)
            {
                Logger.Instance.Error($"UpdateAppStream: Exception occured: {arg}");
            }
            return false;
        }
    }
}
