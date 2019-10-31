﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace AspNetCoreDemo.Services {
   public class GitHubService {
      public HttpClient Client { get; }

      public GitHubService(HttpClient client) {
         client.BaseAddress = new Uri("https://api.github.com/");
         // GitHub API versioning
         client.DefaultRequestHeaders.Add("Accept",
             "application/vnd.github.v3+json");
         // GitHub requires a user-agent
         client.DefaultRequestHeaders.Add("User-Agent",
             "HttpClientFactory-Sample");

         Client = client;
      }

      public async Task<IEnumerable<GitHubIssue>> GetAspNetDocsIssues() {
         var response = await Client.GetAsync(
             "/repos/aspnet/AspNetCore.Docs/issues?state=open&sort=created&direction=desc");

         response.EnsureSuccessStatusCode();

         using var responseStream = await response.Content.ReadAsStreamAsync();
         return await JsonSerializer.DeserializeAsync
             <IEnumerable<GitHubIssue>>(responseStream);
      }
   }
}
