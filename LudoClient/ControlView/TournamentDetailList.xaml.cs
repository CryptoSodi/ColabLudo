using LudoClient.Models;
using SharedCode.Constants;
using System.Timers;

namespace LudoClient.ControlView
{
    public partial class TournamentDetailList : ContentView
    {
        Tournament tournament;
        DateTime ServerDateTime;
        private System.Timers.Timer? countdownTimer;
        
        public TournamentDetailList()
        {
            InitializeComponent();
        }
        internal void SetTournamentDetails(Tournament tournament)
        {
            ServerDateTime = tournament.ServerDateTime;
            this.tournament = tournament;
            
            string status;
            TournamentNameLabel.Text = tournament.TournamentName;
            StartDateLabel.Text = $"Starts: {tournament.StartDate}";
            EndDateLabel.Text = $"Ends: {tournament.EndDate}";
            EntryPriceLabel.Text = $"Entry: {tournament.EntryPrice}";
            PrizeAmountLabel.Text = $"Prize: {tournament.PrizeAmount}";
            StartCountdownTimer();
        }
        /// <summary>
        /// Starts a timer that updates the time remaining label every second.
        /// </summary>
        private void StartCountdownTimer()
        {
            countdownTimer = new System.Timers.Timer(1000); // 1 second
            countdownTimer.Elapsed += OnCountdownTimerElapsed;
            countdownTimer.AutoReset = true;
            countdownTimer.Start();
        }
        /// <summary>
        /// Event handler for the countdown timer's Elapsed event.
        /// </summary>
        private void OnCountdownTimerElapsed(object sender, ElapsedEventArgs e)
        {
            //DateTime timeRemaining;
            String status;
            TimeSpan timeRemaining;

            ServerDateTime = ServerDateTime.Add(TimeSpan.FromSeconds(1));
            if (ServerDateTime > tournament.EndDate)
            {
                status = "Completed";
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    TimeRemainingLabel.Text = "Tournament Ended";
                });
                StopCountdownTimer();
                return; // No need to update the label if the tournament has ended
            }
            else
            if (ServerDateTime > tournament.StartDate)
            {
                status = "Ending in :"; 
                timeRemaining = ServerDateTime - tournament.EndDate;
            }
            else
            {
                status = "Starting in :";
                timeRemaining = ServerDateTime - tournament.StartDate;
            }
            // Calculate the fixed time difference

            MainThread.BeginInvokeOnMainThread(() =>
            {
                TimeRemainingLabel.Text = $"{status} {timeRemaining:dd\\:hh\\:mm\\:ss}";
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
                ExpandSheet.Padding = new Thickness(0, (SubSheet.Height - 10), 0, 0);
                SheetDirection.Source = "arr_up.png";
            }
        }
        private async void Join_Clicked(object sender, EventArgs e)
        {
            int playerId = Preferences.Get("PlayerId", UserInfo.Instance.Id);
            int tournamentId = Int32.Parse(TournamentId.Text);

            // Build the URL with query parameters
            var url = $"api/tournament/join?playerId={playerId}&tournamentId={tournamentId}";

            // Send the POST request without a body, as parameters are in the query string
            var response = await GlobalConstants.httpClient.PostAsync(url, null);

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