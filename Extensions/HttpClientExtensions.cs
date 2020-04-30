using System;
using System.Net;
using System.Net.Http;
using MashupApi.Http;
using MashupApi.Http.Clients;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Caching;
using Polly.Caching.Memory;
using Polly.Extensions.Http;
using Polly.Registry;
using Polly.Timeout;
using Microsoft.Extensions.Logging;

namespace MashupApi.Extensions
{
    public static class HttpClientExtensions
    {
        public static IServiceCollection AddPersistentCaching(this IServiceCollection services) {
            services.AddSingleton<IAsyncCacheProvider, MemoryCacheProvider>();

            return services;
        }
        public static IServiceCollection AddHttpClients(this IServiceCollection services)
        {
            services.AddTransient<LoggingHttpMessageHandler>();

            Func<HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>> policySelector = p =>
                                    HttpPolicyExtensions.HandleTransientHttpError()
                                    .OrResult(m => m.StatusCode == HttpStatusCode.NotFound)
                                    .Or<TimeoutRejectedException>()
                                    .WaitAndRetryAsync(3, attempt =>
                                    {
                                        p.Headers.Remove("X-Retry");
                                        p.Headers.Add("X-Retry", attempt.ToString());
                                        return TimeSpan.FromSeconds(3 * attempt);
                                    });

            services.AddHttpClient<MusicBrainzClient>()
                .AddHttpMessageHandler<LoggingHttpMessageHandler>()
                .AddPolicyHandler(policySelector);

            services.AddHttpClient<WikiDataClient>()
                .AddHttpMessageHandler<LoggingHttpMessageHandler>()
                .AddPolicyHandler(policySelector);

            services.AddHttpClient<WikipediaClient>()
                .AddHttpMessageHandler<LoggingHttpMessageHandler>()
                .AddPolicyHandler(policySelector);

            services.AddHttpClient("coverArtClient", client =>
                {
                    client.BaseAddress = new Uri("http://coverartarchive.org/");
                })
                .AddHttpMessageHandler<LoggingHttpMessageHandler>();

            return services;
        }
        public static IServiceCollection AddCachePolicies(this IServiceCollection services)
        {
            Func<Context, HttpResponseMessage, Ttl> cacheOnly200OKFilter = (context, result) => 
                new Ttl(
                    timeSpan: result.StatusCode == HttpStatusCode.OK ? TimeSpan.FromMinutes(5) : TimeSpan.Zero,
                    slidingExpiration: true);

            Func<Context, HttpResponseMessage, Ttl> cacheAllRequestsFilter = (context, result) => 
                new Ttl(TimeSpan.FromMinutes(5), slidingExpiration: true);

            services.AddMemoryCache();
            services.AddSingleton<IReadOnlyPolicyRegistry<string>, PolicyRegistry>((serviceProvider) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<Startup>>();
                var registry = new PolicyRegistry();

                var cachingPolicy = Policy.CacheAsync<HttpResponseMessage>(
                    serviceProvider.GetRequiredService<IAsyncCacheProvider>()
                        .AsyncFor<HttpResponseMessage>(),
                        new ResultTtl<HttpResponseMessage>(cacheOnly200OKFilter),
                    onCachePut: (context, key) => logger.LogDebug($"CachePUT {key}"),
                    onCacheGet: (context, key) => logger.LogDebug($"CacheHIT - OperationKey: {context.OperationKey} ({key})"),
                    onCachePutError: (context, key, error) => logger.LogWarning(error, $"Could not PUT {key}"), 
                    onCacheGetError: (context, key, error) => logger.LogWarning(error, $"Could not GET {key}"),
                    onCacheMiss: (context, str) => logger.LogDebug($"CacheMISS - OperationKey: {context.OperationKey} ({str})")
                );

                registry.Add("MusicBrainzCachePolicy", cachingPolicy);
                registry.Add("WikiDataCachePolicy", cachingPolicy);
                registry.Add("WikipediaCachePolicy", cachingPolicy);
                registry.Add("CoverArtCachePolicy", cachingPolicy);

                return registry;
            });

            return services;
        }
    }
}
