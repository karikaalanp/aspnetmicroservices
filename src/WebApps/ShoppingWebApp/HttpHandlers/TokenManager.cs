using IdentityModel;
using IdentityModel.Client;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Threading.Tasks;


namespace ShoppingWebApp.HttpHandlers
{
    ///  <summary> 
    /// Access TokenManager
    ///  </summary> 
    public class TokenManager
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _httpClientFactory;
        IConfiguration _configuration;

        public string AccessToken {get; set;}
        public string RefreshToken { get; set; }

        public TokenManager(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClient = _httpClient ?? throw new ArgumentNullException(nameof(_httpClient));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Access Token
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetValidAccessTokenAsync()
        {
            var accessToken = await _httpContextAccessor.HttpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);
            // Get the validity period from Access Token parsing 
            var _accessTokenExpire = GetExpireTimeFromAccessToken(accessToken);

            // If the Access Token expires, update the token 
            if (_accessTokenExpire < DateTime.UtcNow)
            {
                // Update token 
                await RefreshTokenAsync();
            }

            return accessToken;
        }

        // Get the validity period from Access Token
        private DateTime GetExpireTimeFromAccessToken(string? accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return DateTime.MinValue;

            var jwtSecurityToken = new JwtSecurityToken(accessToken);

            return jwtSecurityToken.ValidTo;
        }

        //token
        private async Task RefreshTokenAsync()
        {
            // Discover authentication service endpoint 
            var _httpClient = _httpClientFactory.CreateClient("IdentityClient");

            var discoveryResponse = await _httpClient.GetDiscoveryDocumentAsync();
            if (discoveryResponse.IsError)
            {
                throw new Exception(discoveryResponse.Error);
            }

            // Request token according to Refresh Token 
            var tokenResponse = await _httpClient.RequestRefreshTokenAsync(new RefreshTokenRequest
            {
                Address = discoveryResponse.TokenEndpoint,
                ClientId = _configuration["IdentityServerSettings:ClientId"],
                ClientSecret = _configuration["IdentityServerSettings:ClientSecret"].ToSha256(),
                RefreshToken = "refresh_token"
            });

            if (tokenResponse.IsError)
            {
                throw new Exception(tokenResponse.Error);
            }

            // Save the new token 
            AccessToken = tokenResponse.AccessToken;
            RefreshToken = tokenResponse.RefreshToken;

            Console.WriteLine($"token, {Environment.NewLine}AccessToken={AccessToken}{Environment.NewLine}RefreshToken={RefreshToken}");
        }
    }

}
