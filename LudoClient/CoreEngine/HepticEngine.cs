using Plugin.Maui.Audio;

namespace LudoClient.CoreEngine
{
    public class HepticEngine
    {
        private bool IsSoundEnabled;
        private bool IsVibrationEnabled;
        private readonly IAudioManager audioManager;
        private readonly Dictionary<string, IAudioPlayer> audioPlayers = new();

        public HepticEngine()
        {
            IsSoundEnabled = Preferences.Default.Get("IsSoundEnabled", true);
            IsVibrationEnabled = Preferences.Default.Get("IsVibrationEnabled", true);
            audioManager = AudioManager.Current;
        }

        public async Task PlayHapticFeedback(string hapticInstruct)
        {
            // Refresh preferences in case they have changed
            IsSoundEnabled = Preferences.Default.Get("IsSoundEnabled", true);
            IsVibrationEnabled = Preferences.Default.Get("IsVibrationEnabled", true);

            string soundFileName = $"{hapticInstruct.ToLower()}.mp3";

            if (IsSoundEnabled)
            {
                try
                {
                    if (!audioPlayers.ContainsKey(soundFileName))
                    {
                        var stream = await FileSystem.OpenAppPackageFileAsync(soundFileName);
                        var player = audioManager.CreatePlayer(stream);
                        audioPlayers[soundFileName] = player;
                    }

                    var audioPlayer = audioPlayers[soundFileName];
                    audioPlayer.Stop(); // Ensure the player is stopped before replaying
                    audioPlayer.Play();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error playing sound '{soundFileName}': {ex.Message}");
                }
            }
            int vibeMS = 30;

            switch (hapticInstruct) {
                case "click":
                    vibeMS = 30;
                    break;
                case "kill":
                    vibeMS = 80;
                    break;
                case "tak":
                    vibeMS = 100;
                    break;
                case "move":
                    vibeMS = 5;
                    break;
                default:
                    vibeMS = 10;
                    break;
            }
            if (IsVibrationEnabled || hapticInstruct == "tak")
            {
                try
                {
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(vibeMS));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Vibration error: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            foreach (var player in audioPlayers.Values)
            {
                player.Stop();
                player.Dispose();
            }
            audioPlayers.Clear();
        }
    }
}