using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Test;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IdentityServer
{
    public class Config
    {
        //ICollection<string> AllowedGrantTypes = new Collection<string>();
        //AllowedGrantTypes.Add(GrantTypes.Hybrid);
        //AllowedGrantTypes.Add(GrantTypes.Code);
    
    public static IEnumerable<Client> Clients =>
            new Client[]
            {
                   //new Client
                   //{
                   //     ClientId = "movieClient",
                   //     AllowedGrantTypes = GrantTypes.ClientCredentials,
                   //     ClientSecrets =
                   //     {
                   //         new Secret("secret".Sha256())
                   //     },
                   //     AllowedScopes = { "movieAPI" }
                   //},
                   new Client
                   {
                        ClientId = Startup.Configurations["IdentityServerSettings:ClientId"],
                        ClientName = Startup.Configurations["IdentityServerSettings:ClientName"],
                        AllowedGrantTypes = { GrantType.AuthorizationCode,  //GrantType.Hybrid,  
                           GrantType.ClientCredentials }, // { GrantTypes.CodeAndClientCredentials ,//Hybrid  normal, //new List<string>(){ GrantTypes.Code //swagger,  },
                        RequirePkce = true, //false, //,true swagger
                        //RequireClientSecret=false, // entire line swagger
                        AllowRememberConsent = false,

                        RedirectUris = new List<string>()
                        {
                            Startup.Configurations["IdentityServerSettings:RedirectUris"],
                            "https://localhost:5000/swagger/oauth2-redirect.html",
                            "http://localhost:5001/swagger/oauth2-redirect.html",
                            "http://localhost:5004/swagger/oauth2-redirect.html",
                            "https://oauth.pstmn.io/v1/callback"
                        },
                        PostLogoutRedirectUris = new List<string>()
                        {
                            Startup.Configurations["IdentityServerSettings:PostLogoutRedirectUris"]
                        },
   
                        AllowedCorsOrigins = {"https://localhost:5000", "http://localhost:5001", "http://localhost:5004", "https://localhost:5012" },

                        ClientSecrets = new List<Secret>
                        {
                            new Secret(Startup.Configurations["IdentityServerSettings:ClientSecret"].Sha256())
                        },
                        AllowedScopes = new List<string>
                        {
                            IdentityServerConstants.StandardScopes.OpenId,
                            IdentityServerConstants.StandardScopes.Profile,
                            IdentityServerConstants.StandardScopes.Address,
                            IdentityServerConstants.StandardScopes.Email,
                            Startup.Configurations["IdentityServerSettings:AllowedScope1"],
                            Startup.Configurations["IdentityServerSettings:AllowedScope2"],
                            Startup.Configurations["IdentityServerSettings:AllowedScope3"],
                            "roles"
                        }
                   }
            };

        public static IEnumerable<ApiScope> ApiScopes =>
           new ApiScope[]
           {
               new ApiScope(Startup.Configurations["IdentityServerSettings:AllowedScope1"], Startup.Configurations["IdentityServerSettings:AllowedScope1Description"]),
               new ApiScope(Startup.Configurations["IdentityServerSettings:AllowedScope2"], Startup.Configurations["IdentityServerSettings:AllowedScope2Description"]),
               new ApiScope(Startup.Configurations["IdentityServerSettings:AllowedScope3"], Startup.Configurations["IdentityServerSettings:AllowedScope3Description"])
           };

        public static IEnumerable<ApiResource> ApiResources =>
          new ApiResource[]
          {
               new ApiResource(Startup.Configurations["IdentityServerSettings:AllowedScope1"], Startup.Configurations["IdentityServerSettings:AllowedScope1Description"])
               {
                   Scopes = { Startup.Configurations["IdentityServerSettings:AllowedScope1"] }
               },
               new ApiResource(Startup.Configurations["IdentityServerSettings:AllowedScope2"], Startup.Configurations["IdentityServerSettings:AllowedScope2Description"])
               {
                   Scopes = { Startup.Configurations["IdentityServerSettings:AllowedScope2"] }
               },
               new ApiResource(Startup.Configurations["IdentityServerSettings:AllowedScope3"], Startup.Configurations["IdentityServerSettings:AllowedScope3Description"])
               {
                   Scopes = { Startup.Configurations["IdentityServerSettings:AllowedScope3"] }
               }
          };

        public static IEnumerable<IdentityResource> IdentityResources =>
          new IdentityResource[]
          {
              new IdentityResources.OpenId(),
              new IdentityResources.Profile()
              //,
              //new IdentityResources.Address(),
              //new IdentityResources.Email(),
              //new IdentityResource(
              //      "roles",
              //      "Your role(s)",
              //      new List<string>() { "role" })
          };

        public static List<TestUser> TestUsers =>
            new List<TestUser>
            {
                new TestUser
                {
                    SubjectId = "5BE86359-073C-434B-AD2D-A3932222DABE",
                    Username = "Karikalan",
                    Password = "Sbsl@kpro1112000",
                    Claims = new List<Claim>
                    {
                        new Claim(JwtClaimTypes.GivenName, "Karikalan"),
                        new Claim(JwtClaimTypes.FamilyName, "P")
                    }
                }
            };
    }
}
