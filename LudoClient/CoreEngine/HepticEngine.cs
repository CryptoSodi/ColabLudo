using LudoClient.Services;

namespace LudoClient.CoreEngine
{
    public class HepticEngine
    {
        private bool IsSoundEnabled;
        private bool IsVibrationEnabled;
        private readonly ISoundService _soundService;

        public HepticEngine(ISoundService soundService)
        {
            _soundService = soundService;
            IsSoundEnabled = Preferences.Default.Get("IsSoundEnabled", true);
            IsVibrationEnabled = Preferences.Default.Get("IsVibrationEnabled", true);

            // Preload sounds once
            _soundService.Preload("click.mp3");
            _soundService.Preload("diceroll.mp3");
            _soundService.Preload("ding.mp3");
            _soundService.Preload("home.mp3");
            _soundService.Preload("kill.mp3");
            _soundService.Preload("left.mp3");
            _soundService.Preload("move.mp3");
            _soundService.Preload("playerjoin.mp3");
            _soundService.Preload("tak.mp3");
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
                    _soundService.Play(soundFileName);
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
                catch (Exception)
                {
                   // Console.WriteLine($"Vibration error: {ex.Message}");
                }
            }
        }
    }
}