namespace LudoClient.Services
{
    public interface ISoundService
    {
        void Preload(string fileName);
        void Play(string fileName);
    }
}