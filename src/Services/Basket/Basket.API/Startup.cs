using Basket.API.GrpcServices;
using Basket.API.Repositories;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Basket.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            Startup.Configurations = configuration;
        }

        public IConfiguration Configuration { get; }
        public static IConfiguration Configurations { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();
            services.AddSwaggerGen(Options =>
            {
                Options.SwaggerDoc("v1", new OpenApiInfo { Title = "Basket.API", Version = "v1" });
                Options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri(Configuration["ApiSettings:IdentityServerAuthority"] + "/connect/authorize"),
                            TokenUrl = new Uri(Configuration["ApiSettings:IdentityServerAuthority"] + "/connect/token"),
                            Scopes = new Dictionary<string, string>
                           {
                                {"basket.api", "cart management service"}
                           }
                        }
                    }
                });

                Options.OperationFilter<AuthorizeCheckOperationFilter>();
            });

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = Configuration.GetValue<string>("CacheSetting:ConnectionString");
            });

            services.AddScoped<IBasketRepository, BasketRepository>();
            services.AddGrpcClient<Discount.Grpc.Protos.Discount.DiscountClient>(
                options => options.Address = new Uri(Configuration["GrpcSettings:DiscountUrl"]));
            services.AddScoped<DiscountGrpcService>();

            services.AddAutoMapper(typeof(Startup));

            services.AddMassTransit(config =>
            {
                config.UsingRabbitMq((context, configurator) => {
                    configurator.Host(Configuration["EventBusSettings:HostAddress"]);
                    });
            
            });

            //services.AddAuthentication("Bearer")
            //        .AddJwtBearer("Bearer", options =>
            //        {
            //            options.Authority = Configuration["ApiSettings:IdentityServerAuthority"];
            //            //options.RequireHttpsMetadata = false;
            //            options.TokenValidationParameters = new TokenValidationParameters
            //            {
            //                ValidateAudience = false
            //            };
            //        });

            services.AddAuthentication("Bearer")
           .AddIdentityServerAuthentication("Bearer", options =>
           {
               options.ApiName = Configuration["ApiSettings:AllowedScope1"];
               options.Authority = Configuration["ApiSettings:IdentityServerAuthority"];
               options.LegacyAudienceValidation = true;
           }); 


            services.AddAuthorization(options =>
            {
                options.AddPolicy("BasketPolicy", policy => policy.RequireClaim("client_id", Configuration["ApiSettings:ClientId"]));
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
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

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Basket.API v1");
                options.OAuthClientId(Configuration["ApiSettings:ClientId"]);
                options.OAuthAppName(Configuration["ApiSettings:ClientName"]);
                options.OAuthUsePkce();
                options.OAuthClientSecret("secret");
            });

            app.UseEndpoints(endpoints =>
            {
                //endpoints.MapControllers();
                endpoints.MapDefaultControllerRoute();
            });
        }
    }

    public class AuthorizeCheckOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var hasAuthorize = context.MethodInfo.DeclaringType.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() ||
                               context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any();

            if (hasAuthorize)
            {
                operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
                operation.Responses.Add("403", new OpenApiResponse { Description = "Forbidden" });

                operation.Security = new List<OpenApiSecurityRequirement>
                {
                    new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecurityScheme {Reference = new OpenApiReference {Type = ReferenceType.SecurityScheme, Id = "oauth2"}}]
                            = new[] { Startup.Configurations["ApiSettings:AllowedScope1"] }
                    }
                };
            }
        }
    }
}
