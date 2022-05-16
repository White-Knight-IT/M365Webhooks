using System.Text.Json;

namespace M365Webhooks.API
{
	/// <summary>
	/// A class to represent the Office 365 Management API
	/// ### Permissions Required: ActivityFeed.Read, ActivityFeed.ReadDlp, ServiceHealth.Read ###
	/// </summary>
	internal class Office365Management : Request
	{
		#region Private Members

		//The resource we obtain our JWT OAuth2 token for
		public const string ResourceId = "https://manage.office.com";
		public const string ApiVersion = "v1.0";
		private static readonly string[] _roles = new string[] { "ActivityFeed.Read", "ActivityFeed.ReadDlp", "ServiceHealth.Read" };
		private readonly int _tenantsSubscribed;
		private readonly string[] _contentTypes = new string[] { "Audit.AzureActiveDirectory", "Audit.Exchange", "Audit.SharePoint", "Audit.General", "DLP.All" };

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

		/// <summary>
        /// Subscribes to the Office 365 Management API activity feed for all catagories, across all tenants
        /// </summary>
        /// <returns>Number of tenants subscribed</returns>
		private async Task<int> Subscribe()
        {
			if (Configuration.Debug)
			{
				Log.WriteLine("It may be expected to see some HTTP 400 BadRequest responses below if we are already subscribed for the activity feeds");
			}

			// Subscribe to our specified content type
			async Task<List<HttpContent>> SendSubscribe(string contentType)
            {
				return await SendRequest(ResourceId + "/api/" + ApiVersion + "/{TENANTID}/activity/feed/subscriptions/start?contentType="+contentType+ "&PublisherIdentifier={TENANTID}", HttpMethod.Post);
			}

			foreach(string _s in _contentTypes)
            {
				await SendSubscribe(_s);
            }

			// List subscriptions
			List<HttpContent> response = await SendRequest(ResourceId + "/api/" + ApiVersion + "/{TENANTID}/activity/feed/subscriptions/list?PublisherIdentifier={TENANTID}", HttpMethod.Get);

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

					if(_v.EnumerateObject().FirstOrDefault(p => p.Name == "status").Value.GetString().ToLower().Equals("enabled"))
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

		public async Task<List<JsonElement>> Activities()
        {
			string nowTime = DateTime.Now.ToUniversalTime().ToString("o").Split('.')[0];
			LastRequestTime = LastRequestTime.Split('.')[0];

			// The API returns 400 BadRequest if our start/end times are so much as 1 second over a 24h spread
			if(DateTime.Parse(nowTime).CompareTo((DateTime.Parse(LastRequestTime).AddHours(24)))>0)
            {
				// Reduce our time spread to precisely 24h to satisfy the API
				nowTime= DateTime.Parse(LastRequestTime).AddHours(24).ToString("o").Split('.')[0];

			}

			// Get the Activities for our specified content type
			async Task<List<HttpContent>> GetActivities(string contentType)
            {
				return await SendRequest(ResourceId + "/api/" + ApiVersion + "/{TENANTID}/activity/feed/subscriptions/content?contentType="+contentType+ "&PublisherIdentifier={TENANTID}&startTime=" + LastRequestTime + "&endTime=" + nowTime, HttpMethod.Get);
			}

			List<HttpContent> responseContent = new();

			foreach (string _s in _contentTypes)
            {
				// Microsoft paginates if there are over 100 entries so we check for NextPageUri header which directs us
				async void IteratePages(HttpContent httpContent)
				{
					string nextPageUrl = String.Empty;

					responseContent.Add(httpContent);

					while (httpContent.Headers.TryGetValues("NextPageUri", out var nextPage))
					{
						nextPageUrl = nextPage.First();

						foreach (HttpContent _h in await SendRequest(nextPageUrl, HttpMethod.Get))
                        {
							httpContent = _h;
							responseContent.Add(httpContent);
						}

					}

				}

				foreach (HttpContent _h in await GetActivities(_s))
                {
					IteratePages(_h);
                }
			}

			LastRequestTime = nowTime;
            List<string> activities = new();

			// Get the activity urls out into a list for us to request
			foreach (HttpContent _h in responseContent)
			{
				JsonDocument jsonDoc = await JsonDocument.ParseAsync(await _h.ReadAsStreamAsync());
				foreach (JsonElement _v in jsonDoc.RootElement.EnumerateArray())
				{
					activities.Add(_v.EnumerateObject().FirstOrDefault(p => p.Name == "contentUri").Value.GetString()); ;
				}

			}

			// Now we fetch the activity arrays from the urls and parse them into activity objects we send to webhook

			List<JsonElement> returnActivities =new();

			foreach (string _s in activities)
            {
				string tenantIdFromUrl = _s.Replace(ResourceId + "/api/" + ApiVersion + "/", "").Substring(0,36);
				List<HttpContent> response = await SendRequest(_s+ "?PublisherIdentifier="+tenantIdFromUrl, HttpMethod.Get, tenantIdFromUrl);

				foreach(HttpContent _h in response)
                {
					JsonDocument jsonDoc = await JsonDocument.ParseAsync(await _h.ReadAsStreamAsync());
					foreach (JsonElement _v in jsonDoc.RootElement.EnumerateArray())
                    {
						returnActivities.Add(_v);

					}

				}

			}
			return returnActivities;
		}

        #endregion
    }
}

