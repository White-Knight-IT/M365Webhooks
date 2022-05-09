using Microsoft.Extensions.Configuration;

namespace M365Webhooks
{
	public static class Configuration
	{
		private static IConfiguration _config = new ConfigurationBuilder().AddJsonFile("/Users/hdizzle/Projects/M365Webhooks/M365Webhooks/config.json").Build();

		#region Non-User Configurable

		public const string Authority = "https://login.microsoftonline.com";
		public const string Office365Management = "https://manage.office.com";
		public const string WindowsDefenderAtp = "https://api.securitycenter.microsoft.com";
		public const string MicrosoftGraph = "https://graph.microsoft.com";

		#endregion

		#region User Configurable

		public static readonly string[] TenantId = _config.GetSection("AzureApplications").GetSection("tenantId").Get<string[]>();
		public static readonly string[] AppId = _config.GetSection("AzureApplications").GetSection("appId").Get<string[]>();
		public static readonly string[] CertificatePath = _config.GetSection("AzureApplications").GetSection("certificatePath").Get<string[]>();
		public static readonly string[] CertificatePassword = _config.GetSection("AzureApplications").GetSection("certificatePassword").Get<string[]>();
		public static readonly string[] AppSecret = _config.GetSection("AzureApplications").GetSection("appSecret").Get<string[]>();
		public static readonly string LogPath = _config.GetSection("LogPath").Get<string>();
        #if DEBUG
		public static bool Debug = true;
        #else
		public static bool Debug = config.GetSection("Debug").Get<string>().ToLower().Equals("true");
        #endif
		public static bool DebugShowSecrets = _config.GetSection("DebugShowSecrets").Get<string>().ToLower().Equals("true");
		public static int TokenExpires = _config.GetSection("TokenExpires").Get<int>();
		#endregion

	}
}

