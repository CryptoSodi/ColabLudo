using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LudoServer.Data;
using LudoServer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace SignalR.Server.Services
{
    public class CivicAuthService
    {
        private readonly IDbContextFactory<LudoDbContext> _contextFactory;
        private readonly CryptoHelper _crypto;
        private readonly UtilService _utilService;

        // ---- Embed your Civic client IDs here (dev/prod). Use the same for both if you only have one.
        private readonly string _expectedAudience = "205c334a-404e-4f88-a999-a80b0b8e0504";
        private readonly string _expectedAudienceWeb = "205c334a-404e-4f88-a999-a80b0b8e0504";
        // Civic OIDC issuer (trailing slash is required)
        private const string CivicIssuer = "https://auth.civic.com/oauth/";

        public CivicAuthService(IDbContextFactory<LudoDbContext> contextFactory, CryptoHelper crypto, UtilService utilService)
        {
            _contextFactory = contextFactory;
            _crypto = crypto;
            _utilService = utilService;
        }

        public async Task<Player> CivicAuthentication(string idToken, string city, string countryCode, string role = "Player")
        {
            try
            {
                using var ctx = _contextFactory.CreateDbContext();

                string email = "";
                string name = "";
                string pictureUrl = "";
                string civicId = "";

                // Keep your guest shortcuts
                if (idToken == "Guest1")
                {
                    email = "Sodi@gmail.com";
                    name = "Sodi";
                    pictureUrl = "https://yt3.ggpht.com/ytc/AIdro_nuNlfceTDiBSTQUhxQ56YDJFbBu1DjRfTpJMFP6ck9D0x3tsglom8eMUA2blBLpRVU8w=s108-c-k-c0x00ffffff-no-rj";
                    civicId = idToken;
                }
                else if (idToken == "Guest2")
                {
                    email = "Sodi2@gmail.com";
                    name = "Sodi2";
                    pictureUrl = "https://yt3.ggpht.com/ytc/AIdro_nuNlfceTDiBSTQUhxQ56YDJFbBu1DjRfTpJMFP6ck9D0x3tsglom8eMUA2blBLpRVU8w=s108-c-k-c0x00ffffff-no-rj";
                    civicId = idToken;
                }
                else
                {
                    // ✅ Validate Civic ID token and extract claims
                    var principal = await ValidateCivicIdTokenAsync(idToken);
                    civicId = principal.FindFirstValue("sub") ?? throw new SecurityTokenException("sub missing");
                    email = principal.FindFirstValue("email") ?? "";
                    name = principal.FindFirstValue("name") ?? "";
                    pictureUrl = principal.FindFirstValue("picture") ?? "";
                }

                // NOTE: to avoid a DB migration right now, we reuse GoogleId to store Civic sub.
                // If/when you add a CivicId column, replace GoogleId with CivicId in the two places below.

                Player? existingPlayer = await ctx.Players.FirstOrDefaultAsync(p => p.Email == email); // <- replace with p.CivicId when you add it

                if (existingPlayer == null)
                {
                    var newPlayer = new Player
                    {
                        GoogleId = civicId,     // <- replace with CivicId when you add it
                        Name = name,
                        Email = email,
                        PictureUrl = pictureUrl,
                        City = city,
                        CountryCode = countryCode,
                        IsOnline = true,
                        AuthToken = "",
                        Role = role
                    };

                    ctx.Players.Add(newPlayer);
                    await ctx.SaveChangesAsync();

                    existingPlayer = await ctx.Players.FirstOrDefaultAsync(p => p.Email == email); // <- replace with CivicId
                    if (existingPlayer is not null)
                    {
                        existingPlayer.AuthToken = _crypto.Encrypt(existingPlayer.PlayerId.ToString());
                        await ctx.SaveChangesAsync();
                    }
                }
                else
                {
                    // Optional: keep profile fresh
                    bool changed = false;
                    if (!string.IsNullOrWhiteSpace(name) && existingPlayer.Name != name) { existingPlayer.Name = name; changed = true; }
                    if (!string.IsNullOrWhiteSpace(email) && existingPlayer.Email != email) { existingPlayer.Email = email; changed = true; }
                    if (!string.IsNullOrWhiteSpace(pictureUrl) && existingPlayer.PictureUrl != pictureUrl) { existingPlayer.PictureUrl = pictureUrl; changed = true; }
                    if (!string.IsNullOrWhiteSpace(city) && existingPlayer.City != city) { existingPlayer.City = city; changed = true; }
                    if (!string.IsNullOrWhiteSpace(countryCode) && existingPlayer.CountryCode != countryCode) { existingPlayer.CountryCode = countryCode; changed = true; }
                    if (changed) await ctx.SaveChangesAsync();
                }

                return existingPlayer!;
            }
            catch (Exception ex)
            {
                throw new Exception($"Civic Authentication failed : {ex.Message}", ex);
            }
        }

        private async Task<ClaimsPrincipal> ValidateCivicIdTokenAsync(string idToken)
        {
            const string issuer = "https://auth.civic.com/oauth/"; // NOTE: trailing slash

            var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                issuer + ".well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever());

            var config = await configManager.GetConfigurationAsync();

            var parameters = new TokenValidationParameters
            {
                ValidIssuer = issuer,
                ValidateIssuer = true,

                ValidAudiences = new[] { _expectedAudience, _expectedAudienceWeb },
                ValidateAudience = true,

                IssuerSigningKeys = config.SigningKeys,
                ValidateIssuerSigningKey = true,

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2),

                // Optional: keep these explicit (doesn't control mapping)
                NameClaimType = "name",
                RoleClaimType = "role",
            };

            var handler = new JwtSecurityTokenHandler();

            // ✅ THIS is how you disable claim type mapping:
            //    prevents "sub" being remapped to ClaimTypes.NameIdentifier, etc.
            handler.MapInboundClaims = false;
            // Alternatively (global): JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

            return handler.ValidateToken(idToken, parameters, out _);
        }


    }

    internal static class ClaimsPrincipalExtensions
    {
        public static string? FindFirstValue(this ClaimsPrincipal principal, string type)
            => principal.FindFirst(type)?.Value;
    }
}
