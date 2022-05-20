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

			async Task<bool> CheckSend(bool retry)
			{
				// If webhook failed do a resend
				if (!CheckResponseHttpCode(response) && !Configuration.EndingProgram)
				{
					if (retry)
					{
						if (Configuration.Debug)
						{
							Log.WriteLine("Resending failed webhook");
						}

						response = await Send(HttpMethod.Post, jsonBody);
						return await CheckSend(false);
					}
					else
                    {
						Log.WriteLine("Failed to resend webhook");
						return false;
                    }
				}

				return true;
			}

			return await CheckSend(true);
		}
	}
}