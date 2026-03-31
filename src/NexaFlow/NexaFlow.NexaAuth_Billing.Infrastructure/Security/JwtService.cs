using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using NexaFlow.NexaAuth_Billing.Application.Dto;
using NexaFlow.NexaAuth_Billing.Application.Interfaces.Services;

namespace NexaFlow.NexaAuth_Billing.Infrastructure.Security;

public class JwtService : IJwtService
{
    private readonly string _secret;
    private readonly string _issuer;
    private readonly int _expiresInSeconds;

    public JwtService(string secret, string issuer, int expiresInSeconds = 3600)
    {
        _secret = secret;
        _issuer = issuer;
        _expiresInSeconds = expiresInSeconds;
    }

    public AuthTokenDto GenerateToken(Guid userId, Guid tenantId, string email, string role, string name)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim("tenant_id", tenantId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Name, name),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _issuer,
            claims: claims,
            expires: DateTime.UtcNow.AddSeconds(_expiresInSeconds),
            signingCredentials: creds);

        return new AuthTokenDto(
            new JwtSecurityTokenHandler().WriteToken(token),
            "Bearer",
            _expiresInSeconds,
            userId,
            tenantId,
            role);
    }
}
