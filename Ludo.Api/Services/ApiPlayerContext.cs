using System.Security.Cryptography;
using System.Text;
using LudoServer.Data;
using LudoServer.Models;
using Microsoft.EntityFrameworkCore;

namespace Ludo.Api.Services;

public class ApiPlayerContext(IDbContextFactory<LudoDbContext> contextFactory)
{
    public async Task<Player?> GetAuthenticatedPlayerAsync(HttpRequest request)
    {
        var authToken = GetAuthToken(request);
        if (string.IsNullOrWhiteSpace(authToken))
            return null;

        string playerIdText;
        try
        {
            playerIdText = Decrypt(authToken);
        }
        catch
        {
            return null;
        }

        if (!int.TryParse(playerIdText, out var playerId))
            return null;

        using var ctx = await contextFactory.CreateDbContextAsync();
        var player = await ctx.Players.AsNoTracking().FirstOrDefaultAsync(p => p.PlayerId == playerId);
        if (player == null || player.IsBlocked)
            return null;

        return player;
    }

    private static string GetAuthToken(HttpRequest request)
    {
        if (request.Headers.TryGetValue("X-Auth-Token", out var token) && !string.IsNullOrWhiteSpace(token))
            return token.ToString();

        var authHeader = request.Headers.Authorization.ToString();
        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return authHeader["Bearer ".Length..].Trim();

        return string.Empty;
    }

    private static string Decrypt(string cipherText)
    {
        using var aes = Aes.Create();
        aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes("CryptoHelper.WalletProtector"));
        aes.IV = new byte[16];
        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

        using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }
}
