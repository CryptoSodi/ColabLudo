namespace LudoClient.Services
{
    public interface IGoogleAuthService
    {
        Task<string?> SignInAsync();
        Task<bool> SignOutAsync();

        // Add these properties to expose user info
        string? GoogleId { get; }
        string? UserName { get; }
        string? UserEmail { get; }
        string? UserPhotoUrl { get; }
    }

    public interface IDeviceIdentifierService
    {
        string GetDeviceId();
    }
}
