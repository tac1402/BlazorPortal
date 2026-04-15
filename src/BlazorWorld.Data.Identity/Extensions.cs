using BlazorWorld.Data.Identity.DbContexts;
using BlazorWorld.Data.Identity.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Security.Claims;


namespace BlazorWorld.Data.Identity
{
    public static class Extensions
    {
        public static void AddBlazorWorldIdentity(this IServiceCollection services, IConfiguration configuration)
        {
		string connectionString = configuration.GetConnectionString("IdentityDbConnection");

		services.AddDbContext<AppIdentityDbContext>(options => options.UseSqlServer(connectionString), ServiceLifetime.Transient);
		services.AddDbContext<SqlServerIdentityDbContext>(options => options.UseSqlServer(connectionString), ServiceLifetime.Transient);
	}

        public static void UpdateBlazorWorldIdentityDatabase(this IApplicationBuilder app, IConfiguration configuration)
        {
		app.ProcessDb<SqlServerIdentityDbContext>();
        }

        public static void UseBlazorWorldIdentity(this IApplicationBuilder app)
        {
            app.UseIdentityServer();
            app.UseAuthentication();
            app.UseAuthorization();
        }

        private static void ProcessDb<T>(this IApplicationBuilder app) where T : AppIdentityDbContext
        {
            using var serviceScope = app.ApplicationServices
                .GetRequiredService<IServiceScopeFactory>()
                .CreateScope();
            using var context = serviceScope.ServiceProvider.GetService<T>();
            context.Database.Migrate();
        }

        public static void AddBlazorWorldIdentityRepositories(this IServiceCollection services)
        {
            services.AddTransient<IUserRepository, UserRepository>();
        }


    }
}
