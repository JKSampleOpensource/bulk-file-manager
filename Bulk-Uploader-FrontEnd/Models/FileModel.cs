using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Bulk_Uploader_Electron.Models
{
    public class FileModel
    {
        [JsonPropertyName("fileName")]
        public string FileName { get; set; }
        [JsonPropertyName("formFile")]
        public IFormFile FormFile { get; set; }
    }
}
