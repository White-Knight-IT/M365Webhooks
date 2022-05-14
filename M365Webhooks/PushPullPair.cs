using System.Reflection;
using System.Text.Json;

namespace M365Webhooks
{
    /// <summary>
    /// This class holds an API object to pull data from along with a Webhook object to push data to
    /// </summary>
    public class PullPushPair
    {
        private readonly object? _api;
        private readonly Type? _apiType;
        private readonly object? _webhook;
        private readonly Type? _webhookType;
        private bool _cancelToken;

        public PullPushPair(string api, string apiMethod, string webhookAddress, string webhookType, string webhookAuthType, string webhookAuth)
        {
            _cancelToken = false;
            _apiType = Type.GetType("M365Webhooks.API." + api);
            _webhookType = Type.GetType("M365Webhooks.Webhooks." + webhookType);
            _api = Activator.CreateInstance(_apiType);
            object[] webhookArguments;

            switch (webhookType)
            {
                case "Plain":
                    webhookArguments = new object[] {webhookAddress,api,apiMethod,webhookAuthType, webhookAuth};
                    break;

                default:
                    webhookArguments = new object[] { };
                    break;
            }
            
            _webhook = Activator.CreateInstance(_webhookType,webhookArguments);
        }

        public void Poll()
        {
            MethodInfo apiMethod = _apiType.GetMethod(_webhookType.GetProperty("ApiMethod").GetValue(_webhook).ToString());
            MethodInfo webhookMethod = _webhookType.GetMethod("SendWebhook");

            while (true && !_cancelToken)
            {
                foreach (JsonElement _j in ((Task<List<JsonElement>>)apiMethod.Invoke(_api, null)).Result)
                {
                   
                    if(!((Task<bool>)webhookMethod.Invoke(_webhook, new object[] { _j })).Result)
                    {
                        Log.WriteLine("Sending to webhook did not return HTTP 200 OK: "+_webhookType.GetProperty("WebhookAddress").GetValue(_webhook).ToString());
                    }

                }

                // Causes thread to sleep for PollingTime seconds before pooling again but checks if thread cancelled every 1 second
                for(int _i=0; _i < (Configuration.PollingTime * 1000); _i=_i+1000)
                {
                    if (_cancelToken)
                        break;
                    Thread.CurrentThread.Join(1000);
                }
            }
        }

        #region Properties

        public bool CancelThread { set { _cancelToken = value;} }

        #endregion
    }
}

