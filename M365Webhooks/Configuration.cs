using Microsoft.Extensions.Configuration;

namespace M365Webhooks
{
	public static class Configuration
	{
		private static readonly string _configPath = Environment.CurrentDirectory+"/config.json";
		private static readonly IConfiguration _config = new ConfigurationBuilder().AddJsonFile(_configPath).Build();

		#region Non-User Configurable
		// Sign-in Authority to get OAuth2 tokens
		public const string Authority = "https://login.microsoftonline.com";

		#endregion

		#region User Configurable

		// An array of Tenant IDs that host applications we want to poll Microsoft APIs using
		public static readonly string[] TenantId = _config.GetSection("AzureApplications").GetSection("tenantId").Get<string[]>();
		// An array of App IDs that are Azure AD applications we want to poll Microsoft APIs using
		public static readonly string[] AppId = _config.GetSection("AzureApplications").GetSection("appId").Get<string[]>();
		// Array of certificate paths to use as auth against Azure AD applications
		public static readonly string[] CertificatePath = _config.GetSection("AzureApplications").GetSection("certificatePath").Get<string[]>();
		// Array of passwords to use with the certificates
		public static readonly string[] CertificatePassword = _config.GetSection("AzureApplications").GetSection("certificatePassword").Get<string[]>();
		// Array of app secrets to use
		public static readonly string[] AppSecret = _config.GetSection("AzureApplications").GetSection("appSecret").Get<string[]>();
		// Array of webhook addresses to send webhooks to
		public static readonly string[] WebhookAddress = _config.GetSection("Webhooks").GetSection("webhookAddress").Get<string[]>();
		// Array of webhook types
		public static readonly string[] WebhookType = _config.GetSection("Webhooks").GetSection("webhookType").Get<string[]>();
		// Array of webhook authentication scheme types
		public static readonly string[] WebhookAuthType = _config.GetSection("Webhooks").GetSection("webhookAuthType").Get<string[]>();
		// Array of auth tokens
		public static readonly string[] WebhookAuth = _config.GetSection("Webhooks").GetSection("webhookAuth").Get<string[]>();
		// Array of API names that match the APIs we want to pull data from
		public static readonly string[] Api = _config.GetSection("Webhooks").GetSection("api").Get<string[]>();
		// Array of method names that represent which method to target on each API used by each webhook
		public static readonly string[] ApiMethod = _config.GetSection("Webhooks").GetSection("apiMethod").Get<string[]>();
		// File path to save the log file
		public static readonly string LogPath = _config.GetSection("LogPath").Get<string>();
        #if DEBUG
		// Set to true so we log debug events
		public static bool Debug = true;
        #else
		public static bool Debug = _config.GetSection("Debug").Get<string>().ToLower().Equals("true");
        #endif
		// Set to true to show secrets such as certificate passwords or OAuth2 tokens in log and console
		public static bool DebugShowSecrets = _config.GetSection("DebugShowSecrets").Get<string>().ToLower().Equals("true");
		// Set to decide how long before a token expires we declare it already expired [ Minutes 0 - 58 ]
		public static int TokenExpires = _config.GetSection("TokenExpires").Get<int>();
		// Poll the API in this interval [ Minutes 3 - 2147483647 ] 
		public static int PollingTime = _config.GetSection("PollingTime").Get<int>();
		// On start up fetch back this many minutes from the APIs [ Minutes 0 - 2147483647 ]
		public static int StartFetchMinutes = _config.GetSection("StartFetchMinutes").Get<int>();

		#endregion

	}
}

