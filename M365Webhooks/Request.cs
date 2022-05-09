using System.Net;
using System.Net.Http.Headers;

namespace M365Webhooks
{
	/// <summary>
    /// This class should only be inherited by classes representing various Microsoft APIs
    /// </summary>
	abstract class Request
	{
		private HttpClient _httpClient;
		private List<Credential> _credentials;

		public Request(string resourceId)
		{
			_credentials = Credential.GetCredentials(resourceId);
			_httpClient = new HttpClient();
		}

		/// <summary>
        /// Used by inheriting clases to send a HTTP rerquest to the Microsoft API chosen
        /// </summary>
        /// <param name="url">The API endpoint the HTTP request will be sent</param>
        /// <returns>Returns a task containing a list of HttpContent objects for consumption by the inheriting superclass</returns>
		protected async Task<List<HttpContent>> SendRequest(string url)
        {
			List<HttpContent> responseObjects = new List<HttpContent>();

			//Send a request to the API for every credential
			foreach (Credential _c in _credentials)
			{
				//If we specify debug level loging we will log here
				if (Configuration.debug)
				{
					if(Configuration.debugShowSecrets)
					{
						Console.WriteLine("[{0} - {1}]: Sending Request via URL: {2}, for Tennant ID: {3}, Application ID: {4}, using OAuth2 Token: {5}\n", DateTime.Now.ToShortDateString(),DateTime.Now.ToLongTimeString(), url, _c.TenantId, _c.AppId, _c.OauthToken);
						Log.WriteLine("Sending Request via URL: "+ url + ", for Tennant ID: "+_c.TenantId+", Application ID: "+_c.AppId+", using OAuth2 Token: "+_c.OauthToken);
					}
					else
                    {
						Console.WriteLine("[{0} - {1}]: Sending Request via URL: {2}, for Tennant ID: {3}, Application ID: {4}, using OAuth2 Token: [Show Secrets = false]\n", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString(), url, _c.TenantId, _c.AppId);
						Log.WriteLine("Sending Request via URL: " + url + ", for Tennant ID: " + _c.TenantId + ", Application ID: " + _c.AppId + ", using OAuth2 Token: [Show Secrets = false]");
					}
					
				}

				var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
				requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _c.OauthToken);

				var response = await _httpClient.SendAsync(requestMessage);

				//If we get HTTP 200 from the API
				if (response.StatusCode.Equals(HttpStatusCode.OK))
				{
					//Not empty body in response
					if (response.Content.Headers.ContentLength > 0)
					{
						responseObjects.Add(response.Content);
					}
					else
                    {
						//No content exception
						//throw
                    }
				}
                else
				{
					//Status code not 200 exception
					//throw
				}
			}

			return responseObjects;

		}

	}
}

