using Plugin.Maui.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LudoClient.CoreEngine
{
    public class HepticEngine
    {
        private bool IsSoundEnabled;
        private bool IsVibrationEnabled;
        private IAudioManager audioManager;
        public HepticEngine()
        {
            IsSoundEnabled = Preferences.Default.Get("IsSoundEnabled", true);
            IsVibrationEnabled = Preferences.Default.Get("IsVibrationEnabled", true);
            audioManager = AudioManager.Current;
        }
        public async Task PlayHapticFeedback(string hapticInstruct)
        {
            IsSoundEnabled = Preferences.Default.Get("IsSoundEnabled", true);
            IsVibrationEnabled = Preferences.Default.Get("IsVibrationEnabled", true);

            if (IsSoundEnabled)
            {
                try
                {
                    var stream = await FileSystem.OpenAppPackageFileAsync(hapticInstruct.ToLower()+".mp3");
                    var audioPlayer = audioManager.CreatePlayer(stream);
                    audioPlayer.Play();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error playing sound: {ex.Message}");
                }
            }



            switch (hapticInstruct.ToLower())
            {
                case "kill":
            
                    if (IsVibrationEnabled)
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
                    break;

                default:
                    Console.WriteLine($"No haptic feedback defined for '{hapticInstruct}'");
                    break;
            }
        }
    }
}