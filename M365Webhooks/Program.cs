/// Created by @knightian
/// White Knight IT 2022
/// Published under MIT License

using M365Webhooks;
using System.Text.Json;

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

Log.WriteLine("M365Webhooks Process Started");
Console.WriteLine("[{0} - {1}]: M365Webhooks Process Started\n", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString());

// Do some very light sanity checking of config.json
SanityCheckConfig();

MicrosoftThreatProtection mtp = new MicrosoftThreatProtection();
List<JsonElement> incidents = mtp.ListIncidents().Result;

Log.WriteLine("M365Webhooks Process Ended");
Console.WriteLine("[{0} - {1}]: M365Webhooks Process Ended\n", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString());