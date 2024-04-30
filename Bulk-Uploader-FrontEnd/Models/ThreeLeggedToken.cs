namespace Bulk_Uploader_Electron.Models
{
    public class ThreeLeggedToken
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string TokenType { get; set; }
        public int ExpiresIn { get; set; }
    }
}