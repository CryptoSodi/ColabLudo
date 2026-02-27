using Plugin.Maui.Audio;

namespace LudoClient.Services
{
    public class MauiAudioService : ISoundService
    {
        private readonly IAudioManager _audioManager;
        private readonly Dictionary<string, IAudioPlayer> _players = new();
        public MauiAudioService()
        {
            _audioManager = AudioManager.Current;
        }
        public void Preload(string fileName)
        {
            if (_players.ContainsKey(fileName))
                return;

            var streamTask = FileSystem.OpenAppPackageFileAsync(fileName);
            streamTask.Wait(); // safe during preload

            var player = _audioManager.CreatePlayer(streamTask.Result);
            _players[fileName] = player;
        }

        public void Play(string fileName)
        {
            if (_players.TryGetValue(fileName, out var player))
            {
                player.Stop();
                player.Play();
            }
        }
    }
}
