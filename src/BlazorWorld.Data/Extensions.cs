using BlazorWorld.Core.Repositories;
using BlazorWorld.Data.DbContexts;
using BlazorWorld.Data.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BlazorWorld.Data
{
    public static class Extensions
    {
        public static void AddBlazorWorldDataProvider(this IServiceCollection services, IConfiguration configuration)
        {
            string connectionString = configuration.GetConnectionString("AppDbConnection");

			services.AddDbContext<AppDbContext>(options =>
				options.UseSqlServer(connectionString), ServiceLifetime.Transient);
			services.AddDbContext<SqlServerDbContext>(options =>
				options.UseSqlServer(connectionString), ServiceLifetime.Transient);

        }

        public static void UpdateBlazorWorldDatabase(this IApplicationBuilder app, IConfiguration configuration)
        {
			app.ProcessDb<SqlServerDbContext>();
        }

        private static void ProcessDb<T>(this IApplicationBuilder app) where T : AppDbContext
        {
            using var serviceScope = app.ApplicationServices
                .GetRequiredService<IServiceScopeFactory>()
                .CreateScope();
            using var context = serviceScope.ServiceProvider.GetService<T>();
            context.Database.Migrate();
        }

        public static void AddBlazorWorldApplicationRepositories(this IServiceCollection services)
        {
            services.AddTransient<IActivityRepository, ActivityRepository>();
            services.AddTransient<IEmailRepository, EmailRepository>();
            services.AddTransient<IInvitationRepository, InvitationRepository>();
            services.AddTransient<IMessageRepository, MessageRepository>();
            services.AddTransient<INodeRepository, NodeRepository>();
            services.AddTransient<ISettingRepository, SettingRepository>();
        }
    }
}
