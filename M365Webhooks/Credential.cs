using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Security.Cryptography.X509Certificates;
using System.IdentityModel.Tokens.Jwt;

namespace M365Webhooks
{
	public class Credential
	{
        #region Private Members

        // List of Credentials which we will use to poll the relevant APIs continually
        private static readonly List<Credential> _credentials = new List<Credential>();
		private readonly string _tenantId;
		private readonly string _appId;
		private string _oauthToken;
        private JwtSecurityToken? _decodedOauthToken;
        private readonly object _credential;
        private readonly string _resourceId;

        #endregion

        public Credential(string tenantId, string appId, object credential, string resourceId)
		{

			_tenantId = tenantId;
			_appId = appId;
			_credential = credential;
            _resourceId = resourceId;
            _oauthToken = GetToken();

        }

        #region Private Methods

        // Decode the JWT OAuth2 Token
        private JwtSecurityToken DecodeToken(string oauthToken)
        {
            return new JwtSecurityTokenHandler().ReadJwtToken(oauthToken);
        }

        // Check if the supplied token is expired
        private bool CheckTokenExpired(JwtSecurityToken token)
        {
            return (token.ValidTo.AddMinutes(-Configuration.TokenExpires) < DateTime.UtcNow);
        }

        // Fetch OAuth2 Token from Azure AD app
        private string GetToken()
        {
			AuthenticationContext auth = new AuthenticationContext($"{Configuration.Authority}/{_tenantId}/");
			AuthenticationResult authenticationResult;

            if (Configuration.Debug)
            {
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
            if (Configuration.Debug)
            {
                // Only dump tokens if explicitely told to save tokens ending up in logs and debug output
                if (Configuration.DebugShowSecrets)
                { 
                    Log.WriteLine("Token: "+oauthToken);
                }
                else
                {
                    Log.WriteLine("Token: [DebugShowSecrets = false]");
                }
            }
            return oauthToken;
		}
        #endregion

        #region Public Methods

        /// <summary>
        /// Forces a new fetch of an OAuth2 token
        /// </summary>
        /// <returns>true/false represents if a new token was sucessfully retrieved</returns>
		public bool RefreshToken()
        {
			string newToken = GetToken();

            // Check the new token isn't null
            if (newToken.Equals(null))
            {
				return false;
            }

            JwtSecurityToken newDecodedToken = DecodeToken(newToken);

            // Check the new token isn't isn't expired
            if (CheckTokenExpired(newDecodedToken))
            {
                return false;
            }

            // All good set the new token
            _oauthToken = newToken;
            _decodedOauthToken = newDecodedToken;
            return true;
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Find credentials that work for the given TenantIds and AppIds
        /// </summary>
        /// <param name="resourceId"></param>
        /// <returns>A list of working credentials</returns>
		public static List<Credential> GetCredentials(string resourceId)
        {
            // Clear any existing credentials
            _credentials.Clear();

            // Iterate through all the user supplied configuration to find working credentials and build them into a list

            // Test each Tenant ID
            foreach (string tenantId in Configuration.TenantId)
            {
                // Test each App ID for every Tenant ID
                foreach (string appId in Configuration.AppId)
                {
                    bool found = false;

                    // Test each certificate for every App ID
                    foreach (string certPath in Configuration.CertificatePath)
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
                            foreach (string certPassword in Configuration.CertificatePassword)
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
                                    if (Configuration.DebugShowSecrets)
                                    {
                                        Log.WriteLine("Exception applying password to Certificate: " + certPath + " Password: " + certPassword + " Exception: " + ex.Message +" Inner Exception:" + ex.InnerException + " Source: " +ex.Source);
                                    }
                                    else
                                    {
                                        Log.WriteLine("Exception applying password to Certificate: " + certPath + " Password: [DebugShowSecrets = false] Exception: " + ex.Message + " Inner Exception:" + ex.InnerException + " Source: " + ex.Source);
                                    }
                                }
                            }
                        }
                    }

                    // If we haven't found a certificate try secret, this avoids the issue where a cached certificate credential is returned for an invalid secret
                    if (!found)
                    {
                        // Also test every App ID with each App Secret
                        foreach (string appSecret in Configuration.AppSecret)
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
                                    if (Configuration.DebugShowSecrets)
                                    {
                                        Log.WriteLine("Exception using app secret: " + appSecret + " TenantID: "+tenantId+" AppID: "+appId+" Exception: " + ex.Message + " Inner Exception:" + ex.InnerException + " Source: " + ex.Source);
                                    }
                                    else
                                    {
                                        Log.WriteLine("Exception using app secret: [DebugShowSecrets = false] TenantID: " + tenantId + " AppID: " + appId + " Exception: " + ex.Message + " Inner Exception:" + ex.InnerException + " Source: " + ex.Source);
                                    }
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
        public JwtSecurityToken? JWT { get { return _decodedOauthToken; } }
        public DateTime? Expires { get { return _decodedOauthToken.ValidTo; } }
        public bool Expired { get { return CheckTokenExpired(_decodedOauthToken); } } // We declare the token as expired TokenExpires minutes before real expire time
        public List<Credential> Credentials { get { return _credentials; } }

        #endregion
    }
}

