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
		private readonly Dictionary<string,Credential> _credentials;

		//Request will only seek data that is >= this time
		private string _lastRequestTime;

        #endregion

        public Request(string resourceId, string[] roleCheck)
		{
			_credentials = Credential.GetCredentials(resourceId, roleCheck);
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
		/// <param name="tenantId" [optional=""]>If set with a TenantID the request is pinned to a specific tenant credential</param>
		/// <returns>Returns a task containing a list of HttpContent objects for consumption by the inheriting superclass</returns>
		protected async Task<List<HttpContent>> SendRequest(string url, HttpMethod httpMethod, string tenantId="")
        {
			string originUrl = url;

			List<HttpContent> responseObjects = new();


			//Send a request to the API for every credential
			foreach (Credential _c in _credentials.Values.ToList())
			{
				Credential useForRequest = _c;

				async Task<bool> GetContent()
				{
					
					// Inject any credential information into the URL string
					url = originUrl.Replace("{TENANTID}", useForRequest.TenantId);

					// Check token not expired
					if (useForRequest.Expired)
					{
						if (!useForRequest.RefreshToken())
						{
							// We were not successful refreshing
							if (Configuration.DebugShowSecrets)
							{
								Log.WriteLine("Failed to refresh expired token: " + useForRequest.OauthToken);
							}
							else
							{
								Log.WriteLine("Failed to refresh expired token: [DebugShowSecrets = false]");
							}
							return false;
						}
					}

					//If we specify debug level loging we will log here
					if (Configuration.Debug)
					{
						if (Configuration.DebugShowSecrets)
						{
							Log.WriteLine("Sending Request via URL: " + url + ", for Tennant ID: " + useForRequest.TenantId + ", Application ID: " + useForRequest.AppId + ", using OAuth2 Token: " + useForRequest.OauthToken);
						}
						else
						{
							Log.WriteLine("Sending Request via URL: " + url + ", for Tennant ID: " + useForRequest.TenantId + ", Application ID: " + useForRequest.AppId + ", using OAuth2 Token: [DebugShowSecrets = false]");
						}

					}

					var requestMessage = new HttpRequestMessage(httpMethod, url);
					requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", useForRequest.OauthToken);

					// Requests can throw exceptions for a number of reasons, we really don't want to stop the whole show for an exception from a single request
					try
					{
						var response = await _httpClient.SendAsync(requestMessage);

						//If we dont get get HTTP 200 from the API
						if (!response.StatusCode.Equals(HttpStatusCode.OK))
						{
							Log.WriteLine("Did not get HTTP 200 OK from: " + url + " instead we got: " + response.StatusCode.ToString());
							return false;
						}

						//Empty body in response
						if (response.Content.Headers.ContentLength <= 0)
						{
							Log.WriteLine("Did not get any content from: " + url);
							return false;
						}

						responseObjects.Add(response.Content);
					}
                    catch(Exception ex)
                    {
						Log.WriteLine("Exception occured sending HTTP request: "+ex.Message+" Inner exception: "+ex.InnerException.Message+" Source: "+ex.Source);
						return false;
                    }

					return true;
				}

				// Pinned to a single tenantId, send with only that credential then break loop
				if (tenantId != "")
				{
					useForRequest = _credentials[tenantId];
					await GetContent();
					break;
				}

				await GetContent();

			}

			return responseObjects;

		}

		#endregion

		#region Properties

		protected string LastRequestTime { get { return _lastRequestTime; } set { _lastRequestTime = value; } }

        #endregion
    }
}

