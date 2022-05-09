/// Created by @knightian
/// White Knight IT
/// Published under MIT License

using M365Webhooks;
using System.Text.Json;

Log.WriteLine("M365Webhooks Process Started");
Console.WriteLine("[{0} - {1}]: M365Webhooks Process Started", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString());

MicrosoftThreatProtection mtp = new MicrosoftThreatProtection();
List<JsonElement> incidents = mtp.ListIncidents().Result;

Log.WriteLine("M365Webhooks Process Ended");
Console.WriteLine("[{0} - {1}]: M365Webhooks Process Ended", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString());