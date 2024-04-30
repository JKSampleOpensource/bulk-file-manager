using IdentityModel;
using Newtonsoft.Json;
using Serilog;
using System;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Ac.Net.Authentication.Models
{
    public class AuthParameters : INotifyPropertyChanged
    {
        private object _lock = new object();

        private string refreshToken;

        /// <summary>
        /// Basic constuctor for implicit
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="forgeCallback"></param>
        /// <param name="scope"></param>
        public AuthParameters(string clientId, string forgeCallback, string scope)
        {
            ClientId = clientId;
            ForgeCallback = forgeCallback;
            Scope = scope;
        }

        /// <summary>
        /// Default Constuctor
        /// </summary>
        public AuthParameters()
        {
            ClientId = "";
            ForgeCallback = "";
            Scope = "";
            ClientId = "";

        }

        public event PropertyChangedEventHandler PropertyChanged;
        public string ClientId { get; set; }

        public string ForgeCallback { get; set; }

        public bool IsImplicit { get; set; } = false;

        public string RefreshToken { get => refreshToken; set { lock (_lock) refreshToken = value; } }

        public string ResposeType
        {
            get { return IsImplicit ? "token" : "code"; }
        }

        public string Scope { get; set; }

        public string Secret { get; set; }

        public string State { get; set; } = "";

        /// <summary>
        /// Gets a default path location
        /// </summary>
        /// <returns></returns>
        public static string GetPath()
        {
            FileInfo fileInfo = new FileInfo(typeof(AuthParameters).Assembly.Location);
            var path = fileInfo.DirectoryName.Replace(fileInfo.Directory.Root.FullName, "").Replace("\\", "");
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AC", path, typeof(AuthParameters).Name, "AuthParameters.json");
        }

        /// <summary>
        /// Copies values
        /// </summary>
        /// <param name="source"></param>
        public void CopyValues(AuthParameters source)
        {
            ClientId = source.ClientId;
            ForgeCallback = source.ForgeCallback;
            Scope = source.Scope;
            Secret = source.Secret;
            State = source.State;
            RefreshToken = source.RefreshToken;
            IsImplicit = source.IsImplicit;
        }

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(ClientId)) throw new NullReferenceException("Client ID is Invalid");
            if (string.IsNullOrWhiteSpace(Scope)) throw new NullReferenceException("Scope is Invalid");
            if (string.IsNullOrWhiteSpace(ForgeCallback)) throw new NullReferenceException("Authentication Callback is Invalid");
            if (IsImplicit && string.IsNullOrWhiteSpace(Secret)) throw new NullReferenceException("Secret is Invalid");
            return true;
        }

        /// <summary>
        /// Reads parameters for authentication to a json file
        /// </summary>
        /// <param name="path">Path to json file</param>
        /// <returns></returns>
        public AuthParameters Read(string path)
        {
            if (!File.Exists(path))
            {
                Log.Error($"File: {path} does not exist");
                return null;
            }
            using (var f = File.OpenText(path))
            {
                try
                {
                    var ap = JsonConvert.DeserializeObject<AuthParameters>(f.ReadToEnd());
                    return ap;
                }
                catch (Exception ex)
                {
                    Log.Error("Error Reading Auth Parameters");
                    Log.Error(ex.Message);
                    return null;
                }
            }
        }

        /// <summary>
        /// Reads parameters for authentication from a json file that has been encrypted
        /// </summary>
        /// <param name="path">Path to encrypted json file</param>
        /// <returns></returns>
        public AuthParameters ReadEncrypted(string path)
        {
            if (!File.Exists(path))
            {
                Log.Error($"File: {path} does not exist");
                return null;
            }
            using (var f = File.OpenText(path))
            {
                try
                {
                    var json = f.ReadToEnd();
                    json = EncryptionHelper.Decrypt(json);
                    var ap = JsonConvert.DeserializeObject<AuthParameters>(json);
                    return ap;
                }
                catch (Exception ex)
                {
                    Log.Error("Error Encrypted Reading Auth Parameters");
                    Log.Error(ex.Message);
                    return null;
                }
            }
        }

        /// <summary>
        /// Saves parameters for authentication from a json file
        /// </summary>
        /// <param name="path">Path to json file</param>
        /// <returns></returns>
        public void Save(string path)
        {
            var json = JsonConvert.SerializeObject(this);
            using (var file = File.CreateText(path))
            {
                file.Write(json);
                file.Flush();
            }
        }

        /// <summary>
        /// Saves parameters for authentication to a json file that has been encrypted
        /// </summary>
        /// <param name="path">Path to encrypted json file</param>
        /// <returns></returns>
        public void SaveEncrypted(string path)
        {
            var json = JsonConvert.SerializeObject(this);
            var fileInfo = new FileInfo(path);
            if (!fileInfo.Directory.Exists)
            {
                fileInfo.Directory.Create();
            }
            using (var file = File.CreateText(path))
            {
                file.Write(EncryptionHelper.Encrypt(json));
                file.Flush();
            }
        }
    }

    public class ForgeAuthConstants
    {
        public const string AUTHORIZATION_CODE = "authorization_code";
        public const string CLIENT_CREDENTIALS = "client_credentials";

        public const string CODE = "code";
        public const string REFRESH_TOKEN = "refresh_token";
    }
    public class PkceAuthParameters : INotifyPropertyChanged
    {
        private object _lock = new object();

        private string refreshToken;

        /// <summary>
        /// Basic constuctor for implicit
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="forgeCallback"></param>
        /// <param name="scope"></param>
        public PkceAuthParameters(string clientId, string forgeCallback, string scope)
        {
            ClientId = clientId;
            ForgeCallback = forgeCallback;
            Scope = scope;
            CodeChallenge = "";
            CodeVerifier = "";
        }

        /// <summary>
        /// Default Constuctor
        /// </summary>
        public PkceAuthParameters()
        {
            ClientId = "";
            ForgeCallback = "";
            Scope = "";
            ClientId = "";
            CodeChallenge = "";
            CodeVerifier = "";

        }

        public event PropertyChangedEventHandler PropertyChanged;
        public string ClientId { get; set; }

        public string CodeChallenge { get; set; }

        public string CodeVerifier { get; set; }

        public string ForgeCallback { get; set; }

        public string RefreshToken { get => refreshToken; set { lock (_lock) refreshToken = value; } }

        public string ResposeType
        {
            get { return "code"; }
        }

        public string Scope { get; set; }

        public string State { get; set; } = "";

        public string Secret { get; set; }

        /// <summary>
        /// Gets a default path location
        /// </summary>
        /// <returns></returns>
        public static string GetPath()
        {
            FileInfo fileInfo = new FileInfo(typeof(AuthParameters).Assembly.Location);
            var path = fileInfo.DirectoryName.Replace(fileInfo.Directory.Root.FullName, "").Replace("\\", "");
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AC", path, typeof(AuthParameters).Name, "AuthParameters.json");
        }

        /// <summary>
        /// Copies values
        /// </summary>
        /// <param name="source"></param>
        public void CopyValues(PkceAuthParameters source)
        {
            ClientId = source.ClientId;
            ForgeCallback = source.ForgeCallback;
            Scope = source.Scope;
            CodeChallenge = source.CodeChallenge;
            CodeVerifier = source.CodeVerifier;
            State = source.State;
            RefreshToken = source.RefreshToken;
        }

        public void GenerateChallenge()
        {
            var codeVerifier = CryptoRandom.CreateUniqueId(32);

            // store codeVerifier for later use
            //  context.Properties.Items.Add("code_verifier", codeVerifier);

            // create code_challenge
            string codeChallenge;
            using (var sha256 = SHA256.Create())
            {
                var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
                codeChallenge = Base64Url.Encode(challengeBytes);
            }

            CodeChallenge = codeChallenge;
            CodeVerifier = codeVerifier;
        }
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(ClientId)) throw new NullReferenceException("Client ID is Invalid");
            if (string.IsNullOrWhiteSpace(Scope)) throw new NullReferenceException("Scope is Invalid");
            if (string.IsNullOrWhiteSpace(ForgeCallback)) throw new NullReferenceException("Authentication Callback is Invalid");

            return true;
        }

        /// <summary>
        /// Reads parameters for authentication to a json file
        /// </summary>
        /// <param name="path">Path to json file</param>
        /// <returns></returns>
        public PkceAuthParameters Read(string path)
        {
            if (!File.Exists(path))
            {
                Log.Error($"File: {path} does not exist");
                return null;
            }
            using (var f = File.OpenText(path))
            {
                try
                {
                    var ap = JsonConvert.DeserializeObject<PkceAuthParameters>(f.ReadToEnd());
                    return ap;
                }
                catch (Exception ex)
                {
                    Log.Error("Error Reading Auth Parameters");
                    Log.Error(ex.Message);
                    return null;
                }
            }
        }

        /// <summary>
        /// Reads parameters for authentication from a json file that has been encrypted
        /// </summary>
        /// <param name="path">Path to encrypted json file</param>
        /// <returns></returns>
        public PkceAuthParameters ReadEncrypted(string path)
        {
            if (!File.Exists(path))
            {
                Log.Error($"File: {path} does not exist");
                return null;
            }
            using (var f = File.OpenText(path))
            {
                try
                {
                    var json = f.ReadToEnd();
                    json = EncryptionHelper.Decrypt(json);
                    var ap = JsonConvert.DeserializeObject<PkceAuthParameters>(json);
                    return ap;
                }
                catch (Exception ex)
                {
                    Log.Error("Error Encrypted Reading Auth Parameters");
                    Log.Error(ex.Message);
                    return null;
                }
            }
        }

        /// <summary>
        /// Saves parameters for authentication from a json file
        /// </summary>
        /// <param name="path">Path to json file</param>
        /// <returns></returns>
        public void Save(string path)
        {
            var json = JsonConvert.SerializeObject(this);
            using (var file = File.CreateText(path))
            {
                file.Write(json);
                file.Flush();
            }
        }

        /// <summary>
        /// Saves parameters for authentication to a json file that has been encrypted
        /// </summary>
        /// <param name="path">Path to encrypted json file</param>
        /// <returns></returns>
        public void SaveEncrypted(string path)
        {
            var json = JsonConvert.SerializeObject(this);
            var fileInfo = new FileInfo(path);
            if (!fileInfo.Directory.Exists)
            {
                fileInfo.Directory.Create();
            }
            using (var file = File.CreateText(path))
            {
                file.Write(EncryptionHelper.Encrypt(json));
                file.Flush();
            }
        }
    }
}

//    public class Auth3LeggedParameters : AuthParameters
//    {
//        public Auth3LeggedParameters()
//        {
//        }

//        public Auth3LeggedParameters(string clientId, string forgeCallback, string scope, string secret, string refeshToken) : base(clientId, forgeCallback, scope)
//        {
//            Secret = secret;
//            RefreshToken = refeshToken;
//        }

//        public string Secret { get; set; }
//        public string RefreshToken { get; set; }
//        public readonly string ResponseType = "code";
//    }

//    public class Auth3LeggedImplicitParameters : AuthParameters
//    {
//        public Auth3LeggedImplicitParameters(string clientId, string forgeCallback, string scope) : base(clientId, forgeCallback, scope)
//        {
//        }

//        public readonly string ResponseType = "token";
//    }
//}