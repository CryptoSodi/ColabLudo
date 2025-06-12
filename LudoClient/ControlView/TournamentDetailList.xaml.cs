using LudoClient.Constants;
using LudoClient.Models;
using Newtonsoft.Json.Serialization;
using SharedCode;
using SharedCode.Constants;
using System.Security.AccessControl;
using System.Text.Json;
using System.Timers;
using static Google.Apis.Requests.BatchRequest;

namespace LudoClient.ControlView
{
    public partial class TournamentDetailList : ContentView
    {
        TournamentDTO tournament;
        DateTime ServerDateTime;
        private System.Timers.Timer? countdownTimer;
        
        public TournamentDetailList(TournamentDTO tournament)
        {
            InitializeComponent();
            SetTournamentDetails(tournament);
        }
        internal void SetTournamentDetails(TournamentDTO tournament)
        {
            ServerDateTime = tournament.ServerDateTime;
            this.tournament = tournament;
            
            string status;
            TournamentNameLabel.Text = tournament.Name;
            StartDateLabel.Text = $"Starts: {tournament.StartDate}";
            EndDateLabel.Text = $"Ends: {tournament.EndDate}";
            EntryPriceLabel.Text = $"Entry: {tournament.EntryFee}";
            PrizeAmountLabel1.Text = $"{tournament.Prize1}";
            PrizeAmountLabel2.Text = $"{tournament.Prize2}";
            PrizeAmountLabel3.Text = $"{tournament.Prize3}";
            TournamentId.Text = tournament.TournamentId.ToString();
            OnCountdownTimerElapsed(null,null);
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
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (ServerDateTime > tournament.EndDate)
                {
                    ButtonText.Text = "RESULTS";
                    EntryPriceLabel.Text = "ENDED";
                    status = "Completed";
                    TimeRemainingLabel.Text = "Tournament Ended";
                    StopCountdownTimer();
                    return; // No need to update the label if the tournament has ended
                }
                else if (ServerDateTime > tournament.StartDate)
                {
                    EntryPriceLabel.Text = $"JOINED";
                    ButtonText.Text = "PLAY";
                    status = "Ending in :";
                    timeRemaining = ServerDateTime - tournament.EndDate;
                }
                else
                {
                    if (tournament.IsJoined)
                    {
                        EntryPriceLabel.Text = $"JOINED";
                        ButtonText.Text = "WAIT";
                    }
                    else
                    {
                        ButtonText.Text = "JOIN";
                    }

                    status = "Starting in :";
                    timeRemaining = ServerDateTime - tournament.StartDate;
                }
                // Calculate the fixed time difference


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
        bool joining = false;
        private async void Join_Clicked(object sender, EventArgs e)
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            if (joining)
                return;
            joining = true;

            if (ButtonText.Text == "WAIT") return;
            if (ButtonText.Text == "PLAY") 
            {
                PlayerDto player = new PlayerDto();
                player.PlayerId = UserInfo.Instance.Id;
                player.PlayerName = UserInfo.Instance.Name;
                player.PlayerPicture = UserInfo.Instance.PictureUrl;

                GameDto gameDto = new GameDto();
                gameDto.IsTournamentGame = true; // Set the tournament game flag
                gameDto.IsPracticeGame = true; // Set the practice game flag
                gameDto.GameType = "4";
                gameDto.PlayerCount = 4;
                gameDto.RoomCode = tournament.TournamentId.ToString();
                
                //Navigation.PushAsync(new GameRoom(gameType, entry));
                _ = GlobalConstants.MatchMaker.CreateJoinLobbyAsync(player, gameDto);
                return;
            }
            tournament = await GlobalConstants.MatchMaker.JoinTournament(int.Parse(TournamentId.Text));
            Console.WriteLine($"Failed to join the tournament. Error: {tournament.StatusCode}");
            if (tournament == null)
                //No tournament running with this ID
                return;
            if (tournament.StatusCode == "FAILED")
            {
                Console.WriteLine($"Failed to join the tournament. Error: {tournament.StatusCode}");
            }
            joining = false;
        }
    }
}