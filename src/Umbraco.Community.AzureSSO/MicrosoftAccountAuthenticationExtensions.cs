using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Umbraco.Community.AzureSSO;
using Umbraco.Community.AzureSSO.Settings;
using Umbraco.Extensions;

namespace Umbraco.Cms.Core.DependencyInjection
{
	public static class MicrosoftAccountAuthenticationExtensions
	{
		public static IUmbracoBuilder AddMicrosoftAccountAuthentication(this IUmbracoBuilder builder)
		{
			var azureSsoConfiguration = new AzureSSOConfiguration();
			builder.Config.Bind(AzureSSOConfiguration.AzureSsoSectionName, azureSsoConfiguration);

			builder.Services.AddSingleton<AzureSsoSettings>(conf => new AzureSsoSettings(azureSsoConfiguration));
			builder.Services.ConfigureOptions<MicrosoftAccountBackOfficeExternalLoginProviderOptions>();
			
			var initialScopes = Array.Empty<string>();
			builder.AddBackOfficeExternalLogins(logins =>
			{
				logins.AddBackOfficeLogin(
					backOfficeAuthenticationBuilder =>
					{
						backOfficeAuthenticationBuilder.AddMicrosoftIdentityWebApp(options =>
								{
									builder.Config.Bind(AzureSSOConfiguration.AzureSsoCredentialSectionName, options);
									options.SignInScheme = backOfficeAuthenticationBuilder.SchemeForBackOffice(MicrosoftAccountBackOfficeExternalLoginProviderOptions.SchemeName);
									options.Events = new OpenIdConnectEvents

										{
										OnRedirectToIdentityProvider = async ctxt =>
										{

											ctxt.ProtocolMessage.RedirectUri = azureSsoConfiguration.RedirectUriOverride;
											await Task.Yield();
										}

										};
								},
								options => { builder.Config.Bind(AzureSSOConfiguration.AzureSsoCredentialSectionName, options); },
								displayName: azureSsoConfiguration.DisplayName ?? "Azure Active Directory",
								openIdConnectScheme: backOfficeAuthenticationBuilder.SchemeForBackOffice(MicrosoftAccountBackOfficeExternalLoginProviderOptions.SchemeName) ?? String.Empty)
							.EnableTokenAcquisitionToCallDownstreamApi(options => builder.Config.Bind(AzureSSOConfiguration.AzureSsoCredentialSectionName, options), initialScopes)
							.AddTokenCaches(azureSsoConfiguration.TokenCacheType);
					});
			});
			return builder;
		}

		private static MicrosoftIdentityAppCallsWebApiAuthenticationBuilder AddTokenCaches(this MicrosoftIdentityAppCallsWebApiAuthenticationBuilder builder, TokenCacheType tokenCacheType)
		{
			switch (tokenCacheType)
			{
				case TokenCacheType.InMemory:
					builder.AddInMemoryTokenCaches();
					break;
				case TokenCacheType.Session:
					builder.AddSessionTokenCaches();
					break;
				case TokenCacheType.Distributed:
					builder.AddDistributedTokenCaches();
					break;
				default:
					builder.AddInMemoryTokenCaches();
					break;
			}

			return builder;
		}

	}
}
