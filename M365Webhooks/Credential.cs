using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Security.Cryptography.X509Certificates;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace M365Webhooks
{
	public class Credential
	{
        #region Private Members

		private readonly string _tenantId;
		private readonly string _appId;
		private string _oauthToken;
        private JwtSecurityToken _decodedOauthToken;
        private readonly object _credential;
        private readonly string _resourceId;
        private const int _timeMargin = 4;

        #endregion

        public Credential(string tenantId, string appId, object credential, string resourceId)
		{

			_tenantId = tenantId;
			_appId = appId;
			_credential = credential;
            _resourceId = resourceId;

            // _oauthToken and _decodedOauthToken populate in this call
            GetToken();

        }

        #region Private Methods

        // Check if the supplied token is expired
        private bool CheckTokenExpired()
        {
            return _decodedOauthToken.ValidTo.AddMinutes(-_timeMargin) < DateTime.UtcNow;
        }

        // Fetch OAuth2 Token from Azure AD app
        private void GetToken()
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
					authenticationResult = auth.AcquireTokenAsync(_resourceId, (ClientAssertionCertificate)_credential).Result;
					break;

				// Credential is an App Secret
				default:
					authenticationResult = auth.AcquireTokenAsync(_resourceId, (ClientCredential)_credential).Result;
					break;			
			}

            _oauthToken = authenticationResult.AccessToken;
            _decodedOauthToken= new JwtSecurityTokenHandler().ReadJwtToken(_oauthToken);

            // Check that our machine time is set ok
            if(_decodedOauthToken.IssuedAt>DateTime.UtcNow.AddMinutes(-2) || _decodedOauthToken.IssuedAt <= DateTime.UtcNow.AddMinutes(-7))
            {
                Log.WriteLine("Machine time does not appear in sync with Microsoft Servers, Local time: " + DateTime.UtcNow.ToLongTimeString() + " Remote time: " + _decodedOauthToken.IssuedAt.ToLongTimeString(), true);
                throw (new MachineTime("The system time deviates too far from the time on Microsoft Servers"));
            }

            if (Configuration.Debug)
            {
                // Only dump tokens if explicitely told to save tokens ending up in logs and debug output
                if (Configuration.DebugShowSecrets)
                { 
                    Log.WriteLine("Token: "+_oauthToken);
                }
                else
                {
                    Log.WriteLine("Token: [DebugShowSecrets = false]");
                }
            }
		}      

        #endregion

        #region Public Methods

        /// <summary>
        /// Forces a new fetch of an OAuth2 token
        /// </summary>
        /// <returns>true/false represents if a new token was sucessfully retrieved</returns>
		public bool RefreshToken()
        {
            try
            {
                GetToken();

                // Check the new token isn't null
                if (OauthToken.Equals(null))
                {
                    return false;
                }

                if(Configuration.Debug)
                {
                    if (Configuration.DebugShowSecrets)
                    {
                        Log.WriteLine("Successfully refreshed token for TenantId: " + _tenantId + " and AppId: " + _appId+" Token: "+_oauthToken);
                    }
                    else
                    {
                        Log.WriteLine("Successfully refreshed token for TenantId: " + _tenantId + " and AppId: " + _appId + " Token: [DebugShowSecrets = false]");
                    }
                }

                return true;
            }
            catch(Exception ex)
            {
                Log.WriteLine("Exception refreshing token: " + ex.Message + " Inner Exception: " + ex.InnerException.Message + " Source: " + ex.Source);
            }

            return false;
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Iterate through user supplied configuration to find credentials that work for the given TenantIds and AppIds and Api Roles
        /// </summary>
        /// <param name="resourceId"></param>
        /// <returns>A list of working credentials</returns>
		public static Dictionary<string,Credential> GetCredentials(string resourceId, string[] roleCheck)
        {
            Dictionary<string, Credential> _credentials = new();

            // Check the roles on the token against what the API expects, return true if the token has all expected roles
            static bool CheckRoles(string[] roleCheck, List<Claim> claims)
            {
                int roleHit = 0;

                foreach (string _s in roleCheck)
                {
                    foreach (Claim _cl in claims)
                    {
                        if (_cl.Value.Equals(_s))
                        {
                            roleHit++;

                            // If we find the token has as many matching roles as expected we add it to the list to use
                            if (roleHit == roleCheck.Length)
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }

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
                            Credential credential = new Credential(tenantId, appId, authCert, resourceId);

                            if (CheckRoles(roleCheck, credential.JWT.Claims.ToList()))
                            {
                                _credentials.Add(tenantId,credential);
                            }

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
                                    if (!string.IsNullOrEmpty(certPassword))
                                    {
                                        ClientAssertionCertificate authCert = new ClientAssertionCertificate(appId, new X509Certificate2(certPath, certPassword));
                                        Credential credential = new Credential(tenantId, appId, authCert, resourceId);

                                        if (CheckRoles(roleCheck, credential.JWT.Claims.ToList()))
                                        {
                                            _credentials.Add(tenantId,credential);
                                        }

                                        found = true;
                                        break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    if (Configuration.DebugShowSecrets)
                                    {
                                        Log.WriteLine("Exception applying password to Certificate: " + certPath + " Password: " + certPassword + " Exception: " + ex.Message +" Inner Exception:" + ex.InnerException.Message + " Source: " +ex.Source);
                                    }
                                    else
                                    {
                                        Log.WriteLine("Exception applying password to Certificate: " + certPath + " Password: [DebugShowSecrets = false] Exception: " + ex.Message + " Inner Exception:" + ex.InnerException.Message + " Source: " + ex.Source);
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
                            if (!string.IsNullOrEmpty(appSecret))
                            {
                                try
                                {
                                    ClientCredential clientCredential = new ClientCredential(appId, appSecret);
                                    Credential credential = new Credential(tenantId, appId, clientCredential, resourceId);

                                    if (CheckRoles(roleCheck, credential.JWT.Claims.ToList()))
                                    {
                                        _credentials.Add(tenantId,credential);
                                    }

                                    found = true;
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    if (Configuration.DebugShowSecrets)
                                    {
                                        Log.WriteLine("Exception using app secret: " + appSecret + " TenantID: "+tenantId+" AppID: "+appId+" Exception: " + ex.Message + " Inner Exception:" + ex.InnerException.Message + " Source: " + ex.Source);
                                    }
                                    else
                                    {
                                        Log.WriteLine("Exception using app secret: [DebugShowSecrets = false] TenantID: " + tenantId + " AppID: " + appId + " Exception: " + ex.Message + " Inner Exception:" + ex.InnerException.Message + " Source: " + ex.Source);
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
        public bool Expired { get { return CheckTokenExpired(); } } // We declare the token as expired Configuration.TokenExpires minutes before real expire time

        #endregion
    }
}

