using Ac.Net.Authentication.Models;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Ac.Net.Authentication
{
    public class ThreeLeggedCycle : ITokenProvider
    {
        // private readonly ConcurrentQueue<ThreeLeggedMananger> queue = new ConcurrentQueue<ThreeLeggedMananger>();

        private ImmutableList<ITokenManager>? managers { get => managers1; set { lock (_lock) { managers1 = value; setCount(); } } }

        private readonly object _lock = new object();

        private readonly object _counterLock = new object();

        private readonly object _resetLock = new object();

        private int cnter = 0;
        private int count = 1;
        private int bigCounter = 1;
        private bool waiting = false;


        private async Task ForceRefresh()
        {

            if (AuthData.Count < 1)
            {

                return;
            }

            if (AuthData.Count != managers.Count)
            {

                return;
            }
            try
            {
                int counter = -1;
                foreach (var item in AuthData)
                {
                    counter++;
                    TokenData? tokenData = null;
                    try
                    {
 
                        var token = await managers[counter].ForceRefresh();

                        if (token != null)
                        {
                            item.RefreshToken = token.refresh_token;
                        }
                    }
                    catch (Exception ex)
                    {

                        Serilog.Log.Error(ex, "Error refreshing token");
                    }

                }

                Save();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Token refresh complete");
            }
    
        }

        private int increment()
        {
            lock (_counterLock)
            {
                if (cnter >= count)
                {
                    cnter = -1;
                }
                bigCounter++;
                if (bigCounter > 5000)
                {
                    bigCounter = 0;                  
                    var t = Task.Factory.StartNew(async () => { await ForceRefresh(); });
                }
                cnter = cnter + 1;
            }
            return cnter;
        }

        private void setCount()
        {
            lock (_counterLock) { count = managers.Count - 1; }
        }

        private void reset()
        {
            lock (_counterLock) { cnter = 0; }
        }

        public ConcurrentBag<AuthParameters> AuthData { get; set; } = new ConcurrentBag<AuthParameters>();

        public ThreeLeggedCycle()
        {
        }

        public async Task<string> GetToken()
        {
            if (managers == null) throw new Exception("Toke cycler not inititalized");
            var i = increment();
            var token = await managers[i].GetToken();
            return token;
        }




        public bool IsEnabled
        { get { return managers.Count > 0; } }

        private bool IsInitialized = false;
        private ImmutableList<ITokenManager> managers1;

        public async Task Initialize()
        {
            if (IsInitialized) { return; }
            lock (_lock) { IsInitialized = true; }
            var adata = Load();
            if (adata != null)
            {
                AuthData.Clear();
                foreach (var item in adata)
                {
                    AuthData.Add(item);
                }
            }

            var result = new List<ThreeLeggedManager>();
            foreach (var item in AuthData)
            {
                var newManager = new ThreeLeggedManager(item);
                newManager.OnTokenUpdate += OnTokeUpdate;
                try
                {
                    var token = await newManager.GetToken();
                    result.Add(newManager);
                }
                catch
                {
                }
            }
            Save();
            if (result.Count > 0)
            {
                managers = ImmutableList<ITokenManager>.Empty;
                managers = managers.AddRange(result.ToArray());
            }
        }

        private List<AuthParameters> Load()
        {
            var path = GetPath();
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
                    //  json = EncryptionHelper.Decrypt(json);
                    var ap = JsonConvert.DeserializeObject<List<AuthParameters>>(json);
                    Dictionary<string, AuthParameters> d = new Dictionary<string, AuthParameters>();
                    foreach (var param in ap)
                    {
                        if (!d.ContainsKey(param.ClientId))
                        {
                            d[param.ClientId] = param;
                        }
                    }
                    return d.Values.ToList();
                }
                catch (Exception ex)
                {
                    Log.Error("Error Encrypted Reading Auth Parameters");
                    Log.Error(ex.Message);
                    return null;
                }
            }
        }

        private void OnTokeUpdate(ITokenManager manager, TokenData newToken)
        {
            if (newToken?.isValid ?? false)
            {
                var aid = manager.GetClientId();
                foreach (var item in AuthData)
                {
                    if (aid == item.ClientId)
                    {
                        item.RefreshToken = manager.GetRefreshToken();
                        break;
                    }
                }

                Save();
            }
        }

        private string GetPath()
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "AData.json");
        }

        private void Save()
        {
            var path = GetPath();
            var json = JsonConvert.SerializeObject(AuthData);
            var fileInfo = new FileInfo(path);
            if (!fileInfo.Directory.Exists)
            {
                fileInfo.Directory.Create();
            }
            using (var file = File.CreateText(path))
            {
                //  file.Write(EncryptionHelper.Encrypt(json));
                file.Write(json);
                file.Flush();
            }
        }

        public async Task<List<string>> CycleTokens()
        {
            var result = new List<string>();
            var remove = new List<ITokenManager>();

            if (managers == null)
            {
                await Initialize();
            }

            foreach (var item in managers)
            {
                try
                {
                    var t = await item.GetToken();
                    result.Add(t);
                }
                catch (Exception)
                {

                    remove.Add(item);
                }
            }

            if (remove.Count > 0)
            {
                managers = managers.RemoveRange(remove);
            }

            Save();
            return result;
        }

        public async void AddManager(AuthParameters tokenParameters)
        {
            if (tokenParameters.IsValid())
            {
                try
                {
                    var newManager = new ThreeLeggedManager(tokenParameters);
                    newManager.OnTokenUpdate += OnTokeUpdate;
                    var t = await newManager.GetToken();
                    managers = managers.Add(newManager);
                    AuthData.Add(tokenParameters);
                }
                catch (Exception)
                {
                    throw;
                }
                await CycleTokens();
                Save();
            }
        }
    }
}