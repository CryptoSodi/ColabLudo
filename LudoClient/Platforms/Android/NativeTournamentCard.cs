using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.ConstraintLayout.Widget;
using LudoClient.Constants;
using LudoClient.Popups;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using SharedCode;
using SharedCode.Constants;
using System.Timers;

namespace LudoClient.Platforms.Android
{
    public class NativeTournamentCard : Microsoft.Maui.Controls.View
    {
        public TournamentDTO Tournament { get; set; }

        public NativeTournamentCard(TournamentDTO tournament)
        {
            Tournament = tournament;
            HeightRequest = 96;
            HorizontalOptions = Microsoft.Maui.Controls.LayoutOptions.Fill;
        }
    }

    public class NativeTournamentCardHandler : ViewHandler<NativeTournamentCard, TournamentDetailView>
    {
        public static PropertyMapper<NativeTournamentCard, NativeTournamentCardHandler> Mapper = new(ViewHandler.ViewMapper)
        {
            [nameof(NativeTournamentCard.Tournament)] = MapTournament,
        };

        public NativeTournamentCardHandler() : base(Mapper)
        {
        }

        protected override TournamentDetailView CreatePlatformView()
        {
            var view = new TournamentDetailView(Context);
            view.LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent);

            return view;
        }

        protected override void ConnectHandler(TournamentDetailView platformView)
        {
            base.ConnectHandler(platformView);
            UpdatePlatformView();
        }

        protected override void DisconnectHandler(TournamentDetailView platformView)
        {
            platformView.Release();
            base.DisconnectHandler(platformView);
        }

        static void MapTournament(NativeTournamentCardHandler handler, NativeTournamentCard view)
        {
            handler.UpdatePlatformView();
        }

        void UpdatePlatformView()
        {
            if (PlatformView != null && VirtualView?.Tournament != null)
            {
                PlatformView.SetDetails(VirtualView.Tournament);
                PlatformView.RequestLayout();
            }
        }

