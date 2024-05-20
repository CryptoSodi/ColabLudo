using Google.Apis.Auth.OAuth2;
using System.Threading.Tasks;

namespace LudoClient.Utilities
{

    public class AuthenticationService
    {
        public async Task<UserCredential> AuthenticateAsync()
        {
            // Client secrets from the Google Developer Console.
            var clientSecrets = new ClientSecrets
            {
                ClientId = "104479379594-k4ckrhv61ce7d7ic92kse3j3j0nlmg2d.apps.googleusercontent.com",
                ClientSecret = "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"
            };

            // Scopes for which to request access.
            string[] scopes = { "https://www.googleapis.com/auth/userinfo.profile" };

            // Authenticate the user.
            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets,
                scopes,
                "user",
                CancellationToken.None);

            return credential;
        }
    }
}