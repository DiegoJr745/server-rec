using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using sscs2023.Classes.DBs;
using static sscs2023.Classes.DBs.DBClasses.PlayerDBClasses;

namespace sscs2023.Auth
{
    public static class AuthStuff
    {
        private static readonly string Secret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "sscs_default_secret_key_must_be_over_32_chars_long";
        private static readonly string Issuer = "sscs";

        public static string Encode(long accountId)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            
            var roles = new List<string>
            {
                "developer"
            };

            var claims = new[]
            {
                new Claim("sub", accountId.ToString()),
                new Claim("role", JsonSerializer.Serialize(roles), JsonClaimValueTypes.JsonArray),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var token = new JwtSecurityToken(
                issuer: Issuer,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(30),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static long? GetPlayerId(HttpRequest request)
        {
            var auth = request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(auth) || !auth.StartsWith("Bearer "))
                return null;

            var tokenStr = auth.Substring("Bearer ".Length).Trim();

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));

                handler.ValidateToken(tokenStr, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = Issuer,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out var validated);

                var jwt = (JwtSecurityToken)validated;
                var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

                return sub != null ? long.Parse(sub) : null;
            }
            catch
            {
                return null;
            }
        }
        
        public static FullPlayer? GetCurrentPlayer(HttpRequest request)
        {
            var id = GetPlayerId(request);
            return id.HasValue ? PlayerDB.Players.FindById(id.Value) : null;
        }
    }
}