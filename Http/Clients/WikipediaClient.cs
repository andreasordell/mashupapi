using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MashupApi.Http.Clients.Exceptions;
using Polly;
using Polly.Registry;

namespace MashupApi.Http.Clients
{
    public class WikipediaClient
    {
        private readonly HttpClient _client;
        private readonly IAsyncPolicy<HttpResponseMessage> _cachePolicy;

        public WikipediaClient(HttpClient client, IReadOnlyPolicyRegistry<string> policyRegistry)
        {
            client.BaseAddress = new Uri("https://en.wikipedia.org");

            _client = client;
            _cachePolicy = policyRegistry.Get<IAsyncPolicy<HttpResponseMessage>>("WikipediaCachePolicy");
        }
        public async Task<string> Get(string name)
        {
            var encodedName = System.Web.HttpUtility.UrlEncode(name);
            
            var requestUrl = $"w/api.php?action=query&format=json&prop=extracts&exintro=true&redirects=true&titles={encodedName}";
            var response = await this._cachePolicy.ExecuteAsync(
                async ct => await _client.GetAsync(requestUrl), 
                new Context(requestUrl));

            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var document = JsonDocument.Parse(responseString);

            var pagesElement = document.RootElement
                .GetProperty("query")
                .GetProperty("pages")
                .EnumerateObject()
                .FirstOrDefault();

            // The page does not exist, return null.
            if (pagesElement.Name == "-1") {
                return null;
            }

            return pagesElement.Value.GetProperty("extract").GetString();
        }
    }
}
