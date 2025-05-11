using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace E_Dukate.Application.Services.Auth;
public class JwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(Guid userId, string role, string name)
    {
        var keyValue = _configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(keyValue))
            throw new InvalidOperationException("JWT Key is not configured.");

        var expirationValue = _configuration["Jwt:TokenExpirationInMinutes"];
        if (!int.TryParse(expirationValue, out var expirationMinutes))
            throw new InvalidOperationException("JWT TokenExpirationInMinutes is not configured or invalid.");

        var claims = new[]
        {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Name, name)
            };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyValue));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(expirationMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
