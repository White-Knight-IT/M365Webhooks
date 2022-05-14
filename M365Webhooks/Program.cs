/// Created by @knightian
/// White Knight IT 2022
/// Published under MIT License

using M365Webhooks;

Log.WriteLine("M365Webhooks Process Started");
Log.WriteLine("Press Ctrl-C to terminate");

List<Thread> _threadPool = new();
List<PullPushPair> _pollPairs = new();

// Method to run when control-c instruction to kill process received
Console.CancelKeyPress += delegate { EndProgram(); };
AppDomain.CurrentDomain.ProcessExit += delegate { EndProgram(); };

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
        Log.WriteLine("DebugShowSecrets unspecified so we have set DebugShowSecrets false", true); // Force console output
    }

    // If debug isn't specified we'll assume it's not wanted
    if (string.IsNullOrEmpty(Configuration.Debug.ToString()))
    {
        Configuration.Debug = false;
        Log.WriteLine("Debug unspecified so we have set Debug false", true); // Force console output
    }

    int webhooks = Configuration.WebhookAddress.Length;

    // Check correct number of config items to build webhooks
    if ((webhooks != Configuration.WebhookType.Length) || (webhooks != Configuration.WebhookAuth.Length) || (webhooks != Configuration.WebhookAuthType.Length) || (webhooks != Configuration.Api.Length) || (webhooks != Configuration.ApiMethod.Length))
    {
        throw new NotEnoughArguments("There must be equal amounts of WebhookAddress, WebhookType, WebhookAuth, WebhookAuthType, Api and ApiMethod");
    }

    // Tokens live 60 minutes so anything over 58 could cause constant token fetch
    if (Configuration.TokenExpires > 58)
    {
        if (!Configuration.Debug)
        {
            Log.WriteLine("TokenExpires dangerously large (" + Configuration.TokenExpires + "), reduced it to 58", true); // Force console output
            Configuration.TokenExpires = 58;
        }
        else
        {
            Log.WriteLine("TokenExpires dangerously large (" + Configuration.TokenExpires + "), recommended action to reduce it to 58 or less", true); // Force console output
        }

    }

    // Dont allow a token expires offset < 0
    if (Configuration.TokenExpires < 0)
    { 
        Log.WriteLine("TokenExpires less than 0 (" + Configuration.TokenExpires + "), set it to 0", true); // Force console output
        Configuration.TokenExpires = 0;
    }

    //Make sure polling time at least 3
    if (Configuration.PollingTime < 3)
    {
        if (!Configuration.Debug)
        {
            Log.WriteLine("PollingTime faster than every 3 seconds (" + Configuration.PollingTime + "), this is not recommended as you may get rate limited by Microsoft, setting to 3 seconds", true); //Force to console
            Configuration.PollingTime = 3;
        }
        else
        {
            Log.WriteLine("PollingTime faster than every 3 seconds (" + Configuration.PollingTime + "), this is not recommended as you may get rate limited by Microsoft", true); //Force to console
        }
    }

    //Make sure polling time at least 3
    if (Configuration.StartFetchMinutes < 0)
    {

        Log.WriteLine("StartFetchMinutes is less than 0 (" + Configuration.PollingTime + "), you cannot seek data from the future Marty McFly, setting to 0", true); //Force to console
        Configuration.StartFetchMinutes = 0;
    }

}

#endregion

try
{
    // Do some very light sanity checking of config.json
    SanityCheckConfig();

    // Build PullPushPairs based on webhook outputs
    for (int _i = 0; _i < Configuration.WebhookAddress.Length; _i++)
    {
        // Spawn a new thread for each PullPushPair so that they can operate at their own time and are not bound by eachother
        _pollPairs.Add(new PullPushPair(Configuration.Api[_i], Configuration.ApiMethod[_i], Configuration.WebhookAddress[_i], Configuration.WebhookType[_i], Configuration.WebhookAuthType[_i], Configuration.WebhookAuth[_i]));
        Thread thread = new(new ThreadStart(_pollPairs[_i].Poll));
        _threadPool.Add(thread);
        thread.Start();
    }
}
catch (Exception ex)
{
    Log.WriteLine("Exception: " + ex.Message + "\nInner Exception: " + ex.InnerException + "\nSource:" + ex.Source);
    EndProgram();
}


// Instruct our threads to die and once all threads are dead the process exits with error 0
void EndProgram()
{
    bool threadsAlive = true;

    foreach (PullPushPair poller in _pollPairs)
    {
        poller.CancelThread = true;
    }

    while (threadsAlive)
    {
        threadsAlive = false;

        foreach (Thread _t in _threadPool)
        {
            threadsAlive = _t.IsAlive;
        }
    }

    Log.WriteLine("M365Webhooks Process Ended");

    // Exit without error code
    Environment.Exit(0);
}