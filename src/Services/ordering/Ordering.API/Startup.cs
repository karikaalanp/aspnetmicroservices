using EventBus.Messages.Common;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Ordering.API.EventBusConsumer;
using Ordering.Application;
using Ordering.Infrastructure;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ordering.API
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


        public void ConfigureServices(IServiceCollection services)
        {
            services.AddApplicationServices();
            services.AddInfrastructureServices(Configuration);

            // MassTransit-RabbitMQ Configuration
            services.AddMassTransit(config =>
            {

                config.AddConsumer<BasketCheckoutConsumer>();

                config.UsingRabbitMq((ctx, cfg) =>
                {
                    cfg.Host(Configuration["EventBusSettings:HostAddress"]);

                    cfg.ReceiveEndpoint(EventBusConstants.BasketCheckoutQueue, c =>
                    {
                        c.ConfigureConsumer<BasketCheckoutConsumer>(ctx);
                    });
                });
            });

             //services.AddMassTransitHostedService();

            // General Configuration
            services.AddScoped<BasketCheckoutConsumer>(); 

            services.AddAutoMapper(typeof(Startup));

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
                options.AddPolicy("OrderPolicy", policy => policy.RequireClaim("client_id", Configuration["ApiSettings:ClientId"]));
            });

            services.AddControllers();
            services.AddSwaggerGen(Options =>
            {
                Options.SwaggerDoc("v1", new OpenApiInfo { Title = "Ordering.API", Version = "v1" });

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
                                {"ordering.api", "ordering management service"}
                           }
                        }
                    }
                });

                Options.OperationFilter<AuthorizeCheckOperationFilter>();
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
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Order.API v1");
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
