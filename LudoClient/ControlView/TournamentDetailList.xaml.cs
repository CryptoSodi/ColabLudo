using LudoClient.Models;
using System.Text;
using System.Text.Json;
using System.Timers;

namespace LudoClient.ControlView
{
    public partial class TournamentDetailList : ContentView
    {
        private readonly HttpClient _httpClient;
        private System.Timers.Timer? countdownTimer;
        private DateTime endDateTime;
        public TournamentDetailList()
        {
            InitializeComponent();
            _httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:7255/") }; // Replace with your API base URL
        }
        /// <summary>
        /// Sets the tournament details on the UI and starts the countdown timer.
        /// </summary>
        public void SetTournamentDetails(int tournamentId, string tournamentName, string startDate, string endDate, decimal entryPrice, decimal prizeAmount)
        {
            // Set the text of the labels
            TournamentId.Text = tournamentId.ToString();
            TournamentNameLabel.Text = tournamentName;
            JoiningFeeLabel.Text = $"Joining Fee : {entryPrice}$";
            PrizeAmountLabel.Text = $"{prizeAmount}$";
            StartDateLabel.Text = $" {startDate}";
            EndDateLabel.Text = $" {endDate}";
            // Parse the end date
            if (DateTime.TryParse(endDate, out DateTime parsedEndDate))
            {
                endDateTime = parsedEndDate;
                // Start the countdown timer
                StartCountdownTimer();
            }
            else
            {
                TimeRemainingLabel.Text = "Invalid end date";
            }
        }
        /// <summary>
        /// Starts a timer that updates the time remaining label every second.
        /// </summary>
        private void StartCountdownTimer()
        {
            // Initialize the timer
            countdownTimer = new System.Timers.Timer(1000); // Update every second
            countdownTimer.Elapsed += OnCountdownTimerElapsed;
            countdownTimer.AutoReset = true;
            countdownTimer.Start();
        }
        /// <summary>
        /// Event handler for the countdown timer's Elapsed event.
        /// </summary>
        private void OnCountdownTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Calculate time remaining
            TimeSpan timeRemaining = endDateTime - DateTime.Now;

            // Update the label on the UI thread
            Device.BeginInvokeOnMainThread(() =>
            {
                if (timeRemaining.TotalSeconds > 0)
                {
                    TimeRemainingLabel.Text = $"Time Remaining {timeRemaining:dd\\:hh\\:mm\\:ss}";
                }
                else
                {
                    TimeRemainingLabel.Text = "Tournament Ended";
                    StopCountdownTimer();
                }
            });
        }
        /// <summary>
        /// Stops and disposes of the countdown timer.
        /// </summary>
        private void StopCountdownTimer()
        {
            if (countdownTimer != null)
            {
                countdownTimer.Stop();
                countdownTimer.Dispose();
                countdownTimer = null;
            }
        }
        /// <summary>
        /// Handles the click event to expand or collapse the tournament details.
        /// </summary>
        private void Expand_Clicked(object sender, EventArgs e)
        {
            if (ExpandSheet.Padding.Top > 0)
            {
                ExpandSheet.Padding = new Thickness(0, 0, 0, 0);
                ExpandSheet.Margin = new Thickness(0, 0, 0, 0);
                SheetDirection.Source = "arr_down.png";
            }
            else
            {
                SheetDirection.Source = "arr_up.png";
                ExpandSheet.Padding = new Thickness(0, (SubSheet.Height - 10), 0, 0);
            }
        }
        private async void Join_Clicked(object sender, EventArgs e)
        {
            int playerId = Preferences.Get("PlayerId", 0);
            int tournamentId = Int32.Parse(TournamentId.Text);

            // Build the URL with query parameters
            var url = $"api/tournament/join?playerId={playerId}&tournamentId={tournamentId}";

            // Send the POST request without a body, as parameters are in the query string
            var response = await _httpClient.PostAsync(url, null);

            if (response.IsSuccessStatusCode)
            {
                // Handle successful join, e.g., show a message or update the UI
                Console.WriteLine("Successfully joined the tournament!");
            }

            else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                Console.WriteLine("You are already Joined");
            }
            else
            {
                // Handle failure, e.g., show an error message
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Failed to join the tournament. Error: {response.StatusCode}, Details: {errorContent}");
            }
        }
    }
}