using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;

namespace GitDoc
{
    internal class GitClient
    {
        private readonly HttpClient _httpClient;

        public GitClient()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://api.github.com");
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GitDoc", version));
        }

        public async Task<string> Markdown(string text)
        {
            var response = await _httpClient.PostAsync("markdown/raw", new StringContent(text));

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            throw new HttpRequestException("Couldn't parse response from GitHub: " + response.ReasonPhrase);
        }
    }
}