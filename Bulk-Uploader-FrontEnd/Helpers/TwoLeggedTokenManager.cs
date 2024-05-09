using Autodesk.Authentication.Model;
using Bulk_Uploader_Electron.Helpers;

namespace Bulk_Uploader_Electron.Managers
{
    public static class TwoLeggedTokenManager
    {
        #region Properties
        private static string TwoLeggedToken { get; set; } = "";
        private static DateTime TwoLeggedTokenExpiration { get; set; }
        private static string? ClientId { get; set; }
        private static string? ClientSecret { get; set; }
        #endregion


        #region Methods
        public static async Task<string> GetTwoLeggedToken()
        {
            if (ClientId != AppSettings.Instance.ClientId || ClientSecret != AppSettings.Instance.ClientSecret)
            {
                ClientId = AppSettings.Instance.ClientId;
                ClientSecret = AppSettings.Instance.ClientSecret;
                return await RequestTwoLeggedToken();
            }

            if (!string.IsNullOrEmpty(TwoLeggedToken) && TwoLeggedTokenExpiration > (DateTime.UtcNow.AddMinutes(15)))
                return TwoLeggedToken;
            else
                return await RequestTwoLeggedToken();
        }
        private static async Task<string> RequestTwoLeggedToken()
        { 
            var twoLeggedToken = await APSClientHelper.AuthClient.GetTwoLeggedTokenAsync(AppSettings.Instance.ClientId, AppSettings.Instance.ClientSecret, ScopeStringToArray(AppSettings.Instance.ForgeTwoLegScope));

            TwoLeggedToken = twoLeggedToken.AccessToken;
            TwoLeggedTokenExpiration = DateTime.UtcNow.AddSeconds(twoLeggedToken.ExpiresIn ?? 60);

            return TwoLeggedToken;
        }
        public static List<Scopes> ScopeStringToArray(string scopeString)
        {
            var scopeStrings = scopeString.Split(' ').ToList();
            var scopes = new List<Scopes>();

            if (scopeStrings.Contains("data:read")) scopes.Add(Scopes.DataRead);
            if (scopeStrings.Contains("data:write")) scopes.Add(Scopes.DataWrite);
            if (scopeStrings.Contains("data:create")) scopes.Add(Scopes.DataCreate);
            if (scopeStrings.Contains("data:search")) scopes.Add(Scopes.DataSearch);

            if (scopeStrings.Contains("account:read")) scopes.Add(Scopes.AccountRead);
            if (scopeStrings.Contains("account:write")) scopes.Add(Scopes.AccountWrite);

            if (scopeStrings.Contains("user:profileRead")) scopes.Add(Scopes.UserProfileRead);

            if (scopeStrings.Contains("bucket:read")) scopes.Add(Scopes.BucketRead);
            if (scopeStrings.Contains("bucket:update")) scopes.Add(Scopes.BucketUpdate);
            if (scopeStrings.Contains("bucket:create")) scopes.Add(Scopes.BucketCreate);
            if (scopeStrings.Contains("bucket:delete")) scopes.Add(Scopes.BucketDelete);

            if (scopeStrings.Contains("code:all")) scopes.Add(Scopes.CodeAll);

            return scopes;
        }
        #endregion
    }
}
