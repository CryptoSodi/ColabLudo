using System;
using System.Timers;
using Microsoft.Maui.Controls;

namespace LudoClient.ControlView
{
    public partial class TournamentDetailList : ContentView
    {
        private System.Timers.Timer countdownTimer;
        private DateTime endDateTime;

        public TournamentDetailList()
        {
            InitializeComponent();

            // Initialize the tournament details
           
        }

        /// <summary>
        /// Sets the tournament details on the UI and starts the countdown timer.
        /// </summary>
        public void SetTournamentDetails(
            string tournamentName,
            string startDate,
            string endDate,
            decimal entryPrice,
            decimal prizeAmount)
        {
            // Set the text of the labels
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
    }
}