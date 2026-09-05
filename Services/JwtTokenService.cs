using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace patient_api.Services;

public class JwtTokenService(IConfiguration config)
{
    public string CreateAccessToken(int patientId, bool mustChangePassword)
    {
        var signingKey = config["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");

        var claims = new[]
        {
            new Claim("patient_id", patientId.ToString()),
            new Claim("must_change_password", mustChangePassword.ToString().ToLowerInvariant()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var minutes = config.GetValue("Jwt:AccessTokenMinutes", 45);

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(minutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
