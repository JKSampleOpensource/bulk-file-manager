namespace Bulk_Uploader_Electron.Models.Forge.Project
{
    public class Attributes
    {
        public string name { get; set; }
        public List<string> scopes { get; set; }
        public Extension extension { get; set; }
    }

    public class Checklists
    {
        public Data data { get; set; }
        public Meta meta { get; set; }
    }

    public class Cost
    {
        public Data data { get; set; }
        public Meta meta { get; set; }
    }

    public class Data
    {
        public string type { get; set; }
        public string id { get; set; }
        public Attributes attributes { get; set; }
        public Links links { get; set; }
        public Relationships relationships { get; set; }
        public string projectType { get; set; }
    }

    public class Extension
    {
        public string type { get; set; }
        public string version { get; set; }
        public Schema schema { get; set; }
        public Data data { get; set; }
    }

    public class Hub
    {
        public Data data { get; set; }
        public Links links { get; set; }
    }

    public class Issues
    {
        public Data data { get; set; }
        public Meta meta { get; set; }
    }

    public class Jsonapi
    {
        public string version { get; set; }
    }

    public class Link
    {
        public string href { get; set; }
    }

    public class Links
    {
        public Self self { get; set; }
        public WebView webView { get; set; }
        public Related related { get; set; }
    }

    public class Locations
    {
        public Data data { get; set; }
        public Meta meta { get; set; }
    }

    public class Markups
    {
        public Data data { get; set; }
        public Meta meta { get; set; }
    }

    public class Meta
    {
        public Link link { get; set; }
    }

    public class Related
    {
        public string href { get; set; }
    }

    public class Relationships
    {
        public Hub hub { get; set; }
        public RootFolder rootFolder { get; set; }
        public TopFolders topFolders { get; set; }
        public Issues issues { get; set; }
        public Submittals submittals { get; set; }
        public Rfis rfis { get; set; }
        public Markups markups { get; set; }
        public Checklists checklists { get; set; }
        public Cost cost { get; set; }
        public Locations locations { get; set; }
    }

    public class Rfis
    {
        public Data data { get; set; }
        public Meta meta { get; set; }
    }

    public class ForgeProject
    {
        public Jsonapi jsonapi { get; set; }
        public Links links { get; set; }
        public Data data { get; set; }
    }

    public class RootFolder
    {
        public Data data { get; set; }
        public Meta meta { get; set; }
    }

    public class Schema
    {
        public string href { get; set; }
    }

    public class Self
    {
        public string href { get; set; }
    }

    public class Submittals
    {
        public Data data { get; set; }
        public Meta meta { get; set; }
    }

    public class TopFolders
    {
        public Links links { get; set; }
    }

    public class WebView
    {
        public string href { get; set; }
    }
}