        public override Microsoft.Maui.Graphics.Size GetDesiredSize(double widthConstraint, double heightConstraint)
        {
            if (PlatformView == null)
                return Microsoft.Maui.Graphics.Size.Zero;

            var widthSpec = global::Android.Views.View.MeasureSpec.MakeMeasureSpec(
                (int)Context.ToPixels(widthConstraint),
                global::Android.Views.MeasureSpecMode.AtMost);
            var heightSpec = global::Android.Views.View.MeasureSpec.MakeMeasureSpec(
                0,
                global::Android.Views.MeasureSpecMode.Unspecified);

            PlatformView.Measure(widthSpec, heightSpec);

            return new Microsoft.Maui.Graphics.Size(
                Context.FromPixels(PlatformView.MeasuredWidth),
                Context.FromPixels(PlatformView.MeasuredHeight) > 0 ? Context.FromPixels(PlatformView.MeasuredHeight) : 96);
        }
    }

    [Register("ludoclient.platforms.android.TournamentDetailView")]
    public class TournamentDetailView : ConstraintLayout
    {
        private TextView _tournamentName;
        private TextView _timeRemaining;
        private TextView _prizeOne;
        private TextView _prizeTwo;
        private TextView _prizeThree;
        private TextView _entryPrice;
        private TextView _buttonText;
        private TextView _tournamentStartDate;
        private TextView _tournamentEndDate;
        private global::Android.Views.View _joinButton;
        private TournamentDTO _tournament;
        private DateTime _serverDateTime;
        private System.Timers.Timer? _countdownTimer;
        private bool _navigationCooldown;

        public TournamentDetailView(Context context) : base(context)
        {
            Initialize(context);
        }

        public TournamentDetailView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Initialize(context);
        }

        public TournamentDetailView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
            Initialize(context);
        }

        protected TournamentDetailView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        private void Initialize(Context context)
        {
            var view = LayoutInflater.FromContext(context).Inflate(Resource.Layout.item_tournament_detail, this, true);

            _tournamentName = view.FindViewById<TextView>(Resource.Id.TournamentName);
            _timeRemaining = view.FindViewById<TextView>(Resource.Id.TimeRemaining);
            _prizeOne = view.FindViewById<TextView>(Resource.Id.PrizeOne);
            _prizeTwo = view.FindViewById<TextView>(Resource.Id.PrizeTwo);
            _prizeThree = view.FindViewById<TextView>(Resource.Id.PrizeThree);
            _entryPrice = view.FindViewById<TextView>(Resource.Id.EntryPrice);
            _buttonText = view.FindViewById<TextView>(Resource.Id.ButtonText);
            _tournamentStartDate = view.FindViewById<TextView>(Resource.Id.TournamentStartDate);
            _tournamentEndDate = view.FindViewById<TextView>(Resource.Id.TournamentEndDate);
            _joinButton = view.FindViewById<global::Android.Views.View>(Resource.Id.JoinButton);

            _joinButton.Click += OnJoinClicked;
        }

        public void SetDetails(TournamentDTO tournament)
        {
            Release();
            _tournament = tournament;
            _serverDateTime = tournament.ServerDateTime;

            _tournamentName.Text = tournament.Name;
            _prizeOne.Text = $"{ClientGlobalConstants.NormalizeCoinsDecimal(tournament.Prize1)}";
            _prizeTwo.Text = $"{ClientGlobalConstants.NormalizeCoinsDecimal(tournament.Prize2)}";
            _prizeThree.Text = $"{ClientGlobalConstants.NormalizeCoinsDecimal(tournament.Prize3)}";
            _tournamentStartDate.Text = $"Starts: {tournament.StartDate:g}";
            _tournamentEndDate.Text = $"Ends: {tournament.EndDate:g}";

            UpdateCountdown();
            StartCountdownTimer();
        }

        public void Release()
        {
            if (_countdownTimer == null)
                return;

            _countdownTimer.Stop();
            _countdownTimer.Dispose();
            _countdownTimer = null;
        }

        private void StartCountdownTimer()
        {
            _countdownTimer = new System.Timers.Timer(1000);
            _countdownTimer.Elapsed += OnCountdownTimerElapsed;
            _countdownTimer.AutoReset = true;
            _countdownTimer.Start();
        }

        private void OnCountdownTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            _serverDateTime = _serverDateTime.AddSeconds(1);
            MainThread.BeginInvokeOnMainThread(UpdateCountdown);
        }

        private void UpdateCountdown()
        {
            if (_tournament == null)
                return;

            if (_serverDateTime > _tournament.EndDate)
            {
                _buttonText.Text = "RESULTS";
                _entryPrice.Text = "ENDED";
                _timeRemaining.Text = "Tournament Ended";
                Release();
                return;
            }

            TimeSpan timeRemaining;
            string status;
            if (_serverDateTime > _tournament.StartDate)
            {
                _buttonText.Text = _tournament.IsJoined ? "PLAY" : "JOIN";
                _entryPrice.Text = _tournament.IsJoined
                    ? "JOINED"
                    : $"Entry: {ClientGlobalConstants.NormalizeCoinsDecimal(_tournament.EntryFee)}";
                status = "Ending in:";
                timeRemaining = _tournament.EndDate - _serverDateTime;
            }
            else
            {
                _buttonText.Text = _tournament.IsJoined ? "WAIT" : "JOIN";
                _entryPrice.Text = _tournament.IsJoined
                    ? "JOINED"
                    : $"Entry: {ClientGlobalConstants.NormalizeCoinsDecimal(_tournament.EntryFee)}";
                status = "Starting in:";
                timeRemaining = _tournament.StartDate - _serverDateTime;
            }

            _timeRemaining.Text = $"{status} {timeRemaining:dd\\:hh\\:mm\\:ss}";
        }

        private async void OnJoinClicked(object? sender, EventArgs e)
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");

            if (_navigationCooldown || !GlobalConstants.MatchMaker.Connected || _tournament == null)
                return;

            _navigationCooldown = true;

            try
            {
                var action = _buttonText.Text;
                if (action == "WAIT")
                    return;

                if (action == "PLAY")
                {
                    var gameDto = new GameDto
                    {
                        IsTournamentGame = true,
                        IsPracticeGame = true,
                        GameType = "4",
                        PlayerCount = 4,
                        RoomCode = _tournament.TournamentId.ToString()
                    };

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        ClientGlobalConstants.dashBoard.Navigation.PushAsync(new GameRoom(gameDto.GameType, gameDto.BetAmount));
                        ClientGlobalConstants.FlushOld();
                    });

                    _ = GlobalConstants.MatchMaker.CreateJoinLobbyAsync(gameDto);
                    return;
                }

                if (action == "RESULTS")
                {
                    var resultsDto = await GlobalConstants.MatchMaker.GetResultsTournament(_tournament.TournamentId);
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        ClientGlobalConstants.results = new Results();
                        ClientGlobalConstants.results.init(resultsDto);
                        ClientGlobalConstants.dashBoard.Navigation.PushAsync(ClientGlobalConstants.results);
                    });
                    return;
                }

                var updatedTournament = await GlobalConstants.MatchMaker.JoinTournament(_tournament.TournamentId);
                if (updatedTournament == null)
                    return;

                if (updatedTournament.StatusCode == "SUCCESS" || updatedTournament.StatusCode == "ALREADY_JOINED")
                {
                    MainThread.BeginInvokeOnMainThread(() => SetDetails(updatedTournament));
                    return;
                }

                Console.WriteLine($"Failed to join the tournament. Error: {updatedTournament.StatusCode}");
                if (updatedTournament.StatusCode == "INSUFFICIENT_BALANCE")
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Microsoft.Maui.Controls.Application.Current?.MainPage?.DisplayAlert(
                            "Ludo Cities",
                            "Insufficient balance to join this tournament.",
                            "OK");
                    });
                }
            }
            finally
            {
                await Task.Delay(500);
                _navigationCooldown = false;
            }
        }
    }
}
