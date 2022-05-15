using System.Text.Json;

namespace M365Webhooks.API
{
	/// <summary>
    /// A class to represent the Microsoft Threat Protection API
    /// ### Permissions Required: Incident.Read.All ###
    /// </summary>
	internal class Office365Management : Request
	{
		#region Private Members

		//The resource we obtain our JWT OAuth2 token for
		public const string ResourceId = "https://manage.office.com/";
		public const string ApiVersion = "v1.0";
		private static readonly string[] _roles = new string[] { "ActivityFeed.Read", "ActivityFeed.ReadDlp", "ServiceHealth.Read" };
		private int _tenantsSubscribed;

		#endregion

		public Office365Management():base(ResourceId, _roles)
		{
			_tenantsSubscribed = Subscribe().Result;

			if(Configuration.Debug)
            {
				Log.WriteLine(_tenantsSubscribed.ToString()+" subscribed to Office 365 Management API activity feed");
            }
		}

		#region Private Methods

		private async Task<int> Subscribe()
        {
			if (Configuration.Debug)
			{
				Log.WriteLine("It may be expected to see some HTTP 400 BadRequest responses below if we are already subscribed for the activity feeds");
			}

			// Subscribe to all the things (It will give HTTP 400 if already subscribed so we may see some bad request errors to ignore)
			await SendRequest(ResourceId + "/api/" + ApiVersion + "/{TENANTID}/activity/feed/subscriptions/start?contentType=Audit.AzureActiveDirectory", HttpMethod.Post); //+DateTime.Now.ToUniversalTime().AddDays(-30).ToString("o") SUB -> ResourceId + "/api/" + ApiVersion + "/{TENANTID}/activity/feed/subscriptions/start?contentType=Audit.Exchange"
			await SendRequest(ResourceId + "/api/" + ApiVersion + "/{TENANTID}/activity/feed/subscriptions/start?contentType=Audit.Exchange", HttpMethod.Post);
			await SendRequest(ResourceId + "/api/" + ApiVersion + "/{TENANTID}/activity/feed/subscriptions/start?contentType=Audit.SharePoint", HttpMethod.Post);
			await SendRequest(ResourceId + "/api/" + ApiVersion + "/{TENANTID}/activity/feed/subscriptions/start?contentType=DLP.All", HttpMethod.Post);

			// List subscriptions
			List<HttpContent> response = await SendRequest(ResourceId + "/api/" + ApiVersion + "/{TENANTID}/activity/feed/subscriptions/list", HttpMethod.Get);

			int subsTally = 0;

			foreach (HttpContent _h in response)
			{
				JsonDocument jsonDoc = await JsonDocument.ParseAsync(await _h.ReadAsStreamAsync());

				int enabledSubs = 0;

				// Check that all 5 subscriptions we expect are enabled
				foreach (JsonElement _v in jsonDoc.RootElement.EnumerateArray())
                { 
					// Dump each subscription status
					if (Configuration.Debug)
					{
						Log.WriteLine(_v.ToString());
					}

					if(_v.EnumerateObject().FirstOrDefault(p => p.Name == "status").Value.ToString().ToLower().Equals("enabled"))
                    {
						enabledSubs++;
                    }
				}

				if(enabledSubs >=5)
                {
					subsTally++;
				}
			}

			return subsTally;
		}

		#endregion

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

