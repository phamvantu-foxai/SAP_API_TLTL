using Microsoft.Extensions.Options;
using SAP_API.Model;
using System.Net;
using System;
using System.Text;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Newtonsoft.Json;

namespace SAP_API.Service
{
    public class SapSessionManager
    {
        private readonly APISetting _settings;
        private readonly HttpClient _httpClient;
        private string _sessionId;
        private string _routeId;
        private DateTime _sessionExpire;

        public SapSessionManager(IOptions<APISetting> settings, HttpClient httpClient)
        {
            _settings = settings.Value;
            _httpClient = httpClient;
        }

        public async Task<(string, string)> GetSessionAsync()
        {
            if (string.IsNullOrEmpty(_sessionId) || DateTime.Now >= _sessionExpire)
            {
                await LoginAsync();
            }

            return (_sessionId, _routeId);
        }

        public async Task<(Cookies, bool,string)> LoginAsync()
        {
            var body = new
            {
                CompanyDB = _settings.CompanyDB,
                UserName = _settings.UserName,
                Password = _settings.Password
            };
            var json = JsonConvert.SerializeObject(body);
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create($"{_settings.BaseUrl}" + "/Login");
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";
            httpWebRequest.KeepAlive = true;
            httpWebRequest.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            httpWebRequest.Headers.Add("B1S-WCFCompatible", "true");
            httpWebRequest.Headers.Add("B1S-MetadataWithoutSession", "true");
            httpWebRequest.Accept = "*/*";
            httpWebRequest.ServicePoint.Expect100Continue = false;
            httpWebRequest.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            httpWebRequest.AutomaticDecompression = DecompressionMethods.GZip;

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            { streamWriter.Write(json); }
            try
            {
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                var cookie = httpResponse.Headers.GetValues("Set-Cookie");
                Cookies cookiess = new Cookies();
                int endIndex = cookie[1].ToString().IndexOf(";");
                string routeid = cookie[1].ToString().Substring(0, endIndex);
                endIndex = cookie[0].ToString().IndexOf(";");
                string sesion = cookie[0].ToString().Substring(0, endIndex + 1);
                cookiess.ROUTEID = routeid;
                cookiess.B1SESSION = sesion;
                cookiess.SessionTime = DateTime.Now.AddMinutes(20);
                return(cookiess, true,null);
            }
            catch (WebException ex)
            {
                return (null,true, "Không lấy được B1SESSION từ Service Layer");
            }
        }

        public async Task LogoutAsync()
        {
            if (!string.IsNullOrEmpty(_sessionId))
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/Logout");
                request.Headers.Add("Cookie", $"B1SESSION={_sessionId}");
                await _httpClient.SendAsync(request);
                _sessionId = null;
            }
        }
    }
}
