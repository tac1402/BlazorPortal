using BlazorWorld.Core.Constants;
using BlazorWorld.Core.Entities.Configuration;
using BlazorWorld.Data.Identity;
using BlazorWorld.Services.Configuration;
using BlazorWorld.Services.Configuration.Models;
using BlazorWorld.Services.Content;
using BlazorWorld.Services.Organization;
using BlazorWorld.Services.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace BlazorWorld.Services
{
    public static class Extensions
    {
        public static void AddBlazorWorldServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<ISettingService, SettingService>();

            // security
            services.AddTransient<ISecurityService, SecurityService>();
            services.AddTransient<IInvitationService, InvitationService>();

            // content
            services.AddTransient<IActivityService, ActivityService>();
            services.AddTransient<INodeService, NodeService>();
            services.AddTransient<IMessageService, MessageService>();
            services.AddTransient<IProfileService, ProfileService>();
            services.AddTransient<IUserService, UserService>();

            // organization
            services.AddTransient<IGroupService, GroupService>();


            services.Configure<AuthMessageSenderOptions>(configuration);
        }

        public static void UseBlazorWorldServices(this IServiceProvider serviceProvider, IConfiguration configuration)
        {
            using (IServiceScope scope = serviceProvider.CreateScope())
            {
				ISettingService settingService = scope.ServiceProvider.GetRequiredService<ISettingService>();
				LoadSettingsAsync(settingService, configuration).Wait();

				CreateUserRolesAsync(scope).Wait();
            }
        }

        private static async Task LoadSettingsAsync(ISettingService settingService, IConfiguration configuration)
        {
            //var settingService = serviceProvider.GetRequiredService<ISettingService>();
            await settingService.LoadSettingsAsync(configuration);
        }

        private static async Task CreateUserRolesAsync(IServiceScope scope)
        {
			UserManager<ApplicationUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
			RoleManager<IdentityRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
			ISettingService configurationService = scope.ServiceProvider.GetRequiredService<ISettingService>();

			foreach (var role in Roles.All)
            {
                IdentityResult roleResult;
                var roleCheck = await roleManager.RoleExistsAsync(role);
                if (!roleCheck)
                {
                    roleResult = await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var roleUsersArray = await configurationService.RoleUserSettingsAsync();
            foreach (var roleUserSettings in roleUsersArray)
            {
                var roleUsers = new RoleUsers(roleUserSettings);
                foreach (var userName in roleUsers.Users)
                {
                    ApplicationUser user = await userManager.FindByNameAsync(userName);
                    if (user != null)
                    {
                        var inRole = await userManager.IsInRoleAsync(user, roleUsers.Role);
                        if (!inRole)
                            await userManager.AddToRoleAsync(user, roleUsers.Role);
                    }
                }
            }
        }
    }
}
