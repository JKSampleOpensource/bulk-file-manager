using Ac.Net.Authentication;
using Bulk_Uploader_Electron.Managers;

namespace Bulk_Uploader_Electron.Helpers;

public static class JointTokenManager
{
    public static async Task<string> GetToken()
    {
        try
        {
            return await ThreeLeggedManager.Instance.GetToken();
        }
        catch (Exception e)
        {
            return await TokenManager.GetTwoLeggedToken();
        }
    }
}