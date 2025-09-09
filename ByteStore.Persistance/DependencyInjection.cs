﻿using ByteStore.Domain.Entities;
using ByteStore.Domain.Repositories;
using ByteStore.Persistance.Database;
using ByteStore.Persistance.Repositories;
using ByteStore.Persistance.Services;
using BytStore.Application.Helpers;
using BytStore.Application.IServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;

namespace ByteStore.Persistance
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPersistanceLayer(this IServiceCollection services,IConfiguration configuration)
        {

            #region EFCore
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration));
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
            });
            // shortcut
            //services.AddNpgsql<AppDbContext>((configuration.GetConnectionString("DefaultConnection")));
            #endregion

            #region Redis
            var redisConnection = configuration.GetConnectionString("Redis")
                 ?? throw new ArgumentNullException(nameof(configuration));
            services.AddSingleton<IConnectionMultiplexer>((serviceProvider) =>
            {
                return ConnectionMultiplexer.Connect(redisConnection);
            });
            #endregion

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IShoppingCartRepository, ShoppingCartRepository>();
            services.AddScoped<ITokenGenerator, TokenGenerator>();
            services.AddScoped<IImageService, ImageService>();
            #region JWTConfigs
            //(1)
            // identity ===> i spend one day to find out that you are the problem => it should be above JWTConfigs :(
            services.AddIdentity<AppUser, AppRole>().AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();


            // JWTHelper (2)
            services.Configure<JWT>(configuration.GetSection("JWT"));

            // (3)
            // to use jwt token to check authantication =>[authorize]
            services.AddAuthentication(options =>
            {
                // to change default authantication to jwt 
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;

                //  if u are unauthanticated it will redirect you to login form
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

                // if there other schemas make is default of jwt
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;


                // these configs to check if has token only but i want to check if he has right claims
            }).AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false;

                // check if token have specific data
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = configuration["JWT:Issuer"],
                    ValidAudience = configuration["JWT:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:SecretKey"])),

                    // if u want when the token expire he does not give me مهله بعض الوقت 
                    ClockSkew = TimeSpan.Zero

                };
            }

                         );
            #endregion



            return services;
        }
    }
}
