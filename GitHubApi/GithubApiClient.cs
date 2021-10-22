using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using GitHubApi.Dto;

namespace GitHubApi
{
    public class GithubApiClient
    {
        private const string GithubApiUrl = "https://api.github.com/orgs/";
        private const string ReposApi = "repos";


        private string _token;
        private static readonly HttpClient HttpClient = new HttpClient
        {
            BaseAddress = new Uri(GithubApiUrl)
        };

    public GithubApiClient(string token)
        {
            HttpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
            HttpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MyApplication", "1"));
            HttpClient.DefaultRequestHeaders.Add("Authorization", "token " + token);
            _token = token;
        }

        public async Task<IList<GithubRepositoryDto>> GetRepositoriesFromOrganisationAsync(string organizationName)
        {
            var pageLength = 100;
            var requestUri = $"{organizationName}/{ReposApi}?per_page={pageLength}";

            var response = await HttpClient.GetAsync(requestUri);
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var repositories = JsonSerializer.Deserialize<IList<GithubRepositoryDto>>(responseJson, options);
                return repositories;
            }
            else
            {
                throw new HttpRequestException("Retrieving repositories from github did not succeed");
            }
        }
    }
}
