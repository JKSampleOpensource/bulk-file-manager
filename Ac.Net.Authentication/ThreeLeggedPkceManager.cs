using Ac.Net.Authentication.Models;
using Flurl;
using Flurl.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Ac.Net.Authentication
{

    /// <summary>
    /// Used to get tokens and authenticate using three legged authentication.
    ///
    /// This should be thread safe.
    /// </summary>
    public class ThreeLeggedPkceMananger : ITokenManager, IDisposable
    {
        private static ThreeLeggedPkceMananger? _instance;
        private static object _lockObj = new object();
        private static IAuthParamProvider? _paramProvider;
        private static TokenUpdate _tokenUpdate;

        private static XListener Listener = null;

        private readonly PkceAuthParameters _parameters;

        private bool _fetching = false;

        private TokenData _token = null;

        private object lockObject = new object();

        public ThreeLeggedPkceMananger(PkceAuthParameters tokenParameters)
        {
            tokenParameters.IsValid();
            _parameters = tokenParameters;
            if (!string.IsNullOrEmpty(_parameters.RefreshToken))
            {
                TokenData td = new TokenData("error", _parameters.RefreshToken, DateTime.Now.AddHours(-1));
                this.SetToken(td);
            }
        }

        public event TokenUpdate OnTokenUpdate;

        public static ThreeLeggedPkceMananger Instance
        {
            get
            {
                if (_instance == null)
                {
                    if (_paramProvider == null) throw new NullReferenceException("Token manager has not been initialized");
                    lock (_lockObj)
                    {
                        var pms = new PkceAuthParameters()
                        {
                            ClientId = _paramProvider.ClientId,
                            Secret = _paramProvider.ClientSecret,
                            Scope = _paramProvider.ForgeThreeLegScope,
                            ForgeCallback = _paramProvider.AuthCallback,
                            RefreshToken = _paramProvider.RefreshToken,
                        };

                        _instance = new ThreeLeggedPkceMananger(pms);
                        if (_tokenUpdate != null)
                        {
                            _instance.OnTokenUpdate += _tokenUpdate;
                        }
                    }
                }
                return _instance;
            }
        }

        // when no provider is present this is the html that will be send to the default on unsuccessful login
        public string ErrorHtml { get; set; } = Html.errorHtml;

        // when no provider is present this is the html that will be send to the default browser on successful login
        public string SuccessHtlm { get; set; } = Html.successHtml;

        /// <summary>
        /// Creates an instance of a ThreeLegged Token manager
        /// </summary>
        /// <param name="tokenParameters">Parameters that define the authentication</param>
        private bool Fetching
        {
            get { return _fetching; }
            set
            {
                lock (lockObject)
                {
                    _fetching = value;
                }
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
                var refreshedToken = await RefreshToken(Token.refresh_token);
                if (refreshedToken != null)
                {
                    SetToken(refreshedToken);
                    SetToken(refreshedToken);
                }
                return refreshedToken;
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
            while (Fetching)
            {
                Thread.Sleep(500);
                if (DateTime.Now > limit)
                {
                    throw new TimeoutException("Token request timed out");
                }
            }
            if (IsAuthenticated())
            {
                return Token.access_token;
            }
            // if not then we need a new token
            // first check for refresh...
            if ((Token != null && !string.IsNullOrEmpty(Token.refresh_token)))
            {
                // lock
                Fetching = true;// get token
                try
                {
                    var refreshedToken = await RefreshToken(Token.refresh_token);
                    SetToken(refreshedToken);
                    if (Token == null) throw new UnauthorizedAccessException("Token could not be obtained");
                    return Token.access_token;
                }
                catch
                {
                    SetToken(null);
                    Autenticate();
                    if (Token == null) throw new UnauthorizedAccessException("Token could not be obtained");
                    return Token.access_token;
                }
                finally
                {
                    Fetching = false;
                }
            }
            else
            {
                Fetching = true;
                try
                {
                    SetToken(null);
                    // if at this point we will authenticate to get token
                    Autenticate();
                    if (Token == null) throw new UnauthorizedAccessException("Token could not be obtained");
                    return Token.access_token;
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    Fetching = false;
                }
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

        public void StartListening()
        {
            if (!HttpListener.IsSupported)
            {
                Console.WriteLine("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
                return;
            }

            if (Token != null) return;

            if (Listener == null)
            {
                Listener = new XListener(_parameters.ForgeCallback);
            }
            Listener.StartListen(OnTokenRequestCode, null);
        }

        protected void Autenticate()
        {
            try
            {
                _parameters.GenerateChallenge();
                var authUrl = "https://developer.api.autodesk.com/authentication/v2/authorize"
                    .SetQueryParam("response_type", _parameters.ResposeType)
                    .SetQueryParam("client_id", _parameters.ClientId)
                    .SetQueryParam("redirect_uri", _parameters.ForgeCallback)
                    .SetQueryParam("scope", _parameters.Scope)
                    .SetQueryParam("code_challenge_method", "S256")
                    .SetQueryParam("code_challenge", _parameters.CodeChallenge)
                    .SetQueryParam("scope", _parameters.Scope);

                StartListening();
                Process.Start(new ProcessStartInfo(authUrl.ToString()) { UseShellExecute = true });
                WaitOnAuthResponse();
            }
            catch
            {
                SetToken(null);
                Fetching = false;
            }
        }

        protected virtual async Task<TokenData> RefreshToken(string refreshToken)
        {
            //   Debug.WriteLine($"Token is {Token.refresh_token}, ci = {_parameters.ClientId} s= {_parameters.Secret}");
            var tokenRequestResponse = await "https://developer.api.autodesk.com/authentication/v2/token"
                .WithBasicAuth(_parameters.ClientId, _parameters.Secret)
                .PostUrlEncodedAsync(new
            {
                grant_type = "refresh_token",
                refresh_token = refreshToken,
                redirect_uri = _parameters.ForgeCallback,
                scope = _parameters.Scope,
            }).ReceiveJson<TokenResponse>();
            //   System.Diagnostics.Debug.WriteLine($"Token Refreshed {tokenRequestResponse.refresh_token}");
            return new TokenData(tokenRequestResponse.access_token, tokenRequestResponse.refresh_token, DateTime.Now.AddSeconds(tokenRequestResponse.expires_in - 30));

            throw new Exception("Token Refresh Invalid");
        }

        private void FireTokenUpdate(TokenData tokenData)
        {
            if (OnTokenUpdate != null)
            {
                OnTokenUpdate(this, tokenData);
            }
        }

        private async Task GetTokenFromRequestCode(string tokenRequestCode)
        {

            var thread = new Thread(async (data) =>
            {
                try
                {
                    

                    var tokenResponse = await "https://developer.api.autodesk.com/authentication/v2/token".PostUrlEncodedAsync(new
                    {
                        client_id = _parameters.ClientId,
                        code_verifier = _parameters.CodeVerifier,
                        grant_type = "authorization_code",
                        code = tokenRequestCode,
                        redirect_uri = _parameters.ForgeCallback
                    }).ReceiveJson<TokenResponse>();
                    var tokenData = new TokenData(tokenResponse.access_token, tokenResponse.refresh_token, DateTime.Now.AddSeconds(tokenResponse.expires_in - 30));
                    SetToken(tokenData);
                    Fetching = false;
                }
                catch(Exception ex) 
                {
                    var d = ex;
                    SetToken(null);
                    Fetching = false;
                }
            });
            thread.Start();
        }

        private async Task OnTokenRequestCode(string code)
        {
            await GetTokenFromRequestCode(code);
        }

        // thread safe way to set token
        private void SetToken(TokenData token)
        {
            Debug.WriteLine("Old = " + _token?.refresh_token ?? "NULL");
            lock (lockObject) _token = token;
            Debug.WriteLine("Refresh = " + _token?.refresh_token ?? "NULL");
            FireTokenUpdate(token);
        }
        private void WaitOnAuthResponse()
        {
            int count = 0;
            while (count < 120)
            {
                if (Token != null)
                    return;
                count++;
                Thread.Sleep(500);
            }
            throw new Exception("Timeout retrieving token");
        }
    }
}