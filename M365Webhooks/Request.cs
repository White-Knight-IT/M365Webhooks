using System.Net;
using System.Net.Http.Headers;

namespace M365Webhooks
{
	/// <summary>
    /// This class should only be inherited by classes representing various Microsoft APIs
    /// </summary>
	abstract class Request
	{
		#region Private Members

		private readonly HttpClient _httpClient;
		private readonly List<Credential> _credentials;

		//Request will only seek data that is >= this time
		private string _lastRequestTime;

        #endregion

        public Request(string resourceId)
		{
			_credentials = Credential.GetCredentials(resourceId);
			_httpClient = new HttpClient();

			//On first run we get all incidents in the last 24 hours
			_lastRequestTime = DateTime.Now.ToUniversalTime().AddMinutes(-Configuration.StartFetchMinutes).ToString("o");
		}

		#region Protected Methods

		/// <summary>
		/// Used by inheriting clases to send a HTTP rerquest to the Microsoft API chosen
		/// </summary>
		/// <param name="url">The API endpoint the HTTP request will be sent</param>
		/// <param name="httpMethod">HTTP request method i.e GET, PATCH, POST, PUT or DELETE</param>
		/// <returns>Returns a task containing a list of HttpContent objects for consumption by the inheriting superclass</returns>
		protected async Task<List<HttpContent>> SendRequest(string url, HttpMethod httpMethod)
        {
			List<HttpContent> responseObjects = new List<HttpContent>();

			//Send a request to the API for every credential
			foreach (Credential _c in _credentials)
			{
				// Check token not expired
				if(_c.Expired)
                {
					if(!_c.RefreshToken())
                    {
						// We were not successful refreshing
						if (Configuration.DebugShowSecrets)
						{
							Log.WriteLine("Failed to refresh expired token: " + _c.OauthToken);
						}
						else
                        {
							Log.WriteLine("Failed to refresh expired token: [DebugShowSecrets = false]");
						}
						continue;
                    }
                }
				//If we specify debug level loging we will log here
				if (Configuration.Debug)
				{
					if(Configuration.DebugShowSecrets)
					{
						Log.WriteLine("Sending Request via URL: "+ url + ", for Tennant ID: "+_c.TenantId+", Application ID: "+_c.AppId+", using OAuth2 Token: "+_c.OauthToken);
					}
					else
                    {
						Log.WriteLine("Sending Request via URL: " + url + ", for Tennant ID: " + _c.TenantId + ", Application ID: " + _c.AppId + ", using OAuth2 Token: [DebugShowSecrets = false]");
					}
					
				}

				var requestMessage = new HttpRequestMessage(httpMethod, url);
				requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _c.OauthToken);

				var response = await _httpClient.SendAsync(requestMessage);

				//If we dont get get HTTP 200 from the API
				if (!response.StatusCode.Equals(HttpStatusCode.OK))
				{
					Log.WriteLine("Did not get HTTP 200 OK from: " + url + " instead we got: "+response.StatusCode.ToString());
					continue;
				}

				//Empty body in response
				if (response.Content.Headers.ContentLength <= 0)
				{
					Log.WriteLine("Did not get any content from: " + url);
					continue;
				}

				responseObjects.Add(response.Content);

			}

			return responseObjects;

		}

		#endregion

		#region Properties

		protected string LastRequestTime { get { return _lastRequestTime; } set { _lastRequestTime = value; } }

        #endregion
    }
}

