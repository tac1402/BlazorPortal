using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using DuendeClient = Duende.IdentityServer.Models.Client;

namespace BlazorWorld.Web.Server
{
	public static class Config
	{
		// 1. Клиенты (ваше Blazor-приложение)
		public static IEnumerable<DuendeClient> GetClients() =>
			new List<DuendeClient>
			{
			new DuendeClient
			{
				ClientId = "BlazorWorld.Web.Client",
				AllowedGrantTypes = GrantTypes.Code,
				RequirePkce = true,
				RequireClientSecret = false,
				RedirectUris = { "https://localhost:44356/authentication/login-callback" },
				PostLogoutRedirectUris = { "https://localhost:44356/" },
				AllowedScopes = new List<string>
				{
					IdentityServerConstants.StandardScopes.OpenId,
					IdentityServerConstants.StandardScopes.Profile,
				},
				AllowAccessTokensViaBrowser = true,
				RequireConsent = false,
			}
			};

		// 2. Identity ресурсы (данные о пользователе)
		public static IEnumerable<IdentityResource> GetIdentityResources() =>
			new List<IdentityResource>
			{
			new IdentityResources.OpenId(),
			new IdentityResources.Profile(),
			};

		// 3. Api Scope (если у вас есть защищённые API, добавьте их сюда)
		public static IEnumerable<ApiScope> GetApiScopes() =>
			new List<ApiScope>
			{
				// Пример: new ApiScope("api1", "My API")
			};
	}
}
