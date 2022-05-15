using System.Text.Json;

namespace M365Webhooks.API
{
	/// <summary>
    /// A class to represent the Microsoft Threat Protection API
    /// ### Permissions Required: Incident.Read.All ###
    /// </summary>
	internal class MicrosoftThreatProtection : Request
	{
		#region Private Members

		//The resource we obtain our JWT OAuth2 token for
		public const string ResourceId = "https://api.security.microsoft.com";
		private static readonly string[] _roles = new string[] { "Incident.Read.All","AdvancedHunting.Read.All" };

        #endregion

        public MicrosoftThreatProtection():base(ResourceId, _roles)
		{

		}

		#region Public Methods

		/// <summary>
		/// Gets incidents from https://api.security.microsoft.com/api/incidents
		/// </summary>
		/// <returns>List of JSON Objects, each Object is an Incident see https://docs.microsoft.com/en-us/microsoft-365/security/defender/api-list-incidents?view=o365-worldwide</returns>
		public async Task<List<JsonElement>> Incidents()
        {
			DateTime nowTime = DateTime.Now;

			List<HttpContent> responseContent = await SendRequest(ResourceId+ "/api/incidents?$filter=lastUpdateTime+ge+"+LastRequestTime,HttpMethod.Get);
			LastRequestTime = nowTime.ToUniversalTime().ToString("o");
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

        #endregion
    }
}

