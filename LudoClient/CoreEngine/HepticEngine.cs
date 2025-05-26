using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Devices;
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

            if (IsVibrationEnabled && hapticInstruct.Equals("kill", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(100));
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