using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ac.Net.Authentication
{
    public delegate Task OnSuccess(string token);

    public delegate Task OnError();

    public class XListener
    {
        private HttpListener listener;

        public XListener(string prefix)
        {
            listener = new HttpListener();
            listener.Prefixes.Add(prefix);
        }

        private OnSuccess SuccessDelegate { get; set; }
        private OnError ErrorDelegate { get; set; }

        private object _lock = new object();
        private bool _active;

        private bool Active
        {
            get { return _active; }
        }


        public void ClearActive()
        {
            lock (_lock)
            {
                _active = false;
            }
        }

        public void SetActive()
        {
            lock (_lock)
            {
                _active = true;
            }
        }

        public void StartListen(OnSuccess onSuccess, OnError onError)
        {
            SuccessDelegate = onSuccess;
            ErrorDelegate = onError;
            if (!listener.IsListening)
            {
                listener.Start();

                SuccessDelegate = onSuccess;
                ErrorDelegate = onError;
                SetActive();
                //  var thread = new Thread(Listen();
                Task.Factory.StartNew(async () =>
                {
                    //  t
                    while (listener.IsListening)
                    {
                        await Listen(listener, _active);
                        Thread.Sleep(100);
                    }
                });

                Console.WriteLine("Listener started");
            }
        }

        public void StopListen()
        {
            if (listener.IsListening)
            {
                SuccessDelegate = null;
                ErrorDelegate = null;
                listener.Stop();
                try
                {
                }
                catch (Exception)
                {
                }
                Console.WriteLine("Listener stopped");
            }
        }

        private async Task Listen(HttpListener listener, bool isListening)
        {
            try

            {
                if (isListening)
                {
                    Debug.WriteLine("Listening");
                    HttpListenerContext? ctTemp;
                    HttpListenerContext ctx;
                    try
                    {
                        ctTemp = listener.GetContext();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                        return;
                    }

                    ctx = ctTemp!;

                    using (HttpListenerResponse resp = ctx.Response)
                    {
                        var tokenRequestCode = System.Web.HttpUtility.ParseQueryString(ctx.Request.Url.Query).Get("code");
                        string data = "";

                        // see if request code is correct
                        if (string.IsNullOrEmpty(tokenRequestCode))
                        {
                            resp.StatusCode = (int)HttpStatusCode.Unauthorized;
                            resp.StatusDescription = "User not authorized";
                            data = Html.errorHtml;
                        }
                        else
                        {
                            resp.StatusCode = (int)HttpStatusCode.OK;
                            resp.StatusDescription = "Status OK";
                            data = Html.successHtml;
                        }

                        // send response
                        var url = ctx.Request.Url;
                        resp.StatusCode = (int)HttpStatusCode.OK;
                        resp.StatusDescription = "Status OK";

                        resp.Headers.Set("Content-Type", "text/html");

                        byte[] buffer = Encoding.UTF8.GetBytes(data);
                        resp.ContentLength64 = buffer.Length;

                        using (Stream ros = resp.OutputStream)
                        {
                            ros.Write(buffer, 0, buffer.Length);
                        }

                        if (!string.IsNullOrEmpty(tokenRequestCode))
                        {
                            Debug.WriteLine("Handled");
                            if (SuccessDelegate != null)
                            {
                                var del = SuccessDelegate;
                                SuccessDelegate = null;
                                del(tokenRequestCode);
                            }
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("Done");
                    StopListen();
                }
            }
            catch (HttpListenerException)
            {
                Console.WriteLine("screw you guys, I'm going home!");
            }
        }
    }
}