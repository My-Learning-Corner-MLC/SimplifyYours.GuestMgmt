using Microsoft.IdentityModel.Tokens;
using OpenIddict.Validation.AspNetCore;

namespace GuestManagementService.Api.Security;

internal static class AuthenticationExtensions
{
    public static IServiceCollection AddApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var issuer = configuration["Auth:Issuer"];
        var audience = configuration["Auth:Audience"];
        var encryptionKey = configuration["Auth:AccessTokenEncryptionKeyBase64"];

        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new InvalidOperationException("Configuration value 'Auth:Issuer' is required.");
        }

        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException("Configuration value 'Auth:Audience' is required.");
        }

        if (string.IsNullOrWhiteSpace(encryptionKey))
        {
            throw new InvalidOperationException("Configuration value 'Auth:AccessTokenEncryptionKeyBase64' is required.");
        }

        services.AddOpenIddict()
            .AddValidation(options =>
            {
                options.SetIssuer(new Uri(issuer, UriKind.Absolute));
                options.AddAudiences(audience);
                options.AddEncryptionKey(new SymmetricSecurityKey(Convert.FromBase64String(encryptionKey)));
                options.UseSystemNetHttp();
                options.UseAspNetCore();
            });

        services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        services.AddAuthorization();

        return services;
    }
}
