/// Created by @knightian
/// White Knight IT 2022
/// Published under MIT License

using M365Webhooks;

Log.WriteLine("M365Webhooks Process Started");
Console.WriteLine("[{0} - {1}]: M365Webhooks Process Started\n", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString());

#region Config Sanity Checking

/// <summary>
/// Do some config sanity checking
/// </summary>
void SanityCheckConfig()
{
    // Keep secrets out of debug output unless specifically specified to include
    if (string.IsNullOrEmpty(Configuration.DebugShowSecrets.ToString()))
    {
        Configuration.DebugShowSecrets = false;
    }

    // If debug isn't specified we'll assume it's not wanted
    if (string.IsNullOrEmpty(Configuration.Debug.ToString()))
    {
        Configuration.Debug = false;
    }

    int webhooks = Configuration.WebhookAddress.Length;

    // Check correct number of config items to build webhooks
    if ((webhooks != Configuration.WebhookType.Length) || (webhooks != Configuration.WebhookAuth.Length) || (webhooks != Configuration.WebhookAuthType.Length) || (webhooks != Configuration.Api.Length) || (webhooks != Configuration.ApiMethod.Length))
    {
        //throw
    }

    // Tokens live 60 minutes so anything over 58 could cause constant token fetch
    if (Configuration.TokenExpires>58)
    {
        if (!Configuration.Debug)
        {
            Configuration.TokenExpires = 58;
            Console.WriteLine("[{0} - {1}]: TokenExpires dangerously large ({2}), reduced it to 58\n", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString(), Configuration.TokenExpires);
            Log.WriteLine("TokenExpires dangerously large(" + Configuration.TokenExpires + "), reduced it to 58");
        }
        else
        {
            Console.WriteLine("[{0} - {1}]: TokenExpires dangerously large ({2}), recommended action to reduce it to 58 or less\n", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString(),Configuration.TokenExpires);
            Log.WriteLine("TokenExpires dangerously large(" + Configuration.TokenExpires + "), recommended action to reduce it to 58 or less");
        }
    }
}

#endregion

// Do some very light sanity checking of config.json
SanityCheckConfig();

List<PullPushPair> pollPairs = new();

// Build PullPushPairs based on webhook outputs

for(int _i=0;_i<Configuration.WebhookAddress.Length;_i++)
{
    pollPairs.Add(new PullPushPair(Configuration.Api[_i], Configuration.ApiMethod[_i], Configuration.WebhookAddress[_i], Configuration.WebhookType[_i], Configuration.WebhookAuthType[_i], Configuration.WebhookAuth[_i]));
    pollPairs[_i].Poll();
}


Log.WriteLine("M365Webhooks Process Ended");
Console.WriteLine("[{0} - {1}]: M365Webhooks Process Ended\n", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString());