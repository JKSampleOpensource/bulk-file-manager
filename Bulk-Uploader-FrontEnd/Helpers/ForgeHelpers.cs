using System.Net;
using System.Web;
using Bulk_Uploader_Electron.Models;
using Bulk_Uploader_Electron.Models.Forge.FolderContents;
using Bulk_Uploader_Electron.Models.Forge.Hub;
using Bulk_Uploader_Electron.Models.Forge.Projects;
using Bulk_Uploader_Electron.Models.Forge.Metadata;
using Bulk_Uploader_Electron.Models.Forge.TopFolders;
using Bulk_Uploader_Electron.Models.Forge.Versions;
using Bulk_Uploader_Electron.Models.Forge.WebhookResponse;
using Flurl.Http;
using Serilog;
using Newtonsoft.Json;
using RestSharp;
using Bulk_Uploader_Electron.Managers;
using Bulk_Uploader_Electron.Models.Forge;
using Bulk_Uploader_Electron.Models.Forge.Bim360Project;
using Bulk_Uploader_Electron.Models.Forge.TipResponse;
using Flurl;
using Bulk_Uploader_Electron.Models.Forge.GetDataHooksResponse;
using Bulk_Uploader_Electron.Models.Forge.MetadataProperties;
using mass_upload_via_s3_csharp.Models.Forge;
using mass_upload_via_s3_csharp.Models.Forge.ForgeCreateFolder;
using mass_upload_via_s3_csharp.Models.Forge.ForgeSignedS3Upload;
using mass_upload_via_s3_csharp.Models.Forge.ForgeStorageCreation;
//using Project = Bulk_Uploader_Electron.Models.Project;
using mass_upload_via_s3_csharp;
using System.Linq;
using Microsoft.Extensions.Configuration;
using FolderItemIncluded = Bulk_Uploader_Electron.Models.Forge.FolderContents.FolderItemIncluded;
using Ac.Net.Authentication.Models;
using static System.Net.WebRequestMethods;
using Bulk_Uploader_Electron.Models.Forge.Project;
using Org.BouncyCastle.Asn1.Ocsp;
using Bulk_Uploader_Electron.Helpers;
using APSProject = Autodesk.DataManagement.Model.Project;
using System;

namespace Bulk_Uploader_Electron.Utilities
{
    public static class ForgeHelpers
    {
        public static async Task<List<Account>> GetHubs(string? token = null)
        {
            try
            {
                token ??= await TokenManager.GetTwoLeggedToken();
                var apsHubs = await APSClientHelper.DataManagement.GetHubsAsync(accessToken: token);
                var accounts = new List<Account>();
                foreach (var hub in apsHubs.Data)
                {
                    accounts.Add(new Account()
                    {
                        AccountId = hub.Id,
                        Enabled = false,
                        Region = hub.Attributes.Region,
                        Name = hub.Attributes.Name
                    });
                }
                return accounts;
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message);
                Log.Error(exception.StackTrace ?? string.Empty);
                throw;
            }
        }

        public static async Task<List<Project>> GetHubProjects(string hubId, string token, string? userId = null, List<ErrorMessage>? errors = null)
        {
            var projects = new List<Project>();

            var failures = 0;
            int limit = 50; // Issue with the new SDK: does not work.
            int pageNumber = 0; // Issue with the new SDK: does not work.
            bool hasNextPage = true;

            while (hasNextPage)
            {
                try
                {
                    var apsProjects = await APSClientHelper.DataManagement.GetHubProjectsAsync(hubId, xUserId: userId, accessToken: token, pageNumber: pageNumber, pageLimit: limit);
                    foreach (var project in apsProjects.Data)
                    {
                        projects.Add(new Project()
                        {
                            AccountId = hubId,
                            ProjectId = project.Id,
                            Name = project.Attributes.Name,
                            ProjectType = project.Attributes.Extension.Data.ProjectType == "ACC" ? ProjectType.ACC : ProjectType.BIM360
                        });
                    }
                    hasNextPage = apsProjects.Links.Next != null;
                    pageNumber++;
                }
                catch (Exception exception)
                {
                    errors?.Add(new ErrorMessage("Projects", exception.Message, exception.StackTrace ?? ""));
                    Log.Error(exception.Message);
                    Log.Error(exception.StackTrace ?? string.Empty);
                    failures++;
                    if (failures > 5) throw;
                }
            }


            //accountId = accountId.Substring(0, 2) == "b." ? accountId : "b." + accountId;
            //var uri = AppSettings.GetUriPath(AppSettings.Instance.HubsEndpoint) + $"/{hubId}/projects?page[limit]=100";
            //while (uri != null)
            //{
            //    try
            //    {
            //        //  var token = await TokenManager.GetTwoLeggedToken();

            //        var projectsResponse = uri
            //            .WithOAuthBearerToken(token);

            //        if (!string.IsNullOrWhiteSpace(userId))
            //        {
            //            projectsResponse.WithHeader("x-user-id", userId);
            //        }

            //        var projectResponse = await projectsResponse.GetJsonAsync<ForgeProjects>();

            //        foreach (var project in projectResponse.data)
            //        {
            //            projects.Add(new Project()
            //            {
            //                AccountId = hubId,
            //                ProjectId = project.id,
            //                Name = project.attributes.name,
            //                ProjectType = project.attributes.extension.data.projectType == "ACC" ? ProjectType.ACC : ProjectType.BIM360
            //            });
            //        }

            //        uri = projectResponse.links.next?.href;
            //    }
            //    catch (Exception exception)
            //    {
            //        errors?.Add(new ErrorMessage("Projects", exception.Message, exception.StackTrace ?? ""));
            //        Log.Error(exception.Message);
            //        Log.Error(exception.StackTrace);
            //        failures++;
            //        if (failures > 5) throw;
            //    }
            //}

            return projects;
        }

