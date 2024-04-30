using System.Collections.Generic;

namespace Bulk_Uploader_Electron.Models.Forge.MetadataProperties;

public class ForgeMetadataProperties
{
    public Data data { get; set; }
}

public class Data
{
    public string type { get; set; }
    public Property[] collection { get; set; }
}

public class Property
{
    public int objectid { get; set; }
    public string externalId { get; set; }
    public string name { get; set; }
    public Dictionary<string, Dictionary<string, string>> properties { get; set; }
}