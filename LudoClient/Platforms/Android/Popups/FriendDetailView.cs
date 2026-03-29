using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.ConstraintLayout.Widget;
using SharedCode;
using SharedCode.Constants;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Maui.ApplicationModel;

namespace LudoClient.Platforms.Android.Popups
{
    [Register("ludoclient.platforms.android.Popups.FriendDetailView")]
    public class FriendDetailView : ConstraintLayout
    {
        private ImageView _backgroundLayerImage;
        private ImageView _playerMadel;
        private TextView _rankingText;
        private ImageView _playerImage;
        private ImageView _playerLocationImage;
        private TextView _playerName;
        private TextView _gamesWonText;
        private global::Android.Views.View _blockAction;
        private TextView _blockActionText;
        private global::Android.Views.View _tappedAction;
        private TextView _tappedActionText;

        public PlayerCard playerCard;
        private string _cardActionType;

        public FriendDetailView(Context context) : base(context)
        {
            Initialize(context);
        }

        public FriendDetailView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Initialize(context);
        }

        public FriendDetailView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
            Initialize(context);
        }

        protected FriendDetailView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        private void Initialize(Context context)
        {
            var inflater = LayoutInflater.FromContext(context);
            var view = inflater.Inflate(Resource.Layout.item_friend_detail, this, true);
            
            // Diagnostic: Set a background color to see if the view is rendered
            // SetBackgroundColor(global::Android.Graphics.Color.Yellow);

            _backgroundLayerImage = view.FindViewById<ImageView>(Resource.Id.BackgroundLayerImage);
            _playerMadel = view.FindViewById<ImageView>(Resource.Id.PlayerMadel);
            _rankingText = view.FindViewById<TextView>(Resource.Id.RankingText);
            _playerImage = view.FindViewById<ImageView>(Resource.Id.PlayerImage);
            _playerLocationImage = view.FindViewById<ImageView>(Resource.Id.PlayerLocationImage);
            _playerName = view.FindViewById<TextView>(Resource.Id.PlayerName);
            _gamesWonText = view.FindViewById<TextView>(Resource.Id.GamesWonText);
            _blockAction = view.FindViewById<global::Android.Views.View>(Resource.Id.BlockAction);
            _blockActionText = view.FindViewById<TextView>(Resource.Id.BlockActionText);
            _tappedAction = view.FindViewById<global::Android.Views.View>(Resource.Id.TappedAction);
            _tappedActionText = view.FindViewById<TextView>(Resource.Id.TappedActionText);

            _blockAction.Click += OnBlockActionClicked;
            _tappedAction.Click += OnTappedActionClicked;
        }

        public void SetDetails(PlayerCard pc, string type)
        {
            this.playerCard = pc;
            this._cardActionType = type;

            _playerName.Text = pc.name;
            
            // RESET FIRST: Hide all action elements
            _blockAction.Visibility = ViewStates.Gone;
            _tappedAction.Visibility = ViewStates.Gone;
            _gamesWonText.Visibility = ViewStates.Gone;

            // Placeholder logic
            _playerImage.SetImageResource(Resource.Drawable.player);
            if (!string.IsNullOrEmpty(pc.pictureUrl))
            {
                Task.Run(async () =>
                {
                    try
                    {
                        var httpClient = new System.Net.Http.HttpClient();
                        var bytes = await httpClient.GetByteArrayAsync(pc.pictureUrl);
                        var bitmap = await global::Android.Graphics.BitmapFactory.DecodeByteArrayAsync(bytes, 0, bytes.Length);
                        if (bitmap != null)
                        {
                            MainThread.BeginInvokeOnMainThread(() => _playerImage.SetImageBitmap(bitmap));
                        }
                    }
                    catch { }
                });
            }

            // Rank logic
            switch (pc.rank)
            {
                case 1:
                    _playerMadel.SetImageResource(Resource.Drawable.number_1);
                    _playerName.SetTextColor(global::Android.Graphics.Color.Black);
                    _gamesWonText.SetTextColor(global::Android.Graphics.Color.Black);
                    _backgroundLayerImage.SetImageResource(Resource.Drawable.detailgold);
                    break;
                case 2:
                    _playerMadel.SetImageResource(Resource.Drawable.number_2);
                    _playerName.SetTextColor(global::Android.Graphics.Color.Black);
                    _gamesWonText.SetTextColor(global::Android.Graphics.Color.Black);
                    _backgroundLayerImage.SetImageResource(Resource.Drawable.detailwhite);
                    break;
                case 3:
                    _playerMadel.SetImageResource(Resource.Drawable.number_3);
                    _playerName.SetTextColor(global::Android.Graphics.Color.Black);
                    _gamesWonText.SetTextColor(global::Android.Graphics.Color.Black);
                    _backgroundLayerImage.SetImageResource(Resource.Drawable.detailwhite);
                    break;
                default:
                    _playerMadel.SetImageResource(Resource.Drawable.number_0);
                    _playerName.SetTextColor(global::Android.Graphics.Color.White);
                    _gamesWonText.SetTextColor(global::Android.Graphics.Color.White);
                    _backgroundLayerImage.SetImageResource(Resource.Drawable.friendlong);
                    break;
            }

            // Type specific logic
            _blockAction.Visibility = ViewStates.Visible;
            _tappedAction.Visibility = ViewStates.Visible;
            _gamesWonText.Visibility = ViewStates.Gone;

            if (type == "Header")
            {
                _playerMadel.Visibility = ViewStates.Gone;
                _rankingText.Visibility = ViewStates.Gone;
                _playerName.SetTextColor(global::Android.Graphics.Color.White);
                _backgroundLayerImage.SetImageResource(Resource.Drawable.friendlong);
                _blockAction.Visibility = ViewStates.Gone;
                _tappedAction.Visibility = ViewStates.Gone;
            }
            else if (type == "Friend")
            {
                if (pc.status == "UN FRIEND" || pc.status == "UN BLOCK" || pc.status == "ADD FRIEND")
                {
                    _tappedActionText.Text = "MESSAGE";
                    _blockAction.Visibility = ViewStates.Gone;
                }
                if (pc.status == "BLOCK")
                {
                    _tappedAction.Visibility = ViewStates.Gone;
                    _blockActionText.Text = "UN BLOCK";
                }
                if (pc.status == "BLOCKED_BY_OTHER")
                {
                    _tappedAction.Visibility = ViewStates.Gone;
                    _blockAction.Visibility = ViewStates.Gone;
                }
            }
            else if (type == "Leaderboard")
            {
                _blockAction.Visibility = ViewStates.Gone;
                _tappedAction.Visibility = ViewStates.Gone;
                _gamesWonText.Visibility = ViewStates.Visible;
                _gamesWonText.Text = pc.gamesWon.ToString(); 
            }

            _rankingText.Text = pc.rank > 99 ? "99+" : pc.rank.ToString();
        }

        private async void OnBlockActionClicked(object sender, EventArgs e)
        {
            Constants.ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            await SendFriendRequestAsync(playerCard.playerID, _blockActionText.Text);
        }

        private async void OnTappedActionClicked(object sender, EventArgs e)
        {
            Constants.ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            if (_tappedActionText.Text == "MESSAGE")
            {
                if (!GlobalConstants.MatchMaker.Connected) return;
                // Navigate to ChatPage (MAUI)
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Microsoft.Maui.Controls.Application.Current.MainPage.Navigation.PushAsync(new ChatPage(playerCard));
                });
            }
            else
            {
                await SendFriendRequestAsync(playerCard.playerID, _tappedActionText.Text);
            }
        }

        public async Task SendFriendRequestAsync(int receiverId, string status)
        {
            string responseBody = await GlobalConstants.MatchMaker._hubConnection.InvokeAsync<string>("SendFriendRequest", receiverId, status);
            if (status == responseBody)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    playerCard.status = status;
                    // Update UI states
                    if (status == "BLOCK")
                    {
                        _tappedAction.Visibility = ViewStates.Gone;
                        _blockAction.Visibility = ViewStates.Visible;
                        _blockActionText.Text = "UN BLOCK";
                    }
                    else if (status == "UN BLOCK" || status == "UN FRIEND" || status == "ADD FRIEND")
                    {
                        if (_cardActionType == "Friend")
                        {
                            _tappedActionText.Text = "MESSAGE";
                            _blockAction.Visibility = ViewStates.Gone;
                            _tappedAction.Visibility = ViewStates.Visible;
                        }
                    }
                });
            }
        }
    }
}