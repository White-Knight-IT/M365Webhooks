using System.Reflection;
using System.Text.Json;

namespace M365Webhooks
{
    /// <summary>
    /// This class holds an API object to pull data from along with a Webhook object to push data to
    /// </summary>
    public class PullPushPair
    {
        private readonly object _api;
        private readonly Type _apiType;
        private readonly object _webhook;
        private readonly Type _webhookType;
        private Thread? _thread;

        public PullPushPair(string api, string apiMethod, string webhookAddress, string webhookType, string webhookAuthType, string webhookAuth)
        {
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
            _thread = Thread.CurrentThread;
            MethodInfo apiMethod = _apiType.GetMethod(_webhookType.GetProperty("ApiMethod").GetValue(_webhook).ToString());
            MethodInfo webhookMethod = _webhookType.GetMethod("SendWebhook");

            while (true && !Configuration.EndingProgram)
            {
                int sent = 0;

                foreach (JsonElement _j in ((Task<List<JsonElement>>)apiMethod.Invoke(_api, null)).Result)
                {
                    if (Configuration.EndingProgram)
                    {
                        // Program is ending break loop
                        break;
                    }
                    
                    webhookMethod.Invoke(_webhook, new object[] { _j });

                    // We want to avoid a webhook flood so we will scale back when we get bulk webhooks to post
                    int floodProtect = sent * 10;

                    // Limit flood protection to one webhook every threshold miliseconds second max
                    if (floodProtect > Configuration.Threshold)
                    {
                        floodProtect = Configuration.Threshold;
                    }

                    Thread.CurrentThread.Join(floodProtect);
                    sent++;
                    
                }

                // Causes thread to sleep for PollingTime seconds before pooling again but checks if thread cancelled every 1/10 second
                for(int _i=0; _i < (Configuration.PollingTime * 1000); _i=_i+100)
                {
                    if (Configuration.EndingProgram)
                        break;
                    Thread.CurrentThread.Join(100);
                }
            }
        }

        #region Properties

        public Thread? CurrentThread { get { return _thread; } }

        #endregion
    }
}

