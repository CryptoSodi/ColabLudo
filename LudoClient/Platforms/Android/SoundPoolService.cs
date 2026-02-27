using Android.Content;
using Android.Media;
using LudoClient.Services;
using Android.App;

namespace LudoClient.Platforms.Android
{
    public class SoundPoolService : ISoundService
    {
        private readonly SoundPool _soundPool;
        private readonly Dictionary<string, int> _soundIds = new();
        private readonly Context _context;

        public SoundPoolService()
        {
            _context = global::Android.App.Application.Context;

            _soundPool = new SoundPool.Builder()
                .SetMaxStreams(5)
                .Build();
        }

        public void Preload(string fileName)
        {
            if (_soundIds.ContainsKey(fileName))
                return;

            using var afd = _context.Assets.OpenFd(fileName);
            int soundId = _soundPool.Load(afd, 1);
            _soundIds[fileName] = soundId;
        }

        public void Play(string fileName)
        {
            if (_soundIds.TryGetValue(fileName, out var soundId))
            {
                _soundPool.Play(soundId, 1f, 1f, 1, 0, 1f);
            }
        }
    }
}
