using System.Text.Json;
using MashupApi.Extensions;
using MashupApi.Http.Clients;
using MashupApi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace MashupApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IMongoClient, MongoClient>(serviceProvider => 
                new MongoClient(Configuration.GetConnectionString("CoverArtDbConnectionString")));
            services.AddSingleton<MongoDbService>();
            
            services.AddSingleton<IBackgroundQueue<CoverArtJob>>(new CoverArtJobQueue());
            services.AddTransient<IBackgroundJobProcessor<CoverArtJob>, CoverArtJobProcessor>();
            services.AddHostedService<BackgroundQueueService<CoverArtJob>>();
            services.AddTransient<CoverArtService>();

            services.AddPersistentCaching();
            services.AddCachePolicies();

            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    options.JsonSerializerOptions.IgnoreNullValues = true;
                    options.JsonSerializerOptions.WriteIndented = true;
                    
                });

            services.AddHttpClients();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
