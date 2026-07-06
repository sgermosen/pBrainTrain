using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BrainTrain.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BrainTrain.Api.Services;

public sealed class TokenService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _opt = options.Value;

    public string CreateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("name", user.DisplayName),
                new Claim("guest", user.IsGuest ? "1" : "0")
            ],
            expires: DateTime.UtcNow.AddMinutes(_opt.AccessTokenMinutes),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public int AccessTokenSeconds => _opt.AccessTokenMinutes * 60;

    /// <summary>Genera un refresh token aleatorio; solo su SHA-256 se persiste.</summary>
    public (string Plain, RefreshToken Entity) CreateRefreshToken(long userId)
    {
        var bytes = RandomNumberGenerator.GetBytes(48);
        var plain = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var entity = new RefreshToken
        {
            UserId = userId,
            TokenHash = Hash(plain),
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_opt.RefreshTokenDays)
        };
        return (plain, entity);
    }

    public static string Hash(string token) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
