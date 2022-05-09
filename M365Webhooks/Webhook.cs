namespace M365Webhooks
{
	abstract class Webhook
	{
		private string _webhookAddress;

		public Webhook(string webhookAddress)
		{
			_webhookAddress = webhookAddress;
		}
	}
}

