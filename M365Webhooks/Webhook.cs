using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace M365Webhooks
{
	/// <summary>
    /// This class can only be inherited to represent different webhook types i.e. Plain, Teams, Slack, Discord etc.
    /// </summary>
	abstract class Webhook
	{
        #region Private Members

        private readonly HttpClient _httpClient;
		private readonly string _webhookAddress;
		private readonly string _api;
		private readonly string _apiMethod;
		private readonly string _authType;
		private readonly string _auth;

        #endregion

        public Webhook(string webhookAddress, string api, string apiMethod, string authType="blank", string auth="")
		{
			_webhookAddress = webhookAddress;
			_httpClient = new HttpClient();
			_api = api;
			_apiMethod = apiMethod;
			_authType = authType;
			_auth = auth;
		}

		#region Protected Methods

		/// <summary>
		/// Creates a HTTP Request to be used to send webhook payload
		/// </summary>
		/// <param name="httpMethod">HTTP Method such as GET, POST, PUT</param>
		/// <returns>HttpRequestMessage to be used to send webhook</returns>
		private HttpRequestMessage CreateHttpRequest(HttpMethod httpMethod)
		{
			HttpRequestMessage requestMessage = new HttpRequestMessage(httpMethod, _webhookAddress);

			// If we supply an auth credential allocate it according to specified authType
			if (!string.IsNullOrEmpty(_auth))
			{
				switch (_authType.ToLower())
				{
					case "blank":

						requestMessage.Headers.TryAddWithoutValidation("Authorization", _auth);

						break;

					case "bearer":
						requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth);
						break;

					case "basic":
						requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", _auth);
						break;

					default:
						break;
				}
			}

			return requestMessage;
        }

		/// <summary>
        /// Checks that the HTTP code of the supplied response is HTTP 200 OK
        /// </summary>
        /// <param name="response">The response to check the HTTP code</param>
        /// <returns>true/false if HTTP 200 OK was responded</returns>
		protected bool CheckResponseHttpCode(HttpResponseMessage response)
        {
			if (response.StatusCode.Equals(HttpStatusCode.OK))
            {
				return true;
            }

			if(Configuration.Debug)
            {
				Log.WriteLine("Did not get HTTP 200 OK sending webhook, instead got "+response.StatusCode.ToString());
            }

			return false;
		}

		/// <summary>
        /// Sends the webhook payload
        /// </summary>
        /// <param name="httpMethod">HTTP Method such as GET, POST, PUT etc.</param>
        /// <param name="jsonBody">The JSON object you want to send in webhook</param>
        /// <returns>HTTP Response</returns>
		protected async Task<HttpResponseMessage> Send(HttpMethod httpMethod, object jsonBody)
        {

			HttpRequestMessage requestMessage = CreateHttpRequest(httpMethod);
			requestMessage.Content = new StringContent(JsonSerializer.Serialize(jsonBody));
			requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
			return await _httpClient.SendAsync(requestMessage);
		}

        #endregion

        #region Properties

        public string Api { get { return _api; } }
		public string ApiMethod { get { return _apiMethod; } }
		public string WebhookAddress { get { return _webhookAddress; } }

		#endregion

	}
}

