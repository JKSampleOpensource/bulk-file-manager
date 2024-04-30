using Ac.Net.Authentication.Models;
using Flurl.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ac.Net.Authentication
{
    /// <summary>
    /// Used to get tokens and authenticate using three legged authentication.
    ///
    /// This should be thread safe.
    /// </summary>
    public class TwoLeggedManager : ITokenManager, IDisposable
    {
        private static TwoLeggedManager? _instance;
        private static object _lockObj = new object();
        private static IAuthParamProvider? _paramProvider;
        private static TokenUpdate _tokenUpdate;

        private readonly AuthParameters _parameters;
        private TokenData _token = null;
        private object lockObject = new object();

        public TwoLeggedManager(AuthParameters tokenParameters)
        {
            if (tokenParameters.IsImplicit) throw new ArgumentException("Must be configure for non-implicit authentication");
            tokenParameters.IsValid();
            _parameters = tokenParameters;
            _parameters.RefreshToken = "";
            
        }

        public event TokenUpdate OnTokenUpdate;

        public static TwoLeggedManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    if (_paramProvider == null) throw new NullReferenceException("Token manager has not been initialized");
                    lock (_lockObj)
                    {
                        var pms = new AuthParameters()
                        {
                            ClientId = _paramProvider.ClientId,
                            Secret = _paramProvider.ClientSecret,
                            Scope = _paramProvider.ForgeThreeLegScope,
                            IsImplicit = false,
                            ForgeCallback = _paramProvider.AuthCallback,
                            RefreshToken = _paramProvider.RefreshToken,
                        };

                        _instance = new TwoLeggedManager(pms);
                        if (_tokenUpdate != null)
                        {
                            _instance.OnTokenUpdate += _tokenUpdate;
                        }
                    }
                }
                return _instance;
            }
        }

        private TokenData Token
        { get { return _token; } }

        public static void InitializeInstance(IAuthParamProvider paramProvider, TokenUpdate tokenUpdated)
        {
            lock (_lockObj)
            {
                _paramProvider = paramProvider;
                _tokenUpdate = tokenUpdated;
            }
        }

        public String BuildAuthorizationHeader(String client_id, String client_secret)
        {
            string credentials = $"{client_id}:{client_secret}";
            string base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
            string authorizationHeader = $"Basic {base64Credentials}";

            return authorizationHeader;
        }

        public void Dispose()
        {
            if (OnTokenUpdate != null)
            {
                // clean up so garbage collector will work.
                foreach (var del in (_instance.OnTokenUpdate?.GetInvocationList()?.ToArray() ?? new List<Delegate>().ToArray()))
                {
                    _instance.OnTokenUpdate -= (TokenUpdate)del;
                }
            }
        }

        public async Task<TokenData?> ForceRefresh()
        {
            try
            {
                //   Debug.WriteLine($"Token is {Token.refresh_token}");
                SetToken(null);
                await Autenticate();
                if (Token == null)
                {
                    throw new UnauthorizedAccessException("Token could not be obtained");
                }
                return Token;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public string GetClientId()
        {
            return this._parameters.ClientId;
        }

        /// <summary>
        /// Returns the expiration date of token
        /// </summary>
        /// <returns></returns>
        public DateTime GetExpiration()
        {
            return this.Token == null ? DateTime.MinValue : this.Token.expiresAt;
        }

        public string GetRefreshToken()
        {
            return (_token == null ? "" : _token.refresh_token);
        }

        /// <summary>
        /// Requests a token.  If token is not valid it will try to refresh token if refresh fails it will try to authenticate
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<string> GetToken()
        {
            DateTime limit = DateTime.Now.AddSeconds(15);
            // wait if fetching

            if (IsAuthenticated())
            {
                return Token.access_token;
            }
            else
            {
                SetToken(null);
                // if at this point we will authenticate to get token
                await Autenticate();
                if (Token == null)
                {
                    throw new UnauthorizedAccessException("Token could not be obtained");
                }
                return Token.access_token;
            }
        }

        public bool IsAuthenticated()
        {
            return (Token != null && Token.expiresAt > DateTime.Now && !string.IsNullOrEmpty(Token.access_token));
        }

        public void Logout()
        {
            SetToken(null);
        }

        private async Task Autenticate()
        {
            try
            {
                var tokenResponse = await "https://developer.api.autodesk.com/authentication/v2/token"
                    .WithBasicAuth(_parameters.ClientId, _parameters.Secret)
                    .PostUrlEncodedAsync(new
                    {
                        grant_type = "client_credentials",
                        scope = _parameters.Scope,
                        redirect_uri = _parameters.ForgeCallback
                    }).ReceiveJson<TokenResponse>();
                var tokenData = new TokenData(tokenResponse.access_token, tokenResponse.refresh_token, DateTime.Now.AddSeconds(tokenResponse.expires_in - 30));
                SetToken(tokenData);
            }
            catch (Exception ex)
            {
                SetToken(null);
            }
        }

        private void FireTokenUpdate(TokenData tokenData)
        {
            if (OnTokenUpdate != null)
            {
                OnTokenUpdate(this, tokenData);
            }
        }

        // thread safe way to set token
        private void SetToken(TokenData token)
        {
            Debug.WriteLine("Old = " + _token?.refresh_token ?? "NULL");
            lock (lockObject) _token = token;
            Debug.WriteLine("Refresh = " + _token?.refresh_token ?? "NULL");
            FireTokenUpdate(token);
        }
    }
}