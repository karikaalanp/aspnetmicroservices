using IdentityModel;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.Owin.Security.OpenIdConnect;
using ShoppingWebApp.HttpHandlers;
using ShoppingWebApp.Services;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ShoppingWebApp
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
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.Events = new CookieAuthenticationEvents
                    {
                        // this event is fired everytime the cookie has been validated by the cookie middleware,
                        // so basically during every authenticated request
                        // the decryption of the cookie has already happened so we have access to the user claims
                        // and cookie properties - expiration, etc..
                        OnValidatePrincipal = async x =>
                        {
                            //var accessToken = await x.HttpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);

                            //var jwtSecurityToken = new JwtSecurityToken(accessToken);

                            // since our cookie lifetime is based on the access token one,
                            // check if we're more than halfway of the cookie lifetime
                            var now = DateTimeOffset.UtcNow;
                            var timeElapsed = now.Subtract(x.Properties.IssuedUtc.Value);
                            var timeRemaining = x.Properties.ExpiresUtc.Value.Subtract(now);

                            if (timeElapsed > timeRemaining)
                            //if (jwtSecurityToken.ValidTo < DateTime.UtcNow)
                            { 
                                var identity = (ClaimsIdentity)x.Principal.Identity;
                                var accessTokenClaim = identity.FindFirst("access_token");
                                var refreshTokenClaim = identity.FindFirst("refresh_token");

                                // if we have to refresh, grab the refresh token from the claims, and request
                                // new access token and refresh token
                                var refreshToken = OpenIdConnectGrantTypes.RefreshToken;
                                if (refreshTokenClaim != null) refreshToken = refreshTokenClaim.Value;
                                var response = await new HttpClient().RequestRefreshTokenAsync(new RefreshTokenRequest
                                {
                                    Address = Configuration["ApiSettings:IdentityServerAuthority"] + "/connect/token",
                                    ClientId = Configuration["ApiSettings:ClientId"],
                                    ClientSecret = Configuration["ApiSettings:ClientSecret"].ToSha256(),
                                    GrantType= OpenIdConnectGrantTypes.AuthorizationCode,
                                    RefreshToken = refreshToken
                                });

                                if (!response.IsError)
                                {
                                    // everything went right, remove old tokens and add new ones
                                    identity.RemoveClaim(accessTokenClaim);
                                    identity.RemoveClaim(refreshTokenClaim);

                                    identity.AddClaims(new[]
                                    {
                                        new Claim("access_token", response.AccessToken),
                                        new Claim("refresh_token", response.RefreshToken)
                                    });

                                    // indicate to the cookie middleware to renew the session cookie
                                    // the new lifetime will be the same as the old one, so the alignment
                                    // between cookie and access token is preserved
                                    x.ShouldRenew = true;
                                }
                            }
                        }
                    };
                })
                .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = Configuration["ApiSettings:IdentityServerAuthority"];
                    //options.RequireHttpsMetadata = false;
                    options.ClientId = Configuration["ApiSettings:ClientId"];
                    options.ClientSecret = Configuration["ApiSettings:ClientSecret"];
                    options.ResponseType = Configuration["ApiSettings:ResponseType"];
                    options.UsePkce = true;

                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    //options.Scope.Add("address");
                    //options.Scope.Add("email");
                    //options.Scope.Add("roles");

                    options.ClaimActions.DeleteClaim("sid");
                    options.ClaimActions.DeleteClaim("idp");
                    options.ClaimActions.DeleteClaim("s_hash");
                    options.ClaimActions.DeleteClaim("auth_time");
                    options.ClaimActions.MapUniqueJsonKey("role", "role");

                    options.Scope.Add(Configuration["ApiSettings:AllowedScope1"]);
                    options.Scope.Add(Configuration["ApiSettings:AllowedScope2"]);
                    options.Scope.Add(Configuration["ApiSettings:AllowedScope2"]);

                    options.SaveTokens = true;
                    options.GetClaimsFromUserInfoEndpoint = true;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = JwtClaimTypes.GivenName,
                        RoleClaimType = JwtClaimTypes.Role
                    };

                });

            #region project services

            // 1 create an HttpClient used for accessing the Movies.API
            services.AddTransient<AuthenticationDelegatingHandler>();

            services.AddHttpClient<ICatalogService, CatalogService>(client =>
            {
                client.BaseAddress = new Uri(Configuration["ApiSettings:APIGatewayUrl"]); // API GATEWAY URL
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
            }).AddHttpMessageHandler<AuthenticationDelegatingHandler>();

            services.AddHttpClient<IBasketService, BasketService>(client =>
            {
                client.BaseAddress = new Uri(Configuration["ApiSettings:APIGatewayUrl"]); // API GATEWAY URL
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
            }).AddHttpMessageHandler<AuthenticationDelegatingHandler>();

            services.AddHttpClient<IOrderService, OrderService>(client =>
            {
                client.BaseAddress = new Uri(Configuration["ApiSettings:APIGatewayUrl"]); // API GATEWAY URL
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
            }).AddHttpMessageHandler<AuthenticationDelegatingHandler>();


            // 2 create an HttpClient used for accessing the IDP
            services.AddHttpClient("IdentityClient", client =>
            {
                client.BaseAddress = new Uri(Configuration["ApiSettings:IdentityServerAuthority"]); ;
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
            });

            services.AddHttpContextAccessor();

            //services.AddHttpClient<ICatalogService, CatalogService>(c =>
            //    c.BaseAddress = new Uri(Configuration["ApiSettings:APIGatewayUrl"]));
            //services.AddHttpClient<IBasketService, BasketService>(c =>
            //    c.BaseAddress = new Uri(Configuration["ApiSettings:APIGatewayUrl"]));
            //services.AddHttpClient<IOrderService, OrderService>(c =>
            //    c.BaseAddress = new Uri(Configuration["ApiSettings:APIGatewayUrl"]));

            #endregion

            services.AddCors();
            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
                //test ci
            }


            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseCors(builder => builder
           .AllowAnyOrigin()
           .AllowAnyHeader()
           .AllowAnyMethod());

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages().RequireAuthorization(); 
            });
        }
    }
}
