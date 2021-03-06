﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System.IdentityModel.Tokens.Jwt;
using IdentityServer4.AccessTokenValidation;
using Microsoft.IdentityModel.Logging;
using System.Collections.Generic;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace WebApi
{
    public class Startup
    {
        private IConfiguration _configuration { get; }
        private IWebHostEnvironment _environment { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _environment = env;
        }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var x509Certificate2 = GetCertificate(_environment);

            services.AddSingleton<IAuthorizationHandler, MyApiHandler>();

            services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                .AddJwtBearer("SchemeStsA", options =>
                {
                    options.Audience = "ProtectedApiResourceA";
                    options.Authority = "https://localhost:44318";
                })
                .AddJwtBearer("SchemeStsB", options =>
                {
                    options.Audience = "ProtectedApiResourceB";
                    options.Authority = "https://localhost:44367";
                });

            //.AddJwtBearer(options =>
            //{
            //    options.Audience = "ProtectedApiResource";
            //    options.TokenValidationParameters = new TokenValidationParameters
            //    {
            //        ValidateIssuer = true,
            //        ValidIssuers = new List<string> { "https://localhost:44318", "https://localhost:44367" },
            //        ValidateIssuerSigningKey = true,
            //        IssuerSigningKey = new X509SecurityKey(x509Certificate2),
            //        IssuerSigningKeyResolver = 
            //        (string token, SecurityToken securityToken, string kid, TokenValidationParameters validationParameters)
            //          => new List<X509SecurityKey> { new X509SecurityKey(x509Certificate2) }
            //    };
            //});

            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes("SchemeStsA", "SchemeStsB")
                    .Build();

                options.AddPolicy("MyPolicy", policy =>
                {
                    policy.AddRequirements(new MyApiRequirement());
                });
            });

            //services.AddAuthorization(options =>
            //    options.AddPolicy("protectedScope", policy =>
            //    {
            //        policy.RequireClaim("scope", "scope_used_for_api_in_protected_zone");
            //    })
            //);

            services.AddControllers();

            services.AddSwaggerGen(c =>
            {
                // add JWT Authentication
                var securityScheme = new OpenApiSecurityScheme
                {
                    Name = "JWT Authentication",
                    Description = "Enter JWT Bearer token **_only_**",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer", // must be lower case
                    BearerFormat = "JWT",
                    Reference = new OpenApiReference
                    {
                        Id = JwtBearerDefaults.AuthenticationScheme,
                        Type = ReferenceType.SecurityScheme
                    }
                };
                c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {securityScheme, new string[] { }}
                });

                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "An API ",
                    Version = "v1",
                    Description = "An API",
                    Contact = new OpenApiContact
                    {
                        Name = "damienbod",
                        Email = string.Empty,
                        Url = new Uri("https://damienbod.com/"),
                    },
                });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            IdentityModelEventSource.ShowPII = true;
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Service API One");
                c.RoutePrefix = string.Empty;
            });

            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private static X509Certificate2 GetCertificate(IWebHostEnvironment environment)
        {
            X509Certificate2 cert;

            cert = new X509Certificate2(Path.Combine(environment.ContentRootPath, "sts_dev_cert.pfx"), "1234");

            return cert;
        }

    }
}
