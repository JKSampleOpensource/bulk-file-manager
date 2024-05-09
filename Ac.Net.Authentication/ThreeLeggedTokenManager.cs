using Ac.Net.Authentication.Models;
using Flurl;
using Flurl.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ac.Net.Authentication
{
    /// <summary>
    /// Used to get tokens and authenticate using three legged authentication.
    ///
    /// This should be thread safe.
    /// </summary>
    public class ThreeLeggedTokenManager : ITokenManager, IDisposable
    {
        private static ThreeLeggedTokenManager? _instance;
        private static object _lockObj = new object();
        private static IAuthParamProvider? _paramProvider;
        private static TokenUpdate _tokenUpdate;

        private static string RedirectUrl = "http://localhost:8083/code";

        private readonly AuthParameters _parameters;
        private bool _fetching = false;
        private TokenData _token = null;
        private object lockObject = new object();

        public ThreeLeggedTokenManager(AuthParameters tokenParameters)
        {
            if (tokenParameters.IsImplicit) throw new ArgumentException("Must be configure for non-implicit authentication");
            tokenParameters.IsValid();
            _parameters = tokenParameters;
            if (!string.IsNullOrEmpty(_parameters.RefreshToken))
            {
                TokenData td = new TokenData("error", _parameters.RefreshToken, DateTime.Now.AddHours(-1));
                this.SetToken(td);
            }
        }

        public event TokenUpdate OnTokenUpdate;

        public static ThreeLeggedTokenManager Instance
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
                            ForgeCallback = RedirectUrl,
                            RefreshToken = _paramProvider.RefreshToken,
                        };

                        _instance = new ThreeLeggedTokenManager(pms);
                        if (_tokenUpdate != null)
                        {
                            _instance.OnTokenUpdate += _tokenUpdate;
                        }
                    }
                }
                return _instance;
            }
        }

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

        /// <inheritdoc/>
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
                    // Autenticate();
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
                    // Autenticate();
                    if (Token == null)
                    {
                        throw new UnauthorizedAccessException("Token could not be obtained");
                    }
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

        // public void StartListening()
        // {
        //     if (!HttpListener.IsSupported)
        //     {
        //         Console.WriteLine("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
        //         return;
        //     }
        //
        //     if (Listener == null)
        //     {
        //         Listener = new XListener(_parameters.ForgeCallback);
        //     }
        //     Listener.StartListen(OnTokenRequestCode, OnError);
        // }
        //
        // public void StopListening()
        // {
        //     lock (lockObject) _timeOut = 10000;
        // }

        public string AuthUrl()
        {
            return APSClientHelper.AuthClient.Authorize(_parameters.ClientId, Autodesk.Authentication.Model.ResponseType.Code, RedirectUrl, _parameters.Scope);
        }

        protected virtual async Task<TokenData> RefreshToken(string refreshToken)
        {
            var refreshTokenResponse = await APSClientHelper.AuthClient.GetRefreshTokenAsync(_parameters.ClientId, _parameters.Secret, refreshToken, _parameters.Scope);
            refreshTokenResponse.ExpiresIn -= 30;
            return new TokenData(refreshTokenResponse.AccessToken, refreshTokenResponse._RefreshToken, DateTime.Now.AddSeconds(Convert.ToDouble(refreshTokenResponse.ExpiresIn - 30)));
        }

        private void FireTokenUpdate(TokenData tokenData)
        {
            if (OnTokenUpdate != null)
            {
                OnTokenUpdate(this, tokenData);
            }
        }

        public async Task GetTokenFromRequestCode(string tokenRequestCode)
        {
            var thread = new Thread(async (data) =>
            {
                Debug.WriteLine("Get from code");
                try
                {
                    var authenticateTokenResponse = await APSClientHelper.AuthClient.GetThreeLeggedTokenAsync(_parameters.ClientId, _parameters.Secret, tokenRequestCode, RedirectUrl);
                    var tokenData = new TokenData(authenticateTokenResponse.AccessToken, authenticateTokenResponse.RefreshToken, DateTime.Now.AddSeconds(Convert.ToDouble(authenticateTokenResponse.ExpiresIn - 30)));
                    SetToken(tokenData);
                    Fetching = false;
                }
                catch (Exception ex)
                {
                    SetToken(null);
                    Fetching = false;
                }
            });
            thread.Start();
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