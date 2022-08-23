using IdentityModel;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using static IdentityServer4.Models.IdentityResources;

namespace ShoppingWebApp.HttpHandlers
{
    public class AuthenticationDelegatingHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _httpClientFactory;
        IConfiguration _configuration;

        public AuthenticationDelegatingHandler(IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var accessToken = await _httpContextAccessor
                .HttpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);

            //var accessToken = await GetValidAccessTokenAsync();

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.SetBearerToken(accessToken);
            }

            return await base.SendAsync(request, cancellationToken);
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
        private async Task<string> RefreshTokenAsync()
        {
            // Discover authentication service endpoint 
            var _httpClient = _httpClientFactory.CreateClient("IdentityClient");

            var discoveryResponse = await _httpClient.GetDiscoveryDocumentAsync();
            if (discoveryResponse.IsError)
            {
                throw new Exception(discoveryResponse.Error);
            }

            TokenClientOptions tokenClientOptions = new TokenClientOptions();
            tokenClientOptions.Address = _configuration["ApiSettings:IdentityServerAuthority"];
            tokenClientOptions.ClientId = _configuration["ApiSettings:ClientId"];
            tokenClientOptions.ClientSecret = _configuration["ApiSettings:ClientSecret"].ToSha256();

            var tokenClient = new TokenClient(_httpClient, tokenClientOptions);
            // This will request a new access_token and a new refresh token.
            var tokenResponse = await tokenClient.RequestRefreshTokenAsync("refresh_token");

            if (tokenResponse.IsError)
            {
                throw new Exception(tokenResponse.Error);
            }

            var oldIdToken = await _httpContextAccessor.HttpContext.GetTokenAsync("id_token");

            var tokens = new List<AuthenticationToken>
            {
                new AuthenticationToken
                {
                    Name = OpenIdConnectParameterNames.IdToken,
                    Value = oldIdToken
                },
                new AuthenticationToken
                {
                    Name = OpenIdConnectParameterNames.AccessToken,
                    Value = tokenResponse.AccessToken
                },
                new AuthenticationToken
                {
                    Name = OpenIdConnectParameterNames.RefreshToken,
                    Value = tokenResponse.RefreshToken
                }
            };


            //// Request token according to Refresh Token 
            //var tokenResponse = await _httpClient.RequestRefreshTokenAsync(new RefreshTokenRequest
            //{
            //    Address = _configuration["ApiSettings:IdentityServerAuthority"],
            //    ClientId = _configuration["ApiSettings:ClientId"],
            //    ClientSecret = _configuration["ApiSettings:ClientSecret"],
            //    GrantType = "authorization_code",
            //    RefreshToken = "refresh_token",                
            //});

            //if (tokenResponse.IsError)
            //{
            //    throw new Exception(tokenResponse.Error);
            //}

            // Save the new token 
            //AccessToken = tokenResponse.AccessToken;
            //RefreshToken = tokenResponse.RefreshToken;

            Console.WriteLine($"token, {Environment.NewLine}AccessToken={tokenResponse.AccessToken}{Environment.NewLine}RefreshToken={tokenResponse.RefreshToken}");

            return tokenResponse.AccessToken;
        }

    }


}
