using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;
using CommunityToolkit.Maui.Extensions;
using LudoClient.Constants;
using SharedCode.Constants;

namespace LudoClient.Platforms.Android.Popups
{
    public class ProfileInfoDialogFragment : DialogFragment
    {
        private PlayerBoxLongView _playerBox;
        private TextView _emailText;
        private TextView _numberText;
        private TextView _locationText;
        private TextView _nftCountText;
        private global::Android.Views.View _manageNftsBtn;

        private StatisticCardView _statPlayed;
        private StatisticCardView _statWon;
        private StatisticCardView _statLost;
        private StatisticCardView _statBestWin;
        private StatisticCardView _statTotalWin;
        private StatisticCardView _statTotalLost;

        public override global::Android.Views.View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            if (Dialog != null && Dialog.Window != null)
            {
                Dialog.Window.SetBackgroundDrawable(new ColorDrawable(global::Android.Graphics.Color.Transparent));
                Dialog.Window.RequestFeature(WindowFeatures.NoTitle);
            }

            var view = inflater.Inflate(Resource.Layout.dialog_profile_info, container, false);

            _playerBox = view.FindViewById<PlayerBoxLongView>(Resource.Id.playerBox);
            _emailText = view.FindViewById<TextView>(Resource.Id.emailText);
            _numberText = view.FindViewById<TextView>(Resource.Id.numberText);
            _locationText = view.FindViewById<TextView>(Resource.Id.locationText);
            _nftCountText = view.FindViewById<TextView>(Resource.Id.nftCountText);
            _manageNftsBtn = view.FindViewById<global::Android.Views.View>(Resource.Id.manageNftsBtn);

            _statPlayed = view.FindViewById<StatisticCardView>(Resource.Id.statPlayed);
            _statWon = view.FindViewById<StatisticCardView>(Resource.Id.statWon);
            _statLost = view.FindViewById<StatisticCardView>(Resource.Id.statLost);
            _statBestWin = view.FindViewById<StatisticCardView>(Resource.Id.statBestWin);
            _statTotalWin = view.FindViewById<StatisticCardView>(Resource.Id.statTotalWin);
            _statTotalLost = view.FindViewById<StatisticCardView>(Resource.Id.statTotalLost);

            _statPlayed.SetTitle("GAMES PLAYED");
            _statWon.SetTitle("GAMES WON");
            _statLost.SetTitle("GAMES LOST");
            _statBestWin.SetTitle("BEST WIN");
            _statTotalWin.SetTitle("TOTAL WIN");
            _statTotalLost.SetTitle("TOTAL LOST");

            _manageNftsBtn.Click += OnManageNftsClicked;

            InitializeData();

            return view;
        }

        private void InitializeData()
        {
            if (UserInfo.Instance.player != null)
            {
                var p = UserInfo.Instance.player;
                _playerBox.SetPlayerName(p.Name);
                _emailText.Text = p.Email;
                _numberText.Text = p.PhoneNumber;
                _locationText.Text = p.City;

                UpdateStats(p.GamesPlayed, p.GamesWon, p.GamesLost, p.BestWin, p.TotalWin, p.TotalLost);
                _playerBox.SetScore(p.Score, p.PhoneNumber != "###########");

                // Load profile image
                if (!string.IsNullOrEmpty(p.PictureUrl))
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            var httpClient = new System.Net.Http.HttpClient();
                            var bytes = await httpClient.GetByteArrayAsync(p.PictureUrl);
                            var bitmap = await BitmapFactory.DecodeByteArrayAsync(bytes, 0, bytes.Length);
                            if (bitmap != null)
                            {
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    _playerBox.SetPlayerImageBitmap(bitmap);
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error loading profile image: {ex.Message}");
                        }
                    });
                }
            }

            Task.Run(async () => await LoadValuesAsync());
        }

        private void UpdateStats(int played, int won, int lost, decimal best, decimal totalWin, decimal totalLost)
        {
            _statPlayed.SetValue(played.ToString());
            _statWon.SetValue(won.ToString());
            _statLost.SetValue(lost.ToString());
            _statBestWin.SetValue(ClientGlobalConstants.NormalizeCoins(best));
            _statTotalWin.SetValue(ClientGlobalConstants.NormalizeCoins(totalWin));
            _statTotalLost.SetValue(ClientGlobalConstants.NormalizeCoins(totalLost));
        }

        private async Task LoadValuesAsync()
        {
            if (GlobalConstants.MatchMaker.Connected)
            {
                try
                {
                    PlayerInfo dto = await GlobalConstants.MatchMaker.UserConnectedSetID();
                    if (dto != null)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            UpdateStats(dto.GamesPlayed, dto.GamesWon, dto.GamesLost, dto.BestWin, dto.TotalWin, dto.TotalLost);
                            _playerBox.SetScore(dto.Score, dto.PhoneNumber != "###########" && !string.IsNullOrEmpty(dto.PhoneNumber));
                            if (dto.PhoneNumber != null)
                            {
                                _numberText.Text = dto.PhoneNumber;
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                string result = await GlobalConstants.MatchMaker.MintNFT(0);
                if (!string.IsNullOrWhiteSpace(result) && result.Contains("Success"))
                {
                    result = result.Replace(",Success", "");
                    string[] ids = result.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        _nftCountText.Text = ids.Length + " NFTS";
                    });
                }
            }
        }

        private void OnManageNftsClicked(object sender, EventArgs e)
        {
            if (!GlobalConstants.MatchMaker.Connected)
                return;
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            Dismiss();
            
            // Re-use the existing MAUI dashboard logic to show the minting page popup
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var mainPage = Microsoft.Maui.Controls.Application.Current?.MainPage;
                if (mainPage != null)
                {
                    mainPage.ShowPopup(ClientGlobalConstants.mintingPage);
                }
            });
        }
    }
}