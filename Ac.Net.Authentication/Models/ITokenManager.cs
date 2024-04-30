using System;
using System.Threading.Tasks;

namespace Ac.Net.Authentication.Models
{
    /// <summary>
    /// Used to get token
    /// </summary>
    public interface ITokenProvider
    {
        /// <summary>
        /// Get token
        /// </summary>
        /// <returns></returns>
        Task<string> GetToken();
    }


    /// <summary>
    /// Deleglte for when a token is refreshed
    /// </summary>
    /// <param name="manager"></param>
    /// <param name="newToken"></param>
    public delegate void TokenUpdate(ITokenManager manager, TokenData newToken);

    /// <summary>
    ///
    /// </summary>
    public interface ITokenManager : ITokenProvider
    {
        event TokenUpdate OnTokenUpdate;

        Task<TokenData?> ForceRefresh();

        string GetClientId();

        DateTime GetExpiration();

        string GetRefreshToken();

        Task<string> GetToken();

        bool IsAuthenticated();

        void Logout();
    }
}