using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;

namespace GitDoc
{
    internal class GitClient
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly HttpClient _httpClient;

        public GitClient(string clientId = null, string clientSecret = null)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://api.github.com");
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GitDoc", version));
        }

        public async Task<string> Markdown(string text)
        {
            var response = await _httpClient.PostAsync(BuildUrl("markdown/raw"), new StringContent(text));

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            throw new HttpRequestException("Couldn't parse response from GitHub: " + response.ReasonPhrase);
        }

        private Uri BuildUrl(string path)
        {
            var uri = new UriBuilder(_httpClient.BaseAddress);

            uri.Path = path;

            if (!String.IsNullOrWhiteSpace(_clientId) && !string.IsNullOrWhiteSpace(_clientSecret))
            {
                uri.Query = String.Format("client_id={0}&client_secret={1}", _clientId, _clientSecret);
            }

            return uri.Uri;
        }
    }
}