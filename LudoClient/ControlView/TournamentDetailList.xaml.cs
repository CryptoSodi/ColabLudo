using LudoClient.Constants;
using LudoClient.Popups;
using SharedCode;
using SharedCode.Constants;
using System.Security.AccessControl;
using System.Text.Json;
using System.Timers;

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
            Unloaded += (_, _) => StopCountdownTimer();
            SetTournamentDetails(tournament);
        }
        internal void SetTournamentDetails(TournamentDTO tournament)
        {
            StopCountdownTimer();
            ServerDateTime = tournament.ServerDateTime;
            this.tournament = tournament;
            
            TournamentNameLabel.Text = tournament.Name;
            StartDateLabel.Text = $"Starts: {tournament.StartDate}";
            EndDateLabel.Text = $"Ends: {tournament.EndDate}";
            EntryPriceLabel.Text = $"Entry: {ClientGlobalConstants.NormalizeCoinsDecimal(tournament.EntryFee)}";
            PrizeAmountLabel1.Text = $"{ClientGlobalConstants.NormalizeCoinsDecimal(tournament.Prize1)}";
            PrizeAmountLabel2.Text = $"{ClientGlobalConstants.NormalizeCoinsDecimal(tournament.Prize2)}";
            PrizeAmountLabel3.Text = $"{ClientGlobalConstants.NormalizeCoinsDecimal(tournament.Prize3)}"; 
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
                    if (tournament.IsJoined)
                    {
                        EntryPriceLabel.Text = $"JOINED";
                        ButtonText.Text = "PLAY";
                    }
                    else
                    {
                        ButtonText.Text = "JOIN";
                    }
                    status = "Ending in :";
                    timeRemaining = tournament.EndDate - ServerDateTime;
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
                    timeRemaining = tournament.StartDate - ServerDateTime;
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
        internal void Release()
        {
            StopCountdownTimer();
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
                SheetDirection.Source = "arr_down.webp";
            }
            else
            {
                ExpandSheet.Padding = new Thickness(0, (SubSheet.Height - 10), 0, 0);
                SheetDirection.Source = "arr_up.webp";
            }
        }
        bool _NavigationCooldown = false;
        private async void Join_Clicked(object sender, EventArgs e)
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            if (_NavigationCooldown)
                return;
            _NavigationCooldown = true;

            if (ButtonText.Text == "WAIT") { _NavigationCooldown = false; return; }
            if (ButtonText.Text == "PLAY")
            {
                GameDto gameDto = new GameDto();
                gameDto.IsTournamentGame = true; // Set the tournament game flag
                gameDto.IsPracticeGame = true; // Set the practice game flag
                gameDto.GameType = "4";
                gameDto.PlayerCount = 4;
                gameDto.RoomCode = tournament.TournamentId.ToString();
              
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ClientGlobalConstants.dashBoard.Navigation.PushAsync(new GameRoom(gameDto.GameType, gameDto.BetAmount));
                    ClientGlobalConstants.FlushOld();
                });
                try
                {
                    //Navigation.PushAsync(new GameRoom(gameType, entry));
                    _ = GlobalConstants.MatchMaker.CreateJoinLobbyAsync(gameDto);
                }
                finally
                {
                    await Task.Delay(500); // half-second cooldown
                    _NavigationCooldown = false;
                }
                return;
            }
            if (ButtonText.Text == "RESULTS")
            {
                TournamentResultDTO tournamentResultDTO = await GlobalConstants.MatchMaker.GetResultsTournament(int.Parse(TournamentId.Text));
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Task.Delay(100);
                    ClientGlobalConstants.results = new Results();
                    ClientGlobalConstants.results.init(tournamentResultDTO);
                });
                ClientGlobalConstants.dashBoard.Navigation.PushAsync(ClientGlobalConstants.results);

            }
            tournament = await GlobalConstants.MatchMaker.JoinTournament(int.Parse(TournamentId.Text));
            
            if (tournament == null)
            {
                MainThread.BeginInvokeOnMainThread(() => { _NavigationCooldown = false; });
                return;
            }

            if (tournament.StatusCode == "SUCCESS" || tournament.StatusCode == "ALREADY_JOINED")
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    SetTournamentDetails(tournament); // Refreshes labels and button text
                    _NavigationCooldown = false;
                });
            }
            else
            {
                Console.WriteLine($"Failed to join the tournament. Error: {tournament.StatusCode}");
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (tournament.StatusCode == "INSUFFICIENT_BALANCE")
                    {
                        Application.Current?.MainPage?.DisplayAlert("Ludo Cities", "Insufficient balance to join this tournament.", "OK");
                    }
                    _NavigationCooldown = false;
                });
            }
        }
    }
}
