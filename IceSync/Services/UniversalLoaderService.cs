using IceSync.Data;
using IceSync.Services.Interfaces;

namespace IceSync.Services
{
    public class UniversalLoaderService : IUniversalLoaderService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UniversalLoaderService> _logger;
        private readonly string _tokenResponse;
        private readonly DateTime _tokenExpiration;

        public UniversalLoaderService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<UniversalLoaderService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> GetTokenAsync()
        {
            if (_tokenResponse != null && _tokenExpiration > DateTime.UtcNow)
            {
                return _tokenResponse;
            }

            //Getting the credentials for the API from the appsettings.json file
            var ApiCompanyId = _configuration["UniversalLoader:ApiCompanyId"];
            var ApiUserId = _configuration["UniversalLoader:ApiUserId"];
            var ApiUserSecret = _configuration["UniversalLoader:ApiUserSecret"];

            var client = _httpClientFactory.CreateClient();

            var tokenResponse = await client.PostAsJsonAsync(
                $"{_configuration["UniversalLoader:ApiBaseUrl"]}/authenticate",
                new { ApiCompanyId, ApiUserId, ApiUserSecret });

            var tokenContent = "";

            if (tokenResponse.IsSuccessStatusCode)
            {
                tokenContent = await tokenResponse.Content.ReadAsStringAsync();
                _logger.LogInformation($"Token response: {tokenContent}");
            }
            else
            {
                _logger.LogError($"Failed to retrieve token: {tokenResponse.ReasonPhrase}");
            }

            return tokenContent;
        }

        public async Task<List<Workflow>> GetWorkflowsAsync()
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", await GetTokenAsync());

            var response = await client.GetAsync($"{_configuration["UniversalLoader:ApiBaseUrl"]}/workflows");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<Workflow>>();
            }

            return new List<Workflow>();
        }

        public async Task<bool> RunWorkflowAsync(int workflowId)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", await GetTokenAsync());

            var response = await client.PostAsync($"{_configuration["UniversalLoader:ApiBaseUrl"]}/workflows/{workflowId}/run", null);

            return response.IsSuccessStatusCode;
        }
    }
}

