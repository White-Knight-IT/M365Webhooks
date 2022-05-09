using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Security.Cryptography.X509Certificates;
using System.IdentityModel.Tokens.Jwt;

namespace M365Webhooks
{
	public class Credential
	{
		// List of Credentials which we will use to poll the relevant APIs continually
		private static List<Credential> _credentials = new List<Credential>();

		#region Internal Members

		private string _tenantId;
		private string _appId;
		private string _oauthToken;
        private JwtSecurityToken _decodedOauthToken;
        private object _credential;
        private string _resourceId;

        #endregion

        public Credential(string tenantId, string appId, object credential, string resourceId)
		{

			_tenantId = tenantId;
			_appId = appId;
			_credential = credential;
            _resourceId = resourceId;
            _oauthToken = GetToken();

        }

        #region Internal Methods

        // Decode the JWT OAuth2 Token
        private JwtSecurityToken DecodeToken(string oauthToken)
        {
            return new JwtSecurityTokenHandler().ReadJwtToken(oauthToken);
        }

        // Fetch OAuth2 Token from Azure AD app
        private string GetToken()
        {
			AuthenticationContext auth = new AuthenticationContext($"{Configuration.authority}/{_tenantId}/");
			AuthenticationResult authenticationResult;

            if (Configuration.debug)
            {
                Console.WriteLine("[{0} - {1}]: Attempting to get token for Tenant ID: {2} and App ID: {3}\n", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString(), _tenantId, _appId);
                Log.WriteLine("Attempting to get token for Tenant ID: "+_tenantId+" and App ID: "+_appId);
            }

			switch (_credential)
			{
				// Credential is a certificate
				case ClientAssertionCertificate:
					authenticationResult = auth.AcquireTokenAsync(_resourceId, (ClientAssertionCertificate)_credential).GetAwaiter().GetResult();
					break;

				// Credential is an App Secret
				default:
					authenticationResult = auth.AcquireTokenAsync(_resourceId, (ClientCredential)_credential).GetAwaiter().GetResult();
					break;			
			}

            string oauthToken = authenticationResult.AccessToken;
            _decodedOauthToken=DecodeToken(oauthToken);
            if (Configuration.debug)
            {
                // Only dump tokens if explicitely told to save tokens ending up in logs and debug output
                if (Configuration.debugShowSecrets)
                {
                    Console.WriteLine("[{0} - {1}]: Token: {0}\n", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString(), oauthToken);
                    Log.WriteLine("Token: "+oauthToken);
                }
                else
                {
                    Console.WriteLine("[{0} - {1}]: Token: [Show Secrets = false]\n", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString());
                    Log.WriteLine("Token: [Show Secrets = false]");
                }
            }
            return oauthToken;
		}
        #endregion

        #region Public Methods

		public bool RefreshToken()
        {
			_oauthToken = GetToken();

            // Check the new token isn't null and that it isn't expired
			if (_oauthToken.Equals(null) || Expired)
            {
				return false;
            }

                return true;
        }

        #endregion

        #region Public Static Methods

		public static List<Credential> GetCredentials(string resourceId)
        {
            // Clear any existing credentials
            _credentials.Clear();

            // Iterate through all the user supplied configuration to find working credentials and build them into a list

            // Test each Tenant ID
            foreach (string tenantId in Configuration.tenantId)
            {
                // Test each App ID for every Tenant ID
                foreach (string appId in Configuration.appId)
                {
                    bool found = false;

                    // Test each certificate for every App ID
                    foreach (string certPath in Configuration.certificatePath)
                    {
                        try
                        {
                            // Try certificate with no password first
                            ClientAssertionCertificate authCert = new ClientAssertionCertificate(appId, new X509Certificate2(certPath));
                            _credentials.Add(new Credential(tenantId, appId, authCert, resourceId));
                            found = true;
                            break;
                        }
                        catch
                        {
                            // Failed trying certificate with no password so try with each password
                            foreach (string certPassword in Configuration.certificatePassword)
                            {
                                try
                                {
                                    ClientAssertionCertificate authCert = new ClientAssertionCertificate(appId, new X509Certificate2(certPath, certPassword));
                                    _credentials.Add(new Credential(tenantId, appId, authCert, resourceId));
                                    found = true;
                                    break;
                                }
                                catch (Exception ex)
                                { 
                                    //throw;
                                }
                            }
                        }
                    }

                    // If we haven't found a certificate try secret, this avoids the issue where a cached certificate credential is returned for an invalid secret
                    if (!found)
                    {
                        // Also test every App ID with each App Secret
                        foreach (string appSecret in Configuration.appSecret)
                        {
                            if (!appSecret.Equals(null) && appSecret != "")
                            {
                                try
                                {
                                    ClientCredential clientCredential = new ClientCredential(appId, appSecret);
                                    _credentials.Add(new Credential(tenantId, appId, clientCredential, resourceId));
                                    found = true;
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    //throw;
                                }
                            }
                        }
                    }
                }
            }
            return _credentials;
        }

        #endregion

        #region Properties

        public string TenantId { get { return _tenantId; } }
		public string AppId { get { return _appId; } }
		public string OauthToken { get { return _oauthToken; } }
        public string ResourceID { get { return _resourceId; } }
        public JwtSecurityToken JWT { get { return _decodedOauthToken; } }
        public DateTime Expires { get { return _decodedOauthToken.ValidTo; } }
        public bool Expired { get { return _decodedOauthToken.ValidTo.AddSeconds(-30) < DateTime.UtcNow; } }
        public List<Credential> Credentials { get { return _credentials; } }

        #endregion
    }
}

