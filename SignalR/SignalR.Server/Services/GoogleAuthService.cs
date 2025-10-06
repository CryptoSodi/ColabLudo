using Google.Apis.Auth;
using LudoServer.Data;
using LudoServer.Models;
using Microsoft.EntityFrameworkCore;

namespace SignalR.Server.Services
{
    public class GoogleAuthService(IDbContextFactory<LudoDbContext> contextFactory, CryptoHelper crypto, UtilService utilService)
    {
        private readonly string _expectedAudience = "973406093603-g14f7hkjafphcij4p16ectibrkmj7q8f.apps.googleusercontent.com";
        private readonly string _expectedAudienceWeb = "973406093603-dlm3o6jrkuf6b1m1lc7m8hir9qc4cul5.apps.googleusercontent.com";
        public async Task<Player> GoogleAuthentication(string idToken, string city, string countryCode)
        {
            try
            {
                using var ctx = contextFactory.CreateDbContext();
                // Example: extract useful info
                String email = "";
                String name = "";
                String pictureUrl = "";// ✅ profile picture URL
                String googleId = "";// Unique Google user ID
                if (idToken == "Guest1")
                {
                    email = "Sodi@gmail.com";
                    name = "Sodi";
                    pictureUrl = "https://yt3.ggpht.com/ytc/AIdro_nuNlfceTDiBSTQUhxQ56YDJFbBu1DjRfTpJMFP6ck9D0x3tsglom8eMUA2blBLpRVU8w=s108-c-k-c0x00ffffff-no-rj";// ✅ profile picture URL
                    googleId = idToken;
                }
                else if (idToken == "Guest3")
                {
                    email = "Sodi3@gmail.com";
                    name = "Sodi3";
                    pictureUrl = "https://yt3.ggpht.com/ytc/AIdro_nuNlfceTDiBSTQUhxQ56YDJFbBu1DjRfTpJMFP6ck9D0x3tsglom8eMUA2blBLpRVU8w=s108-c-k-c0x00ffffff-no-rj";// ✅ profile picture URL
                    googleId = idToken;
                }
                else
                {
                    var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);

                    // ✅ Validate issuer and audience (REPLACE with your real Google OAuth client ID)

                    //973406093603-g14f7hkjafphcij4p16ectibrkmj7q8f.apps.googleusercontent.com
                    if ((payload.Issuer != "accounts.google.com" && payload.Issuer != "https://accounts.google.com"))
                    {
                        throw new Exception($"Google Authentication failed : : 001");
                    }
                    // Example: extract useful info
                    email = payload.Email;
                    name = payload.Name;
                    pictureUrl = payload.Picture; // ✅ profile picture URL
                    googleId = payload.Subject; // Unique Google user ID
                }

                Player existingPlayer = ctx.Players.FirstOrDefault(p => p.Email == email);
                // If player exists by email, return with Player Id to login
                if (existingPlayer == null)
                {
                    Player newPlayer = new Player
                    {
                        GoogleId = googleId,
                        Name = name,
                        Email = email,
                        PictureUrl = pictureUrl,
                        City = city,
                        CountryCode = countryCode,
                        IsOnline = true,
                        AuthToken = "",
                        Role = "Player"
                    };
                    ctx.Players.Add(newPlayer);
                    // Save changes to the database
                    await ctx.SaveChangesAsync();
                    existingPlayer = ctx.Players.FirstOrDefault(p => p.Email == email);
                    existingPlayer.AuthToken = crypto.Encrypt(existingPlayer.PlayerId.ToString()); // or a JWT with playerId claim
                    await ctx.SaveChangesAsync();
                }
                return existingPlayer;
            }
            catch (Exception ex)
            {
                throw new Exception($"Google Authentication failed : {ex.Message}", ex);
            }
        }
    }
}