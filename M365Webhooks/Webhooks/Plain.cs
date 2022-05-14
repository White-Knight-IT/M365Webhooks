namespace M365Webhooks.Webhooks
{
	internal class Plain : Webhook
	{
		public Plain(string webhookAddress, string api, string apiMethod, string authType, string auth) : base(webhookAddress, api, apiMethod, authType, auth)
		{

		}

		public async Task<bool> SendWebhook(object jsonBody)
        {
			HttpResponseMessage response = await Send(HttpMethod.Post, jsonBody);
            return CheckResponseHttpCode(response);
        }
	}
}