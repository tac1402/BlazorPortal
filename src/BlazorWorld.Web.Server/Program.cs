using BlazorWorld.Data;
using BlazorWorld.Data.Identity;
using BlazorWorld.Data.Identity.DbContexts;
using BlazorWorld.Services;
using BlazorWorld.Services.Security;
using BlazorWorld.Web.Server.Messages.Services;
using BlazorWorld.Web.Server.Services;
using BlazorWorld.Web.Server.Services.Hubs;
using BlazorWorld.Web.Shared;
using BlazorWorld.Web.Shared.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.ResponseCaching;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using System.Security.Claims;

namespace BlazorWorld.Web.Server
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
			var builder = WebApplication.CreateBuilder(args);

			// ========== НАСТРОЙКА КОНФИГУРАЦИИ ==========
			builder.Configuration.Sources.Clear();
			var env = builder.Environment;
			builder.Configuration
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
				.AddJsonFile("Settings/content-appsettings.json", optional: true, reloadOnChange: true)
				.AddJsonFile($"Settings/content-appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
				.AddJsonFile("Settings/modules-appsettings.json", optional: true, reloadOnChange: true)
				.AddJsonFile($"Settings/modules-appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
				.AddJsonFile("Settings/security-appsettings.json", optional: true, reloadOnChange: true)
				.AddJsonFile($"Settings/security-appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
				.AddJsonFile("Settings/site-appsettings.json", optional: true, reloadOnChange: true)
				.AddJsonFile($"Settings/site-appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
				.AddEnvironmentVariables();
			if (args != null) builder.Configuration.AddCommandLine(args);

			// ========== КОНФИГУРАЦИЯ СЕРВИСОВ ==========

			// --- Старые кастомные методы ---
			builder.Services.AddBlazorWorldIdentity(builder.Configuration);
			builder.Services.AddBlazorWorldDataProvider(builder.Configuration);

			// --- Настройка IdentityOptions (ClaimTypes) ---
			builder.Services.Configure<IdentityOptions>(options =>
				options.ClaimsIdentity.UserIdClaimType = ClaimTypes.NameIdentifier
				);

			// --- HttpClient ---
			builder.Services.AddHttpClient();

			// --- Razor Pages ---
			builder.Services.Configure<RazorPagesOptions>(options => options.RootDirectory = "/Pages");

			// --- Scoped сервисы аутентификации для Blazor Server ---
			builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
			builder.Services.AddScoped<SignOutSessionStateManager>();

			// --- Репозитории и сервисы вашего приложения ---
			builder.Services.AddBlazorWorldIdentityRepositories();
			builder.Services.AddBlazorWorldApplicationRepositories();
			builder.Services.AddBlazorWorldServices(builder.Configuration);
			builder.Services.AddTransient<IWebHubClientService, ServerHubClientService>();
			builder.Services.AddTransient<IWebMessageService, ServerMessageService>();
			builder.Services.AddBlazorWorldWebServerServices();

			// --- SignalR ---
			builder.Services.AddSignalR();

			// --- MVC и Views ---
			builder.Services.AddControllersWithViews();

			// *** ИЗМЕНЕНИЕ 1: Удалён AddApiAuthorization(), заменён на новый Identity API ***
			// Было: services.AddApiAuthorization();
			// Стало:
			//  1) Регистрируем Identity с EntityFramework
			//  2) Добавляем аутентификацию через Bearer token (JWT)
			//  3) Добавляем готовые Identity API endpoints
			//  4) Добавляем авторизацию (стандартная)

			/*builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
				.AddEntityFrameworkStores<AppIdentityDbContext>()
				.AddDefaultTokenProviders();*/


			builder.Services.AddIdentityApiEndpoints<ApplicationUser>(options =>
			{
				options.SignIn.RequireConfirmedAccount = true;
			})
			.AddRoles<IdentityRole>()
			.AddEntityFrameworkStores<AppIdentityDbContext>();

			builder.Services.AddTransient<IEmailSender<ApplicationUser>, EmailSender<ApplicationUser>>();
			builder.Services.AddTransient<IAppEmailSender<ApplicationUser>, EmailSender<ApplicationUser>>();

			// Добавляем IdentityServer
			builder.Services.AddIdentityServer(options => 
				{
					options.UserInteraction.LoginUrl = "/Identity/Account/Login";
					//options.UserInteraction.LoginReturnUrlParameter = "ReturnUrl";
				})
				.AddAspNetIdentity<ApplicationUser>()           // связывает с вашими пользователями
				.AddInMemoryClients(Config.GetClients())
				.AddInMemoryApiScopes(Config.GetApiScopes())
				.AddInMemoryIdentityResources(Config.GetIdentityResources())
				.AddDeveloperSigningCredential();

			builder.Services.AddAuthorization();

			// --- Response Compression (для SignalR) ---
			builder.Services.AddResponseCompression(opts =>
			{
				opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
					new[] { "application/octet-stream" });
			});

			// --- Database Developer Page Exception Filter (только для разработки) ---
			builder.Services.AddDatabaseDeveloperPageExceptionFilter();

			// --- Razor Pages ---
			builder.Services.AddRazorPages();

			// --- Локализация ---
			builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

			// --- Response Caching ---
			builder.Services.AddResponseCaching();

			// --- MVC с локализацией ---
			builder.Services.AddMvc()
				.AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
				.AddDataAnnotationsLocalization();

			// ========== ПОСТРОЕНИЕ ПРИЛОЖЕНИЯ ==========
			var app = builder.Build();

			// ========== КОНФИГУРАЦИЯ PIPELINE (вместо Configure) ==========

			app.Use(async (context, next) =>
			{
				await next(); // сначала выполнить остальной конвейер

				if (context.Response.StatusCode == 400)
				{
					// Здесь можно поставить точку останова в Visual Studio
					//System.Diagnostics.Debugger.Break();

					// Дополнительно можно вывести информацию в консоль
					Console.WriteLine($"=== 400 ERROR ===");
					Console.WriteLine($"Path: {context.Request.Path}{context.Request.QueryString}");
					Console.WriteLine($"Method: {context.Request.Method}");
					Console.WriteLine($"Headers: {string.Join(", ", context.Request.Headers.Select(h => $"{h.Key}={h.Value}"))}");
					Console.WriteLine($"Response body is {(context.Response.Body.CanSeek ? "available" : "not available")}");
				}
			});

			// --- Response Compression (должен быть первым) ---
			app.UseResponseCompression();

			// --- Обработка ошибок в зависимости от окружения ---
			if (app.Environment.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
				app.UseMigrationsEndPoint();
				app.UseWebAssemblyDebugging();
			}
			else
			{
				app.UseExceptionHandler("/Error");
				app.UseHsts();
			}

			// --- HTTPS, Blazor framework files, статические файлы ---
			app.UseHttpsRedirection();
			app.UseBlazorFrameworkFiles();
			app.UseStaticFiles();

			// --- Локализация (культуры) ---
			var supportedCultures = new[] { "en-US", "fr" };
			var localizationOptions = new RequestLocalizationOptions()
				.SetDefaultCulture(supportedCultures[0])
				.AddSupportedCultures(supportedCultures)
				.AddSupportedUICultures(supportedCultures);
			app.UseRequestLocalization(localizationOptions);

			// --- Routing ---
			app.UseRouting();

			// --- Пользовательская middleware для изменения Scheme/Host (без изменений) ---
			app.Use(async (ctx, next) =>
			{
				ctx.Request.Scheme = builder.Configuration["SiteScheme"];
				ctx.Request.Host = new HostString(builder.Configuration["SiteUrl"]);
				await next();
			});

			// *** ИЗМЕНЕНИЕ 2: UseIdentityServer() удалён, заменён на стандартные аутентификацию/авторизацию ***
			// Было: app.UseIdentityServer();
			app.UseIdentityServer();

			// Стало: ниже идут UseAuthentication, UseAuthorization (IdentityServer больше не нужен)
			app.UseAuthentication();
			app.UseAuthorization();

			// --- Ваши кастомные методы обновления БД и использования сервисов ---
			app.UpdateBlazorWorldDatabase(builder.Configuration);
			app.UpdateBlazorWorldIdentityDatabase(builder.Configuration);
			// Внимание: services был IServiceProvider из параметра Configure. Теперь используем app.Services
			app.Services.UseBlazorWorldServices(builder.Configuration);

			// --- Response Caching middleware ---
			app.UseResponseCaching();

			// --- Кастомная middleware для управления кэшем (без изменений) ---
			app.Use(async (context, next) =>
			{
				context.Response.GetTypedHeaders().CacheControl =
					new CacheControlHeaderValue()
					{
						Public = true,
						MaxAge = TimeSpan.FromSeconds(10)
					};
				var responseCachingFeature = context.Features.Get<IResponseCachingFeature>();
				if (responseCachingFeature != null)
				{
					responseCachingFeature.VaryByQueryKeys = new[] { "*" };
				}
				context.Response.Headers[HeaderNames.Vary] = new[] { "Accept-Encoding" };
				await next();
			});

			// --- Endpoints (маршруты) ---
			app.UseEndpoints(endpoints =>
			{
				endpoints.MapAreaControllerRoute(
					name: "Identity",
					areaName: "Identity",
					pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
				endpoints.MapDefaultControllerRoute();
				endpoints.MapRazorPages();
				endpoints.MapControllers();
				endpoints.MapHub<MessagesHub>(Constants.MessagesHubPattern);
				endpoints.MapFallbackToPage("/_Host");
			});



			// *** ИЗМЕНЕНИЕ 3: Добавлены новые Identity API endpoints ***
			// Эти эндпоинты заменяют старый OidcConfigurationController
			app.MapIdentityApi<ApplicationUser>();

			// Запуск
			await app.RunAsync();
		}

    }
}
