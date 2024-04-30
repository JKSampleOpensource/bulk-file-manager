using System;
using System.Collections.Generic;

namespace Bulk_Uploader_Electron.Models.Forge
{
    public class ForgeBatchS3Download
    {
        public Dictionary<string, ForgeBatchS3DownloadItem> results { get; set; }
    }
    
    public class ForgeBatchS3DownloadItem
    {
        public string status { get; set; }
        public string reason { get; set; }
        public string url { get; set; }
        // public Nullable<Dictionary<string, string>> urls { get; set; }
    }
    
}