        public static async Task<APSProject> GetHubProject(string token, string hubId, string projectId)
        {
            try
            {
                return await APSClientHelper.DataManagement.GetProjectAsync(hubId, projectId, accessToken: token);
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message);
                Log.Error(exception.StackTrace ?? string.Empty);
                throw;
            }
        }

        public static async Task<List<SimpleFolder>> GetTopFolders(string token, string hubId, string projectId)
        {
            try
            {
                var apsProjectTopFolders = await APSClientHelper.DataManagement.GetProjectTopFoldersAsync(hubId, projectId, accessToken: token, excludeDeleted: true);
                var folders = new List<SimpleFolder>();
                foreach (var folder in apsProjectTopFolders.Data)
                {
                    if (IsValidTopFolder(folder.Attributes.Name))
                    {
                        folders.Add(new SimpleFolder()
                        {
                            FolderId = folder.Id,
                            Name = folder.Attributes.Name,
                            Url = folder.Links.WebView.Href,
                            IsRoot = false
                        });
                    }
                }
                return folders;
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message);
                Log.Error(exception.StackTrace ?? string.Empty);
                throw;
            }
        }
        private static bool IsValidTopFolder(string name)
        {
            if (name.Contains("checklist_") || name.Contains("submittals-attachments") || name.Contains("Photos") ||
                name.Contains("ProjectTb") || name.Contains("dailylog_") || name.Contains("issue_") ||
                name.Contains("correspondence-project") || name.Contains("meetings-project") || name.Contains("issues_")
                || name.Contains("COST Root Folder") || name.Contains("Recycle Bin") || Guid.TryParse(name, out _)
                || name.Contains("quantification_") || name.Contains("rfis_project_") || name.Contains("assets_"))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static async Task<SimpleFolder> GetRootFolder(string token, string hubId, string projectId, string apsFolderUrn)
        {
            try
            {
                var apsProject = await APSClientHelper.DataManagement.GetProjectAsync(hubId, projectId, accessToken: token);
                var apsFolder = await APSClientHelper.DataManagement.GetFolderAsync(projectId, apsFolderUrn, accessToken: token);

                return new SimpleFolder
                {
                    Name = apsFolder.Data.Attributes.Name,
                    ParentPath = apsFolder.Data.Attributes.DisplayName,
                    FolderId = apsFolderUrn,
                    Path = $"{apsProject.Data.Attributes.Name}/{apsFolder.Data.Attributes.Name}"
                };
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message);
                Log.Error(exception.StackTrace ?? string.Empty);
                throw;
            }
        }

        public static async Task<(List<SimpleFolder>, List<SimpleFile>)> GetFolderContents(string token, string projectId, string folderId)
        {
            var (folders, files) = (new List<SimpleFolder>(), new List<SimpleFile>());

            var failures = 0;
            var limit = 1; // Issue with the new SDK: does not work.
            int pageNumber = 0; // Issue with the new SDK: does not work.
            bool hasNextPage = true;

            while (hasNextPage)
            {
                try
                {
                    var apsFolderContent = await APSClientHelper.DataManagement.GetFolderContentsAsync(projectId, folderId, accessToken: token, pageLimit: limit, pageNumber: pageNumber);
                    foreach (var item in apsFolderContent.Data)
                    {
                        if (item.Type == "folders")
                        {
                            folders.Add(new SimpleFolder()
                            {
                                FolderId = item.Id,
                                Name = item.Attributes.Name,
                                Url = item.Links.Self.Href,  // Issue with the new SDK: does not support webView link
                                IsRoot = false
                            });
                        }
                    }
                    if (apsFolderContent.Included != null)
                        foreach (var item in apsFolderContent.Included)
                        {
                            if (item.Type == "versions" && item.Attributes.FileType != null)
                            {
                                var newfile = new SimpleFile()
                                {
                                    VersionId = item.Id,
                                    Name = item.Attributes.Name,
                                    FileType = item.Attributes.FileType,
                                    ItemId = item.Relationships.Item.Data.Id,
                                    DerivativeId = item.Relationships.Derivatives.Data.Id,
                                    ObjectId = item.Relationships.Storage.Data.Id,
                                    Url = item.Links.WebView.Href,
                                    Size = Convert.ToInt64(item.Attributes.StorageSize)
                                };
                                if (DateTime.TryParse(item.Attributes.LastModifiedTime, out DateTime itemTime))
                                    newfile.LastModified = itemTime;
                                else
                                    newfile.LastModified = DateTime.MinValue;
                                files.Add(newfile);
                            }
                        }
                    hasNextPage = apsFolderContent.Links.Next != null;
                    pageNumber++;
                }
                catch (Exception exception)
                {
                    Log.Error(exception.Message);
                    Log.Error(exception.StackTrace ?? string.Empty);
                    failures++;
                    if (failures > 5) throw;
                }
            }

            return (folders, files);
        }

        public static async Task<string> GetDownloadUrl(string token, string bucketKey, string objectKey, int minutesExpiration = 3)
        {
            try
            {
                var apsS3DownloadUrl = await APSClientHelper.OssApi.SignedS3DownloadAsync(bucketKey, objectKey, accessToken: token, minutesExpiration: minutesExpiration, publicResourceFallback: true);

                if (apsS3DownloadUrl.Content.Status == "complete" || apsS3DownloadUrl.Content.Status == "fallback")
                    return apsS3DownloadUrl.Content.Url;
                else throw new Exception("Signed Response Failed");
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message);
                Log.Error(exception.StackTrace ?? "No Stack");
                throw;
            }

        }

        public static async Task<Stream> GetDownloadStream(string token, string storageUrn)
        {
            try
            {
                var bucketKey = HttpUtility.UrlEncode("wip.dm.prod");
                var objectName = HttpUtility.UrlEncode(storageUrn.Split('/').Last());
                var downloadUrl = await GetDownloadUrl(token, bucketKey, objectName);
                var response = await downloadUrl
                    .WithHeader("ConnectionClose", true)
                    .SendAsync(HttpMethod.Get);
                return await response.GetStreamAsync();
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message);
                Log.Error(exception.StackTrace ?? string.Empty);
                throw;
            }
        }

        /// <summary>
        /// Return the URLs to upload the file
        /// </summary>
        /// <param name="bucketKey">Bucket key</param>
        /// <param name="objectKey">Object key</param>
        /// <param name="parts">[parts=1] How many URLs to generate in case of multi-part upload</param>
        /// <param name="firstPart">B[firstPart=1] Index of the part the first returned URL should point to</param>
        /// <param name="uploadKey">[uploadKey] Optional upload key if this is a continuation of a previously initiated upload</param>
        /// <param name="minutesExpiration">[minutesExpiration] Custom expiration for the upload URLs (within the 1 to 60 minutes range). If not specified, default is 2 minutes.
        public static async Task<Autodesk.Oss.Model.Signeds3uploadResponse> GetUploadUrls(string bucketKey, string objectKey, int? minutesExpiration, int parts = 1, int firstPart = 1, string uploadKey = null)
        {
            var token = await TokenManager.GetTwoLeggedToken();

            var apsS3UploadUrl = await APSClientHelper.OssApi.SignedS3UploadAsync(bucketKey, objectKey, accessToken: token, minutesExpiration: minutesExpiration, parts: parts, firstPart: firstPart, uploadKey: uploadKey);
            if (apsS3UploadUrl.HttpResponse.IsSuccessStatusCode)
            {
                return apsS3UploadUrl.Content;
            }
            if (apsS3UploadUrl.HttpResponse.StatusCode == HttpStatusCode.TooManyRequests)
            {
                _ = int.TryParse(apsS3UploadUrl.HttpResponse.Headers.GetValues("Retry-After").FirstOrDefault(), out int retryAfter);
                await Task.Delay(retryAfter);
                return await GetUploadUrls(bucketKey, objectKey, minutesExpiration, parts, firstPart, uploadKey);
            }
            else
            {
                throw new Exception("Failed to get upload URLs");
            }
        }

        public static async Task<bool> CompleteUpload(string bucketKey, string objectKey, string uploadKey)
        {
            try
            {
                var token = await TokenManager.GetTwoLeggedToken();
                Autodesk.Oss.Model.Completes3uploadBody body = new() { UploadKey = uploadKey };
                var apsCompleteUpload = await APSClientHelper.OssApi.CompleteSignedS3UploadAsync(bucketKey, objectKey, "application/json", body, accessToken: token);
                return apsCompleteUpload.IsSuccessStatusCode;
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message);
                Log.Error(exception.StackTrace ?? string.Empty);
                throw;
            }
        }


        public static async Task<Autodesk.DataManagement.Model.Storage> CreateStorageLocation(string projectId, string fileName, string folderUrn)
        {
            try
            {
                var token = await TokenManager.GetTwoLeggedToken();
                Autodesk.DataManagement.Model.StoragePayload storagePayload = new()
                {
                    Jsonapi = new() { _Version = Autodesk.DataManagement.Model.VersionNumber._10 },
                    Data = new()
                    {
                        Type = Autodesk.DataManagement.Model.Type.Objects,
                        Attributes = new()
                        {
                            Name = fileName
                        },
                        Relationships = new()
                        {
                            Target = new()
                            {
                                Data = new()
                                {
                                    Type = Autodesk.DataManagement.Model.Type.Folders,
                                    Id = folderUrn
                                }
                            }
                        }
                    }
                };
                return await APSClientHelper.DataManagement.CreateStorageAsync(projectId, storagePayload: storagePayload, accessToken: token);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Log.Error(ex.Message);
                throw;
            }
        }

        public static async Task<Autodesk.DataManagement.Model.Folder> CreateFolder(string projectId, string parentFolderId, string folderName)
        {
            await Task.Delay(10000);  // What is the purpose of this 10sec delay?
            try
            {
                projectId = projectId.StartsWith("b.") ? projectId : $"b.{projectId}";  // Is it required?
                var token = await TokenManager.GetTwoLeggedToken();

                Autodesk.DataManagement.Model.FolderPayload folderPayload = new()
                {
                    Jsonapi = new() { _Version = Autodesk.DataManagement.Model.VersionNumber._10 },
                    Data = new()
                    {
                        Type = Autodesk.DataManagement.Model.Type.Folders,
                        Attributes = new()
                        {
                            Name = folderName,
                            Extension = new()
                            {
                                Type = Autodesk.DataManagement.Model.Type.FoldersautodeskBim360Folder, // "folders:autodesk.bim360:Folder",
                                _Version = Autodesk.DataManagement.Model.VersionNumber._10
                            }
                        },
                        Relationships = new()
                        {
                            Parent = new()
                            {
                                Data = new()
                                {
                                    Type = Autodesk.DataManagement.Model.Type.Folders,
                                    Id = parentFolderId
                                }
                            }
                        }
                    }
                };
                var apsCreateFolder = await APSClientHelper.DataManagement.CreateFolderAsync(projectId, folderPayload: folderPayload, accessToken: token);
                // Issue with the new SDK:  how to handle 429 status response for retry.
                return apsCreateFolder;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error Creating Folder: " + folderName);
                Log.Error("Error Creating Folder: " + folderName);
                Console.WriteLine(ex.Message);
                Log.Error(ex.Message);
                throw;
            }
        }

        public static async Task<Autodesk.DataManagement.Model.Item> CreateFirstVersion(string projectId, string fileName, string folderId, string objectId)
        {
            try
            {
                projectId = projectId.Split(".")[0] == "b" ? projectId : "b." + projectId;  // Is it required?
                var token = await TokenManager.GetTwoLeggedToken();

                Autodesk.DataManagement.Model.ItemPayload itemPayload = new()
                {
                    Jsonapi = new() { _Version = Autodesk.DataManagement.Model.VersionNumber._10 },
                    Data = new()
                    {
                        Type = Autodesk.DataManagement.Model.Type.Items,
                        Attributes = new()
                        {
                            DisplayName = fileName,
                            Extension = new()
                            {
                                Type = Autodesk.DataManagement.Model.Type.ItemsautodeskBim360File, // "items:autodesk.bim360:File",,
                                _Version = Autodesk.DataManagement.Model.VersionNumber._10
                            }
                        },
                        Relationships = new()
                        {
                            Tip = new()
                            {
                                Data = new()
                                {
                                    Type = Autodesk.DataManagement.Model.Type.Versions,
                                    Id = "1"
                                }
                            },
                            Parent = new()
                            {
                                Data = new()
                                {
                                    Type = Autodesk.DataManagement.Model.Type.Folders,
                                    Id = folderId
                                }
                            }
                        }
                    },
                    Included = new()
                    {
                        new() {
                            Type = Autodesk.DataManagement.Model.Type.Versions,
                            Id = "1",
                            Attributes = new()
                            {
                                Name = fileName,
                                Extension = new()
                                {
                                    Type = Autodesk.DataManagement.Model.Type.VersionsautodeskBim360File, // "versions:autodesk.bim360:File",
                                    _Version = Autodesk.DataManagement.Model.VersionNumber._10
                                }
                            },
                            Relationships = new()
                            {
                                Storage = new()
                                {
                                    Data = new()
                                    {
                                        Type = Autodesk.DataManagement.Model.Type.Objects,
                                        Id = $"urn:adsk.objects:os.object:wip.dm.prod/{objectId}"
                                    }
                                }
                            }
                        }
                    }
                };
                var apsCreateItems = await APSClientHelper.DataManagement.CreateItemAsync(projectId, itemPayload: itemPayload, accessToken: token);
                // Issue with the new SDK:  how to handle 429 status response for retry.
                return apsCreateItems;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to upload the first version of file " + fileName);
                Log.Error("Failed to upload the first version of file " + fileName);
                Console.WriteLine(ex.Message);
                Log.Error(ex.Message);
                throw;
            }
        }

        public static async Task<Autodesk.DataManagement.Model.ModelVersion> CreateNextVersion(string projectId, string fileName, string itemId, string objectId)
        {
            try
            {
                projectId = projectId.Split(".")[0] == "b" ? projectId : "b." + projectId;
                var token = await TokenManager.GetTwoLeggedToken();

                Autodesk.DataManagement.Model.VersionPayload versionPayload = new()
                {
                    Jsonapi = new() { _Version = Autodesk.DataManagement.Model.VersionNumber._10 },
                    Data = new()
                    {
                        Type = Autodesk.DataManagement.Model.Type.Versions,
                        Attributes = new()
                        {
                            DisplayName = fileName,
                            Extension = new()
                            {
                                Type = Autodesk.DataManagement.Model.Type.VersionsautodeskBim360File, // "versions:autodesk.bim360:File",,
                                _Version = Autodesk.DataManagement.Model.VersionNumber._10
                            }
                        },
                        Relationships = new()
                        {
                            Item = new()
                            {
                                Data = new()
                                {
                                    Type = Autodesk.DataManagement.Model.Type.Items,
                                    Id = itemId
                                }
                            },
                            Storage = new()
                            {
                                Data = new()
                                {
                                    Type = Autodesk.DataManagement.Model.Type.Objects,
                                    Id = $"urn:adsk.objects:os.object:wip.dm.prod/{objectId}"
                                }
                            }
                        }
                    }
                };
                var apsCreateVersions = await APSClientHelper.DataManagement.CreateVersionAsync(projectId, versionPayload: versionPayload, accessToken: token);
                // Issue with the new SDK:  how to handle 429 status response for retry.
                return apsCreateVersions;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not create next version of folder: " + fileName);
                Log.Error("Could not create next version of folder: " + fileName);
                Console.WriteLine(ex.Message);
                Log.Error(ex.Message);
                throw;
            }
        }




        #region Redundant
        //public static async Task<List<SimpleFolder>> GetSubFolders(string token, string projectId, string folderId, string userId = null, int? limit = null)
        //{
        //    var (folders, files) = (new List<SimpleFolder>(), new List<SimpleFile>());
        //    var uri = AppSettings.GetUriPath(AppSettings.Instance.ProjectsEndpoint) + $"/{projectId}/folders/{folderId}/contents";

        //    if (limit != null) uri += $"?page[limit]={limit}";

        //    // var failures = 0;

        //    while (uri != null)
        //    {
        //        try
        //        {
        //            var request = uri
        //                .WithOAuthBearerToken(token);

        //            if (userId != null)
        //            {
        //                request = request.WithHeader("x-user-id", userId);
        //            }

        //            var contentsResponse = await request
        //                .GetJsonAsync<ForgeFolderContents>();

        //            if (contentsResponse.data != null)
        //            {
        //                foreach (var item in contentsResponse.data)
        //                {
        //                    if (item.type == "folders")
        //                    {
        //                        folders.Add(new SimpleFolder()
        //                        {
        //                            FolderId = item.id,
        //                            Name = item.attributes.name,
        //                        });
        //                    }
        //                }
        //            }

        //            uri = contentsResponse.links.next?.href ?? null;
        //        }
        //        //catch (FlurlHttpException exception)
        //        //{
        //        //    if (exception.StatusCode == 403)
        //        //    {
        //        //        Log.Warning($"GetFolderContents returned 403");
        //        //        throw;
        //        //    }
        //        //    //else if (exception.StatusCode == 429)
        //        //    //{
        //        //    //    failures++;
        //        //    //    Log.Warning($"GetFolderContents returned 429: {failures}");
        //        //    //    if (failures > 5) throw;
        //        //    //    await Task.Delay(15000);

        //        //    //}
        //        //    //else
        //        //    //{
        //        //    //    Log.Error(exception.Message);
        //        //    //    Log.Error(exception.StackTrace);
        //        //    //    failures++;

        //        //    //    if (failures > 5) throw;
        //        //    //}
        //        //}
        //        catch (Exception exception)
        //        {
        //            Log.Error(exception.Message);
        //            Log.Error(exception.StackTrace);
        //            //failures++;

        //            //if (failures > 5) throw;
        //            throw;
        //        }
        //    }

        //    return folders;
        //}
        //public static async Task<ForgeItemTipResponse> GetItemVersion(string projectId, string versionId, string userId)
        //{

        //    var token = await TokenManager.GetTwoLeggedToken();
        //    var request =
        //        AppSettings.GetUriPath(AppSettings.Instance.ProjectsEndpoint) + $"/{projectId}/versions/{HttpUtility.UrlEncode(versionId)}"
        //            .WithOAuthBearerToken(token);

        //    if (userId != null)
        //    {
        //        request.WithHeader("x-user-id", userId);
        //    }

        //    var versionResponse = await request.GetJsonAsync<ForgeItemTipResponse>();

        //    return versionResponse;
        //}

        //public static async Task<ForgeMetadata> GetMetadata(string encodedUrn)
        //{
        //    var token = await TokenManager.GetTwoLeggedToken();

        //    var metadataResponse = await (AppSettings.GetUriPath(AppSettings.Instance.ModelformatEndpoint) + $"/{encodedUrn}/metadata")

        //        .WithOAuthBearerToken(token)
        //        .GetJsonAsync<ForgeMetadata>();

        //    return metadataResponse;
        //}

        //public static async Task<ForgeVersions> GetVersion(string projectId, string version)
        //{
        //    projectId = projectId.Substring(0, 2) == "b." ? projectId : "b." + projectId;
        //    var token = await TokenManager.GetTwoLeggedToken();

        //    string encodedUrn = HttpUtility.UrlEncode(version);

        //    var request =
        //        await (AppSettings.GetUriPath(AppSettings.Instance.ProjectsEndpoint) + $"/{projectId}/versions/{encodedUrn}")
        //            .WithOAuthBearerToken(token)
        //            .AllowHttpStatus(HttpStatusCode.TooManyRequests)
        //            .GetAsync();


        //    if (request.StatusCode == 429)
        //    {

        //        await Task.Delay(15000);
        //        return await GetVersion(projectId, version);
        //    }

        //    return await request.GetJsonAsync<ForgeVersions>();
        //}

        //public static async Task<ForgeMetadataProperties> GetMetaDataProperties(string encodedUrn, string guid)
        //{
        //    var token = await TokenManager.GetTwoLeggedToken();

        //    //Get the full data set if possible using forceget
        //    var propertiesResponse = await (AppSettings.GetUriPath(AppSettings.Instance.ModelformatEndpoint) + $"/{encodedUrn}/metadata/{guid}/properties")
        //        .SetQueryParam("forgeget", "true")
        //        .WithOAuthBearerToken(token)
        //        .AllowHttpStatus(HttpStatusCode.Conflict)
        //        .GetJsonAsync<ForgeMetadataProperties>();

        //    return propertiesResponse;
        //}
        //public static async Task<List<ForgeAttributesBatchGetResult>> GetCustomAttributes(string projectId, List<string> urns)
        //{
        //    projectId = projectId.StartsWith("b.") ? projectId.Substring(2) : projectId;
        //    var token = await TokenManager.GetTwoLeggedToken();

        //    var request =
        //        await (AppSettings.GetUriPath(AppSettings.Instance.BimProjectEndpoint) + $"/{projectId}/versions:batch-get")
        //            .WithOAuthBearerToken(token)
        //            .AllowHttpStatus(HttpStatusCode.TooManyRequests)
        //            .PostJsonAsync(new
        //            {
        //                urns
        //            });

        //    if (request.StatusCode == 429)
        //    {
        //        await Task.Delay(15000);
        //        return await GetCustomAttributes(projectId, urns);
        //    }

        //    var response = await request.GetJsonAsync<ForgeAttributesBatchGet>();

        //    return response.results;
        //}

        //public static async Task<List<Project>> GetProjectsWithBusinessUnits(string accountId, string region)
        //{
        //    var url = region == "US"
        //        ? AppSettings.GetUriPath(AppSettings.Instance.APACEndpoint) + $"/{accountId.Substring(2)}/projects"
        //        : AppSettings.GetUriPath(AppSettings.Instance.EMEAEndpoint) + $"/{accountId.Substring(2)}/projects";

        //    var offset = 0;
        //    var limit = 100;

        //    var projects = new List<Project>();

        //    while (true)
        //    {
        //        var token = await TokenManager.GetTwoLeggedToken();

        //        var projectResponse = await $"{url}?offset={offset}&limit={limit}"
        //            .WithOAuthBearerToken(token)
        //            .GetJsonAsync<List<ForgeBim360Project>>();

        //        projectResponse.ForEach(project =>
        //        {
        //            projects.Add(new Project()
        //            {
        //                AccountId = accountId,
        //                BusinessUnitId = project.business_unit_id,
        //                Name = project.name,
        //                ProjectId = $"b.{project.id}"
        //            });
        //        });

        //        if (projectResponse.Count == 0)
        //        {
        //            break;
        //        }
        //        else
        //        {
        //            offset += limit;
        //        }
        //    }

        //    return projects;
        //}
        //public static async Task<List<ForgeBusinessUnit>> GetBusinessUnits(string accountId, string region, string twoLeggedToken)
        //{
        //    var url = region.ToUpper() == "US"
        //        ? AppSettings.GetUriPath(AppSettings.Instance.APACEndpoint) + $"/{accountId}/business_units_structure"
        //        : AppSettings.GetUriPath(AppSettings.Instance.EMEAEndpoint) + $"/{accountId}/business_units_structure";

        //    var businessUnitResponse = await url
        //        .WithOAuthBearerToken(twoLeggedToken)
        //        .GetJsonAsync<ForgeBusinessUnitResponse>();

        //    if (businessUnitResponse.business_units == null)
        //    {
        //        return new List<ForgeBusinessUnit>();
        //    }
        //    else
        //    {
        //        return businessUnitResponse.business_units;
        //    }
        //}
        //public static async Task<List<string>> GetProjectManagers(string projectId, string twoLeggedToken)
        //{
        //    projectId = projectId.StartsWith("b.") ? projectId.Remove(0, 2) : projectId;

        //    var url = AppSettings.GetUriPath(AppSettings.Instance.BimProjectEndpoint) + $"/{projectId}/users";

        //    var response = await url
        //        .SetQueryParam("filter[accessLevels]", "projectAdmin")
        //        .WithOAuthBearerToken(twoLeggedToken)
        //        .GetAsync();

        //    var projectManagers = await response.GetJsonAsync<ForgeProjectUsers>();
        //    return projectManagers.results.Select(x => x.email).ToList();
        //}

        //public static async Task<ForgeSignedS3Upload> GetUploadUrls(string bucketKey, string objectKey, int partIndex, int partCount, string uploadKey = null)
        //{
        //    try
        //    {
        //        var token = await TokenManager.GetTwoLeggedToken();

        //        var request = await (AppSettings.GetUriPath(AppSettings.Instance.BucketsEndpoint) + $"/{bucketKey}/objects/{objectKey}/signeds3upload")
        //            .SetQueryParam("minutesExpiration", "60")
        //            .SetQueryParam("firstPart", partIndex + 1) //1-indexed
        //            .SetQueryParam("parts", partCount)
        //            .WithOAuthBearerToken(token)
        //            .GetJsonAsync<ForgeSignedS3Upload>();

        //        return request;
        //    }
        //    catch (FlurlHttpException exception)
        //    {
        //        var response = await exception.GetResponseJsonAsync();
        //        Log.Error(exception.Message);
        //        throw;
        //    }
        //    catch (Exception exception)
        //    {
        //        Log.Error(exception.Message);
        //        throw;
        //    }
        //}
        //public static async Task<ForgeBatchS3Download> GetBatchS3(string bucketKey, List<string> objectIds)
        //{
        //    try
        //    {
        //        var uri =
        //            AppSettings.GetUriPath(AppSettings.Instance.BucketsEndpoint) + $"/{bucketKey}/objects/batchsigneds3download?minutesExpiration=60";

        //        var token = await TokenManager.GetTwoLeggedToken();

        //        var filteredObjectIds = objectIds.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

        //        if (filteredObjectIds.Count == 0)
        //        {
        //            return new ForgeBatchS3Download();
        //        }

        //        var request = await uri
        //            .WithOAuthBearerToken(token)
        //            .PostJsonAsync(new
        //            {
        //                requests = filteredObjectIds
        //                    .Select(x => new { objectKey = x.Split("/").Last() })
        //                    .ToList()
        //            });

        //        return await request.GetJsonAsync<ForgeBatchS3Download>();
        //    }
        //    catch (Exception e)
        //    {
        //        Log.Debug(e.Message);
        //        throw;
        //    }
        //}

        //public static async Task<ForgeSupportedFormats> GetSupportedFormats()
        //{
        //    var token = await TokenManager.GetTwoLeggedToken();

        //    //Get the full data set if possible using forceget
        //    var response = await (AppSettings.GetUriPath(AppSettings.Instance.ModelformatEndpoint) + $"/formats")
        //        .WithOAuthBearerToken(token)
        //        .AllowHttpStatus(HttpStatusCode.Conflict)
        //        .GetJsonAsync<ForgeSupportedFormats>();

        //    return response;
        //}
        //public static async Task<List<(string, bool)>> CheckItemPermissions(string projectId, string userId, List<string> permissions, List<string> itemIds)
        //{
        //    var batchSize = 50;
        //    var offset = 0;
        //    var resolvedPermissions = new List<(string, bool)>();
        //    while (offset < itemIds.Count)
        //    {
        //        try
        //        {
        //            var body = new
        //            {
        //                jsonapi = new { version = "1.0" },
        //                data = new
        //                {
        //                    type = "commands",
        //                    attributes = new
        //                    {
        //                        extension = new
        //                        {
        //                            type = "commands:autodesk.core:CheckPermission",
        //                            version = "1.0.0",
        //                            data = new
        //                            {
        //                                requiredActions = permissions.ToList()
        //                            }
        //                        }
        //                    },
        //                    relationships = new
        //                    {
        //                        resources = new
        //                        {
        //                            data = itemIds
        //                                .Skip(offset)
        //                                .Take(batchSize)
        //                                .Select(x => new
        //                                {
        //                                    type = "versions",
        //                                    id = x
        //                                })
        //                                .ToList()
        //                        }
        //                    }
        //                }
        //            };

        //            var token = await TokenManager.GetTwoLeggedToken();
        //            var request =
        //                await (AppSettings.GetUriPath(AppSettings.Instance.ProjectsEndpoint) + $"/{projectId}/commands")
        //                    .WithOAuthBearerToken(token)
        //                    .WithHeader("x-user-id", userId)
        //                    .PostJsonAsync(body);

        //            var response = await request
        //                .GetJsonAsync<ForgeCheckPermissionResponse>();

        //            var result = response.data.attributes.extension.data.permissions.Select(x => (x.id, x.permission))
        //                .ToList();

        //            resolvedPermissions.AddRange(result);
        //        }
        //        catch (FlurlHttpException e)
        //        {
        //            if (e.StatusCode == 403)
        //            {
        //                resolvedPermissions.AddRange(itemIds.Select(x => (x, false)).ToList());
        //            }
        //            else
        //            {
        //                throw;
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            Log.Error(e.Message);
        //            throw;
        //        }
        //        finally
        //        {
        //            offset += batchSize;
        //        }
        //    }
        //    return resolvedPermissions;
        //}
        //public static async Task<bool> CheckItemPermissions(string projectId, string userId, List<string> permissions, string itemId)
        //{
        //    var itemPermissions = await CheckItemPermissions(projectId, userId, permissions, new List<string>() { itemId });
        //    Nullable<(string, bool)> result = itemPermissions.FirstOrDefault();
        //    return result.HasValue && result.Value.Item2;
        //}

        //public static async Task<List<SimpleHook>> CreateDataWebhooks(string folderId, string region, string twoLeggedToken, string callbackUrl)
        //{
        //    var hookResponse = await AppSettings.GetUriPath(AppSettings.Instance.WebhooksDataEndpoint)
        //        .WithOAuthBearerToken(twoLeggedToken)
        //        //.WithHeader("Authorization", "Bearer " + twoLeggedToken)
        //        //.WithHeader("Content-Type", "application/json")
        //        .WithHeader("x-ads-region", region)
        //        .AllowHttpStatus(HttpStatusCode.Conflict)
        //        .PostJsonAsync(new
        //        {
        //            callbackUrl = callbackUrl,
        //            scope = new
        //            {
        //                folder = folderId
        //            }
        //        });

        //    var hookIds = new List<SimpleHook>();

        //    if (hookResponse.StatusCode == 409)
        //    {
        //        hookIds = await GetWebhooks("data", twoLeggedToken);
        //        return hookIds;
        //    }

        //    var hookResults = await hookResponse.GetJsonAsync<ForgeWebhookResponse>();

        //    foreach (var hook in hookResults.hooks)
        //    {
        //        hookIds.Add(new SimpleHook()
        //        {
        //            System = "data",
        //            HookId = hook.hookId,
        //            Event = hook._event
        //        });
        //    }

        //    return hookIds;
        //}
        //public static async Task DeleteWebhooks(string system, string webhookEvent, string hookId, string twoLeggedToken)
        //{
        //    var hookResponse = await (AppSettings.GetUriPath(AppSettings.Instance.WebhooksEndpoint) + $"/{system}/events/{webhookEvent}/hooks/{hookId}")
        //        .WithOAuthBearerToken(twoLeggedToken)
        //        .DeleteAsync();

        //    return;
        //}
        //public static async Task<List<SimpleHook>> GetWebhooks(string system, string twoLeggedToken, string next = "/hooks")
        //{
        //    ForgeGetDataHooksResponse pageResults = null;
        //    List<SimpleHook> hooks = new List<SimpleHook>();
        //    while (pageResults == null || pageResults.links.next != null)
        //    {
        //        pageResults = await (AppSettings.GetUriPath(AppSettings.Instance.WebhooksEndpoint) + $"/{system}{next}")
        //            .WithOAuthBearerToken(twoLeggedToken)
        //            .GetJsonAsync<ForgeGetDataHooksResponse>();

        //        foreach (var hook in pageResults.data)
        //        {
        //            hooks.Add(new SimpleHook()
        //            {
        //                Event = hook._event,
        //                HookId = hook.hookId,
        //                System = hook.system,
        //                Callback = hook.callbackUrl,
        //                FolderId = hook.tenant
        //            }); ;
        //        }

        //        next = pageResults.links.next;
        //    }

        //    return hooks;
        //}

        #endregion
    }
}