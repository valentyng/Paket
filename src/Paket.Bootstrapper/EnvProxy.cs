﻿using System;
using System.Collections.Generic;
using System.Net;

namespace Paket.Bootstrapper
{
    internal class EnvProxy
    {
        private static readonly EnvProxy instance = new EnvProxy();
        private readonly Dictionary<string, IWebProxy> proxies = new Dictionary<string, IWebProxy>(StringComparer.OrdinalIgnoreCase);

        private static string GetEnvVarValue(string name)
        {
            // under mono, env vars are case sensitive
            return Environment.GetEnvironmentVariable(name.ToUpperInvariant())
                ?? Environment.GetEnvironmentVariable(name.ToLowerInvariant());
        }

        private string[] GetBypassList()
        {
            var noproxy = GetEnvVarValue("NO_PROXY");
            if (string.IsNullOrEmpty(noproxy))
                return new string[0];
            return noproxy.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private bool TryGetCredentials(Uri uri, out NetworkCredential credentials)
        {
            var userPass = uri.UserInfo.Split(new[] { ':' }, 2);
            if (userPass.Length == 2 && userPass[0].Length > 0)
            {
                credentials = new NetworkCredential(Uri.UnescapeDataString(userPass[0]), Uri.UnescapeDataString(userPass[1]));
                return true;
            }
            credentials = null;
            return false;
        }

        private void AddProxy(string scheme, string[] bypassList)
        {
            var envVarName = string.Format("{0}_PROXY", scheme.ToUpperInvariant());
            var envVarValue = GetEnvVarValue(envVarName);
            if (envVarValue == null)
                return;
            Uri envUri;
            if (Uri.TryCreate(envVarValue, UriKind.Absolute, out envUri))
            {
                var proxy = new WebProxy(new Uri(string.Format("{0}://{1}:{2}", scheme, envUri.Host, envUri.Port)));
                NetworkCredential credentials;
                if (TryGetCredentials(envUri, out credentials))
                    proxy.Credentials = credentials;
                proxy.BypassProxyOnLocal = true;
                proxy.BypassList = bypassList;
                proxies.Add(scheme, proxy);
            }
        }

        protected EnvProxy()
        {
            var bypassList = GetBypassList();
            AddProxy("http", bypassList);
            AddProxy("https", bypassList);
        }

        public static bool TryGetProxyFor(Uri uri, out IWebProxy proxy)
        {
            return instance.proxies.TryGetValue(uri.Scheme, out proxy);
        }
    }
}
