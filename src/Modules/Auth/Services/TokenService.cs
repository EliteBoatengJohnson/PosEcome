using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PosSystem.Modules.Auth.Entities;

namespace PosSystem.Modules.Auth.Services;

// ─────────────────────────────────────────────────────────────────────
// TokenService — generates and validates JWT access tokens + refresh tokens.
//
// It reads the JWT config from appsettings.json:
//   "Jwt": {
//     "Issuer":            "pos-system",
//     "Audience":          "pos-client",
//     "SecretKey":         "pos-secret-key224lTvp88674pxl5$%3",
//     "ExpiryMinutes":     480,
//     "RefreshExpiryDays": 30
//   }
//
// Flow:
//   1. AuthService validates email/password
//   2. AuthService calls TokenService.GenerateAccessToken(user) → JWT string
//   3. AuthService calls TokenService.GenerateRefreshToken() → random Base64 string
//   4. Both tokens are returned to the client in LoginResponse
//   5. On refresh: client sends the refresh token, AuthService validates it,
//      then calls GenerateAccessToken again for a new JWT
// ─────────────────────────────────────────────────────────────────────
public class TokenService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly byte[] _secretKeyBytes;
    private readonly int _expiryMinutes;
    private readonly int _refreshExpiryDays;

    public TokenService(IConfiguration configuration)
    {
        // Read JWT settings from appsettings.json → "Jwt" section
        var jwtSection = configuration.GetSection("Jwt");

        _issuer          = jwtSection["Issuer"]    ?? throw new InvalidOperationException("Jwt:Issuer is missing");
        _audience        = jwtSection["Audience"]  ?? throw new InvalidOperationException("Jwt:Audience is missing");
        var secretKey    = jwtSection["SecretKey"]  ?? throw new InvalidOperationException("Jwt:SecretKey is missing");
        _expiryMinutes   = int.Parse(jwtSection["ExpiryMinutes"] ?? "480");
        _refreshExpiryDays = int.Parse(jwtSection["RefreshExpiryDays"] ?? "30");

        // Convert the secret key string to bytes for HMAC signing
        _secretKeyBytes = Encoding.UTF8.GetBytes(secretKey);
    }

    /// <summary>
    /// Generates a signed JWT access token containing the user's claims.
    /// The token includes: userId, email, name, branch, and all roles.
    /// </summary>
    public string GenerateAccessToken(AppUser user)
    {
        // ── 1. Build the claims list ─────────────────────────────────
        // Claims are key-value pairs embedded in the JWT payload.
        // The API reads these on every request to know WHO the user is
        // and WHAT they're allowed to do.
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),         // subject = user ID
            new(JwtRegisteredClaimNames.Email, user.Email),               // email
            new(JwtRegisteredClaimNames.GivenName, user.FirstName),       // first name
            new(JwtRegisteredClaimNames.FamilyName, user.LastName),       // last name
            new("branch_id", user.Branch.ToString()),                     // custom claim: branch
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),  // unique token ID (prevents replay)
        };

        // Add a "role" claim for each role the user has (e.g., "SuperAdmin", "Cashier")
        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // ── 2. Create the signing credentials ────────────────────────
        // HMAC-SHA256: symmetric key — same key signs AND verifies.
        // The API middleware uses this same key to validate incoming tokens.
        var signingKey = new SymmetricSecurityKey(_secretKeyBytes);
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        // ── 3. Build and sign the JWT ────────────────────────────────
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_expiryMinutes),     // token expiry (e.g., 480 min = 8 hours)
            Issuer = _issuer,                                          // who issued the token
            Audience = _audience,                                      // who the token is intended for
            SigningCredentials = credentials,
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        // ── 4. Serialize to compact string format ────────────────────
        // Output: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOi..."
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Generates a cryptographically random refresh token (Base64 string).
    /// This is NOT a JWT — it's an opaque string stored in the DB.
    /// The client sends it back to get a new access token without re-entering credentials.
    /// </summary>
    public string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }

    /// <summary>
    /// Returns when the refresh token should expire (UTC).
    /// Used by AuthService when saving the token to the DB.
    /// </summary>
    public DateTime GetRefreshTokenExpiry()
    {
        return DateTime.UtcNow.AddDays(_refreshExpiryDays);
    }

    /// <summary>
    /// Validates a JWT access token and extracts the ClaimsPrincipal.
    /// Used when you need to read claims from an expired token during refresh flow
    /// (ValidateLifetime = false allows reading expired tokens).
    /// </summary>
    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = false,              // allow expired tokens (we're refreshing)
            ValidIssuer = _issuer,
            ValidAudience = _audience,
            IssuerSigningKey = new SymmetricSecurityKey(_secretKeyBytes),
        };

        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var securityToken);

            // Ensure the token was signed with HMAC-SHA256 (not tampered)
            if (securityToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            // Token is invalid (corrupted, wrong signature, etc.)
            return null;
        }
    }
}