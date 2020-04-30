using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;

namespace MashupApi.Http.Clients
{
    public class WikiDataClient
    {
        private HttpClient Client { get; }

        private IAsyncPolicy<HttpResponseMessage> cachePolicy;
        private ILogger<WikiDataClient> logger;
        public readonly JsonSerializerOptions serializerOptions;
        public WikiDataClient(HttpClient client, IReadOnlyPolicyRegistry<string> policyRegistry, ILogger<WikiDataClient> logger)
        {
            client.BaseAddress = new Uri("https://www.wikidata.org");

            this.Client = client;
            this.cachePolicy = policyRegistry.Get<IAsyncPolicy<HttpResponseMessage>>("WikiDataCachePolicy");
            this.logger = logger;
        }
        public async Task<string> GetTitle(string identifier, string siteFilter = "")
        {
            // https://www.wikidata.org/w/api.php?action=wbgetentities&ids=Q11649&format=json&props=sitelinks&sitefilter=enwikis
            // TODO: add enwiki to retrieve only the propery we're actually after...
            var requestUrl = $"/w/api.php?action=wbgetentities&ids={identifier}&format=json&props=sitelinks";
            logger.LogDebug($"requestUrl", requestUrl);

            if (!string.IsNullOrEmpty(siteFilter))
            {
                requestUrl = $"{requestUrl}&sitefilter={siteFilter}";
            }
            var response = await this.cachePolicy.ExecuteAsync(async ct => await Client.GetAsync(requestUrl), new Context(requestUrl));

            var responseString = await response.Content.ReadAsStringAsync();
            var document = JsonDocument.Parse(responseString);
            // var xx = new StringReader(responseString).ReadToEnd();
            // JsonDocument document;
            //
            // JsonDocument.TryParseValue(new Utf8JsonReader(System.Text.Encoding.UTF8.GetEncoder().GetBytes(responseString), true, out document));

            // Get the entities property from root, 
            // find first entity and get its sitelinks property
            // TODO: If retrieving multiple mbid, we need to make sure it really is the first entity we are interested in.
            var element = document.RootElement
                .GetProperty("entities")
                .EnumerateObject().First().Value
                .GetProperty("sitelinks");

            var enwikiElement = element.EnumerateObject()
                .FirstOrDefault(prop => prop.Value.GetProperty("site").GetString() == "enwiki");

            var title = enwikiElement.Value.GetProperty("title").GetString();

            return title;
        }
    }
}
