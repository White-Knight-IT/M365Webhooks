using System.Reflection;
using System.Text.Json;

namespace M365Webhooks
{
    /// <summary>
    /// This class holds an API object to pull data from along with a Webhook object to push data to
    /// </summary>
    public class PullPushPair
    {
        private object _api;
        private Type _apiType;
        private string _apiMethod;
        private object _webhook;
        private Type _webhookType;

        public PullPushPair(string api, string apiMethod, string webhookAddress, string webhookType, string webhookAuthType, string webhookAuth)
        {
            _apiType = Type.GetType("M365Webhooks.API." + api);
            _webhookType = Type.GetType("M365Webhooks.Webhooks." + webhookType);
            _api = Activator.CreateInstance(_apiType);
            _apiMethod = apiMethod;
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

        public async void Poll()
        {
            MethodInfo apiMethod = _apiType.GetMethod(_apiMethod);

            foreach (JsonElement _j in ((Task<List<JsonElement>>)apiMethod.Invoke(_api, null)).Result)
            {
                MethodInfo webhookMethod = _webhookType.GetMethod("SendWebhook");
                bool result = ((Task<bool>) webhookMethod.Invoke(_webhook, new object[] {_j})).Result;
                
            }
        }
    }
}

