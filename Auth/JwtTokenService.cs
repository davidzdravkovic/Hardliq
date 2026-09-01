using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Models;

namespace TaskManager.Auth;

public class JwtTokenService
{
    private readonly JwtOptions _jwt;
    private readonly SymmetricSecurityKey _key;
    private readonly SigningCredentials _credentials;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _jwt = options.Value;

        _key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwt.Key));

        _credentials = new SigningCredentials(
            _key,
            SecurityAlgorithms.HmacSha256);
    }

    public string CreateToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        };

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwt.ExpiresMinutes),
            signingCredentials: _credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}