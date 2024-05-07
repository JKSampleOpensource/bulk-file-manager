using Ac.Net.Authentication.Models;
using System.Collections.Concurrent;

namespace Bulk_Uploader_Electron;

public class AppSettings : IAuthParamProvider
{
    public ConcurrentDictionary<string, string> GlobalSettings { get; } = new ConcurrentDictionary<string, string>();

    public readonly static AppSettings Instance = new();
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string AuthCallback { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public string ForgeTwoLegScope { get; set; } = "data:read data:write data:create bucket:read";
    public string ForgeThreeLegScope { get; set; } = "data:read data:write data:create bucket:read";
    public string FileWorkerCount { get; set; } = "10";
    public string FolderWorkerCount { get; set; } = "10";
    public string BasePath { get; set; } = "";
    public string HubsEndpoint { get; set; } = "";
    public string BucketsEndpoint { get; set; } = "";
    public string ProjectsEndpoint { get; set; } = "";
    //public string AccHubId { get; set; } = "10";
    //public string ProjectId { get; set; } = "";
    //public string ParentFolderUrn { get; set; } = "";
    //public string LocalParentPath { get; set; } = "";
    //public string APACEndpoint { get; set; } = "";
    //public string EMEAEndpoint { get; set; } = "";
    //public string WebhooksEndpoint { get; set; } = "";
    //public string WebhooksDataEndpoint { get; set; } = "";
    //public string BimProjectEndpoint { get; set; } = "";
    //public string ModelformatEndpoint { get; set; } = "";

    public string AccountID = "";

    public string[] IllegalFileTypes { get; set; } = new string[] { "ade", "adp","app","asp","bas","bat","cer","chm","class",
    "cmd","com","command","cpl","crt","csh","exe","fxp","hex","hlp","hqx","hta","htm","html","inf","ini","ins","isp","its","jar",
    "job","js","jse","ksh","lnk","lzh","mad","maf","mag","mam","maq","mar","mas","mau","mav","maw","mda","mde","mdt","mdw","mdz",
    "msc","msi","msp","mst","ocx","ops","pcd","pkg","pif","prf","prg","ps1","pst","reg","scf","scr","sct","sea","sh","shb","shs",
    "svg","tmp","url","vb","vbe","vbs","vsmacros","vss","vst","vsw","webloc","ws","wsc","wsf","wsh","zlo","zoo"};

    public string CustomerExcludedFileTypes { get; set; } = "";
    public string CustomerExcludedFolderNames { get; set; } = "";

    public bool ConfigIsBuilt = false;

    public static void BuildConfig()
    {
        // Create Configuration from Environment and AppSettings
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile($"appsettings.json", true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true)
            .AddJsonFile($"appsettings.Local.json", true)
            .AddEnvironmentVariables();

        var config = configuration.Build();

        Instance.BasePath = "https://developer.api.autodesk.com";
        Instance.HubsEndpoint = "/project/v1/hubs";
        Instance.ProjectsEndpoint = "/data/v1/projects";
        Instance.BucketsEndpoint = "/oss/v2/buckets";

        //Instance.APACEndpoint = "/hq/v1/accounts";
        //Instance.EMEAEndpoint = "/hq/v1/regions/eu/accounts";
        //Instance.WebhooksEndpoint = "/webhooks/v1/systems";
        //Instance.WebhooksDataEndpoint = "/webhooks/v1/systems/data/hooks";
        //Instance.BimProjectEndpoint = "/bim360/admin/v1/projects";
        //Instance.ModelformatEndpoint = "/modelderivative/v2/designdata";
    }
    public static string GetUriPath(string path)
    {
        return (Instance.BasePath + path).ToString();
    }

}
