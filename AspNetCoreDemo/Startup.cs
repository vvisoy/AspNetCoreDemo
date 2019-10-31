using System;
using AspNetCoreDemo.HttpHandlers;
using AspNetCoreDemo.Interfaces;
using AspNetCoreDemo.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AspNetCoreDemo {
   public class Startup {
      public Startup(IConfiguration configuration) {
         Configuration = configuration;
      }

      public IConfiguration Configuration { get; }

      // This method gets called by the runtime. Use this method to add services to the container.
      public void ConfigureServices(IServiceCollection services) {
         services.AddControllers();

         //Basic usage
         services.AddHttpClient();

         // Named client
         services.AddHttpClient("github", c => {
            c.BaseAddress = new Uri("https://api.github.com");
            // Github API versioning
            c.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
            // Github requires a user-agent
            c.DefaultRequestHeaders.Add("User-Agent", "HttpClientFactory-Sample");
         });

         // Typed client
         services.AddHttpClient<GitHubService>();

         services
            .AddHttpClient("hello", c => {
               c.BaseAddress = new Uri("http://localhost:5000");
            })
            .AddTypedClient(c => Refit.RestService.For<IHelloClient>(c));

         services.AddTransient<ValidateHeaderHandler>();
      }

      // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
      public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
         if (env.IsDevelopment()) {
            app.UseDeveloperExceptionPage();
         }

         app.UseHttpsRedirection();

         app.UseRouting();

         app.UseAuthorization();

         app.UseEndpoints(endpoints => {
            endpoints.MapControllers();
         });
      }
   }
}
