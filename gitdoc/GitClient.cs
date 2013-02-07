using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace GitDoc
{
    internal class GitClient
    {
        private readonly HttpClient _httpClient;

        public GitClient()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://api.github.com");
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