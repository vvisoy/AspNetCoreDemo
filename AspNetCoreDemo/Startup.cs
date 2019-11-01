using System;
using System.Net.Http;
using AspNetCoreDemo.HttpHandlers;
using AspNetCoreDemo.Interfaces;
using AspNetCoreDemo.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;

namespace AspNetCoreDemo {
   public class Startup {
      public Startup(IConfiguration configuration) {
         Configuration = configuration;
      }

      public IConfiguration Configuration { get; }

      // This method gets called by the runtime. Use this method to add services to the container.
      public void ConfigureServices(IServiceCollection services) {
         services.AddControllers();

         #region Making HTTP requests

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

         // Outgoing request middleware
         services.AddTransient<ValidateHeaderHandler>();

         services
            .AddHttpClient("externalservice", c => {
               // Assume this is an "external" service which requires an API KEY
               c.BaseAddress = new Uri("https://localhost:5001/");
            })
            .AddHttpMessageHandler<ValidateHeaderHandler>();

         services.AddTransient<SecureRequestHandler>();
         services.AddTransient<RequestDataHandler>();

         services
            .AddHttpClient("clientwithhandlers")
            // This handler is on the outside and called first during the 
            // request, last during the response.
            .AddHttpMessageHandler<SecureRequestHandler>()
            // This handler is on the inside, closest to the request being 
            // sent.
            .AddHttpMessageHandler<RequestDataHandler>();

         // Retry policy using polly
         services
            .AddHttpClient<UnreliableEndpointCallerService>()
            .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(600)));

         var timeout = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));
         var longTimeout = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(30));

         // Dynamically select policies
         services
            .AddHttpClient("conditionalpolicy")
            // Run some code to select a policy based on the request
            .AddPolicyHandler(request => request.Method == HttpMethod.Get ? timeout : longTimeout);

         // Add multiple Polly handlers
         services
            .AddHttpClient("multiplepolicies")
            .AddTransientHttpErrorPolicy(p => p.RetryAsync(3))
            .AddTransientHttpErrorPolicy(p => p.CircuitBreakerAsync(3, TimeSpan.FromSeconds(30)));

         // Add policies from the Polly registry
         var registry = services.AddPolicyRegistry();

         registry.Add("regular", timeout);
         registry.Add("long", longTimeout);

         services
            .AddHttpClient("regularTimeoutHandler")
            .AddPolicyHandlerFromRegistry("regular");

         services
            .AddHttpClient("longTimeoutHandler")
            .AddPolicyHandlerFromRegistry("long");

         // HttpClient and lifetime management
         services
            .AddHttpClient("extendedhandlerlifetime")
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));

         // Configure the HttpMessageHandler
         services
            .AddHttpClient("configured-inner-handler")
            .ConfigurePrimaryHttpMessageHandler(() => {
               return new HttpClientHandler() {
                  AllowAutoRedirect = false,
                  UseDefaultCredentials = true
               };
            });

         #endregion
      }

      private static void HandleMapTest1(IApplicationBuilder app) {
         app.Run(async context => {
            await context.Response.WriteAsync("Map Test 1");
         });
      }

      private static void HandleMapTest2(IApplicationBuilder app) {
         app.Run(async context => {
            await context.Response.WriteAsync("Map Test 2");
         });
      }

      private static void HandleBranch(IApplicationBuilder app) {
         app.Run(async context => {
            var branchVer = context.Request.Query["branch"];
            await context.Response.WriteAsync($"Branch used = {branchVer}");
         });
      }

      private static void HandleMultipleSeg(IApplicationBuilder app) {
         app.Run(async context => {
            await context.Response.WriteAsync("Map multiple segments.");
         });
      }

      // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
      public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
         #region ASP.NET Core Middleware

         // Create a middleware pipeline with IApplicationBuilder
         //app.Run(async context => {
         //   await context.Response.WriteAsync("Hello, World!");
         //});

         // Chaining multiple request delegates together
         app.Use(async (context, next) => {
            // Do work that doesn't write to the Response.
            await next.Invoke();
            // Do logging or other work that doesn't write to the Response.
         });

         //app.Run(async context => {
         //   await context.Response.WriteAsync("Hello from 2nd delegate.");
         //});

         #endregion

         if (env.IsDevelopment()) {
            app.UseDeveloperExceptionPage();
            app.UseDatabaseErrorPage();
         } else {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
         }

         app.UseHttpsRedirection();
         app.UseStaticFiles();

         //app.UseResponseCompression();

         app.UseCookiePolicy();

         app.UseRouting();
         app.UseAuthentication();

         app.UseAuthorization();
         //app.UseSession();

         //app.UseEndpoints(endpoints => {
         //   endpoints.MapRazorPages();
         //});

         // Use, Run, and Map         
         app.MapWhen(context => context.Request.Query.ContainsKey("branch"), HandleBranch);

         // Map nesting
         app.Map("/level1", levelApp1 => {
            levelApp1.Map("/level2a", level2AApp => {
               // "/level1/level2a" processing
            });
            levelApp1.Map("/level2b", level2BApp => {
               // "/level1/level2b" processing
            });
         });

         // Multiple segments
         app.Map("/map1/seg1", HandleMultipleSeg);

         app.Map("/map1", HandleMapTest1);
         app.Map("/map2", HandleMapTest2);

         app.Run(async context => {
            await context.Response.WriteAsync("Hello from non-Map delegate. <p>");
         });

         app.UseEndpoints(endpoints => {
            endpoints.MapControllers();
         });
      }
   }
}
