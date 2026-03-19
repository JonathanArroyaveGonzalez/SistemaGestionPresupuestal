using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SAPFIAI.Application.Common.Interfaces;

namespace SAPFIAI.Infrastructure.Services;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;
    private const string TwoFactorPendingClaim = "2fa_pending";

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(string userId, string email, bool requiresTwoFactorVerification = false)
    {
        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key is not configured. Set the 'Jwt:Key' configuration value.");
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? "SAPFIAI";
        var jwtAudience = _configuration["Jwt:Audience"] ?? "SAPFIAI-Users";
        var jwtExpireMinutes = int.Parse(_configuration["Jwt:ExpireMinutes"] ?? "60");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new(TwoFactorPendingClaim, requiresTwoFactorVerification.ToString().ToLower())
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(jwtExpireMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public bool RequiresTwoFactorVerification(string token)
    {
        var principal = ValidateAndGetPrincipal(token);
        var claim = principal?.FindFirst(TwoFactorPendingClaim);
        return claim?.Value == "true";
    }

    public string? GetUserIdFromToken(string token)
    {
        var principal = ValidateAndGetPrincipal(token);
        return principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public bool ValidateToken(string token)
    {
        return ValidateAndGetPrincipal(token) != null;
    }

    public string? GetEmailFromToken(string token)
    {
        var principal = ValidateAndGetPrincipal(token);
        return principal?.FindFirst(JwtRegisteredClaimNames.Email)?.Value
            ?? principal?.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Validates the token signature, issuer, audience, and lifetime, returning the ClaimsPrincipal if valid.
    /// </summary>
    private ClaimsPrincipal? ValidateAndGetPrincipal(string token)
    {
        try
        {
            var jwtKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT Key is not configured.");
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "SAPFIAI";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "SAPFIAI-Users";

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtKey);

            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }
}


