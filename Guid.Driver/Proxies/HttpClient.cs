using System;
using System.Collections.Generic;
using System.Text;

namespace Guid.Driver.Proxies
{
    /// <summary>
    /// HttpClient singleton.
    /// </summary>
    public class HttpClient : System.Net.Http.HttpClient
    {
        public static readonly HttpClient Instance = new HttpClient();

        static HttpClient()
        {
            Instance.BaseAddress = new Uri("https://localhost:44384");
        }
    }
}
