using Newtonsoft.Json;

namespace Bulk_Uploader_Electron.Models.Forge
{
    public partial class BatchAttributesResponse
    {
        [JsonProperty("results")]
        public List<VersionAttributes> Results { get; set; } = new List<VersionAttributes>();

        [JsonProperty("errors")]
        public List<VersionAttributesError> Errors { get; set; } = new List<VersionAttributesError>();
    }

    public partial class BRApprovalStatus
    {
        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }

    public partial class VersionAttributes
    {
        [JsonProperty("urn")]
        public string Urn { get; set; }

        [JsonProperty("itemUrn")]
        public string ItemUrn { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("createTime")]
        public DateTime CreateTime { get; set; }

        [JsonProperty("createUserId")]
        public string CreateUserId { get; set; }

        [JsonProperty("createUserName")]
        public string CreateUserName { get; set; }

        [JsonProperty("lastModifiedTime")]
        public DateTime LastModifiedTime { get; set; }

        [JsonProperty("lastModifiedUserId")]
        public string LastModifiedUserId { get; set; }

        [JsonProperty("lastModifiedUserName")]
        public string LastModifiedUserName { get; set; }

        [JsonProperty("entityType")]
        public string EntityType { get; set; }

        [JsonProperty("revisionNumber")]
        public long RevisionNumber { get; set; }

        [JsonProperty("processState")]
        public string ProcessState { get; set; }

        [JsonProperty("extractionState")]
        public string ExtractionState { get; set; }

        [JsonProperty("reviewState")]
        public string ReviewState { get; set; }

        [JsonProperty("customAttributes")]
        public BRAttribute[] CustomAttributes { get; set; }

        [JsonProperty("approvalStatus")]
        public BRApprovalStatus ApprovalStatus { get; set; }

        [JsonProperty("storageUrn")]
        public string StorageUrn { get; set; }

        [JsonProperty("parentUrn")]
        public string ParentUrn { get; set; }

        [JsonProperty("parentPath")]
        public string ParentPath { get; set; }

        [JsonProperty("storageSize")]
        public long StorageSize { get; set; }
    }

    public partial class VersionAttributesError
    {
        [JsonProperty("urn")]
        public string Urn { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("detail")]
        public string Detail { get; set; }

        public override string ToString()
        {
            return $"{(Urn ?? "")}, {(Code ?? "")}, {(Title ?? "")}, {(Detail ?? "")}";
        }
    }

    public partial class BRAttribute
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
