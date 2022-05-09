using Microsoft.Extensions.Configuration;

namespace M365Webhooks
{
	public static class Configuration
	{
		private static IConfiguration config = new ConfigurationBuilder().AddJsonFile("/Users/hdizzle/Projects/M365Webhooks/M365Webhooks/config.json").Build();

		#region Non-User Configurable

		public const string authority = "https://login.microsoftonline.com";
		public const string office365Management = "https://manage.office.com";
		public const string windowsDefenderAtp = "https://api.securitycenter.microsoft.com";
		public const string microsoftGraph = "https://graph.microsoft.com";

		#endregion

		#region User Configurable

		public static readonly string[] tenantId = config.GetSection("AzureApplications").GetSection("tenantId").Get<string[]>();
		public static readonly string[] appId = config.GetSection("AzureApplications").GetSection("appId").Get<string[]>();
		public static readonly string[] certificatePath = config.GetSection("AzureApplications").GetSection("certificatePath").Get<string[]>();
		public static readonly string[] certificatePassword = config.GetSection("AzureApplications").GetSection("certificatePassword").Get<string[]>();
		public static readonly string[] appSecret = config.GetSection("AzureApplications").GetSection("appSecret").Get<string[]>();
        #if DEBUG
		public static readonly bool debug = true;
        #else
		public static readonly bool debug = config.GetSection("Debug").Get<string>().ToLower().Equals("true");
        #endif
		public static readonly bool debugShowSecrets = config.GetSection("DebugShowSecrets").Get<string>().ToLower().Equals("true");
		public static readonly string logPath = config.GetSection("LogPath").Get<string>();
		#endregion

	}
}

