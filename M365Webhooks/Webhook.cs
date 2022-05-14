using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace M365Webhooks
{
	abstract class Webhook
	{
        #region Private Members

        private HttpClient _httpClient;
		private string _webhookAddress;
		private string _api;
		private string _apiMethod;
		private string _authType;
		private string _auth;

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

        private HttpRequestMessage CreateHttpRequest(HttpMethod httpMethod)
		{
			HttpRequestMessage requestMessage = new HttpRequestMessage(httpMethod, _webhookAddress);

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

			return requestMessage;
        }

		protected bool CheckResponseHttpCode(HttpResponseMessage response)
        {
			if (response.StatusCode.Equals(HttpStatusCode.OK))
            {
				return true;
            }

			return false;
		}

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

        #endregion

    }
}

