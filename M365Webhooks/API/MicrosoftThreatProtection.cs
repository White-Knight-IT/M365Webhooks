using System.Text.Json;
namespace M365Webhooks
{
	internal class MicrosoftThreatProtection : Request
	{
		//The resource we obtain our JWT OAuth2 token for
		public const string ResourceId = "https://api.security.microsoft.com";

		//Request will only  seek data that is >= this time
		private string _lastRequestTime;

		public MicrosoftThreatProtection():base(ResourceId)
		{
			//On first run we get all incidents in the last 24 hours
			_lastRequestTime = DateTime.Now.ToUniversalTime().AddHours(-24).ToString("o");
		}

        /// <summary>
        /// Gets incidents from https://api.security.microsoft.com/api/incidents
        /// </summary>
        /// <returns>List of JSON Objects, each Object is an Incident see https://docs.microsoft.com/en-us/microsoft-365/security/defender/api-list-incidents?view=o365-worldwide</returns>
        public async Task<List<JsonElement>> ListIncidents()
        {
			DateTime nowTime = DateTime.Now;

			List<HttpContent> responseContent = await SendRequest(ResourceId+ "/api/incidents?$filter=lastUpdateTime+ge+"+_lastRequestTime);
			_lastRequestTime = nowTime.ToUniversalTime().ToString("o");
            List<JsonElement> incidents = new();

			//We will get 
			foreach (HttpContent _h in responseContent)
			{
				JsonDocument jsonDoc = await JsonDocument.ParseAsync(await _h.ReadAsStreamAsync());
				var value = jsonDoc.RootElement.EnumerateObject().FirstOrDefault(p => p.Name == "value");
				foreach (JsonElement _v in value.Value.EnumerateArray())
				{
					incidents.Add(_v);
				}

			}
			return incidents;
		}
	}
}

