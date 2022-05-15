using System.Text.Json;

namespace M365Webhooks.API
{
	/// <summary>
    /// A class to represent the Microsoft Threat Protection API
    /// ### WIP NOT WORKING ###
    /// </summary>
	internal class MicrosoftGraphSecurity : Request
	{
		#region Internal Members

		//The resource we obtain our JWT OAuth2 token for
		public const string ResourceId = "https://graph.microsoft.com";
		public const string ApiVersion = "v1.0";
		private static readonly string[] _roles = new string[] { "ActivityFeed.Read", "ActivityFeed.ReadDlp", "ServiceHealth.Read" };

		#endregion

		public MicrosoftGraphSecurity():base(ResourceId, _roles)
		{

		}

		#region Public Methods

		/// <summary>
		/// Gets incidents from https://graph.microsoft.com/ApiVersion/security/incidents
		/// </summary>
		/// <returns>List of JSON Objects, each Object is an Incident see https://docs.microsoft.com/en-us/microsoft-365/security/defender/api-list-incidents?view=o365-worldwide</returns>
		/*
		public async Task<List<JsonElement>> ListIncidents()
        {
			DateTime nowTime = DateTime.Now;

			List<HttpContent> responseContent = await SendRequest(ResourceId+"/"+ApiVersion+"/security/incidents?$filter=lastUpdateTime+ge+"+LastRequestTime,HttpMethod.Get);
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
		}*/

        #endregion
    }
}

