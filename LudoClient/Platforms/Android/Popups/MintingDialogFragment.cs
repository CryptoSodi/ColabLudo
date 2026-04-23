using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using LudoClient.Constants;
using Microsoft.Maui.ApplicationModel;
using SharedCode.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Timers;

namespace LudoClient.Platforms.Android.Popups
{
    public class MintingDialogFragment : DialogFragment
    {
        private LinearLayout _mainContainer;
        private TextView _entryLabel;
        private TextView _costText;
        private global::Android.Views.View _btnMint;
        private global::Android.Widget.ImageButton _btnMinus;
        private global::Android.Widget.ImageButton _btnPlus;

        private int _amount = 1;
        private System.Timers.Timer _nftTimer;
        private const int CardsPerRow = 3;

        public override global::Android.Views.View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            if (Dialog != null && Dialog.Window != null)
            {
                Dialog.Window.SetBackgroundDrawable(new ColorDrawable(global::Android.Graphics.Color.Transparent));
                Dialog.Window.RequestFeature(WindowFeatures.NoTitle);
            }

            var view = inflater.Inflate(Resource.Layout.dialog_minting, container, false);

            _mainContainer = view.FindViewById<LinearLayout>(Resource.Id.MainContainer);
            _entryLabel = view.FindViewById<TextView>(Resource.Id.entryLabel);
            _costText = view.FindViewById<TextView>(Resource.Id.costText);
            _btnMint = view.FindViewById<global::Android.Views.View>(Resource.Id.btnMint);
            _btnMinus = view.FindViewById<global::Android.Widget.ImageButton>(Resource.Id.btnMinus);
            _btnPlus = view.FindViewById<global::Android.Widget.ImageButton>(Resource.Id.btnPlus);

            _btnMinus.Click += OnBtnMinusClicked;
            _btnPlus.Click += OnBtnPlusClicked;
            _btnMint.Click += OnBtnMintClicked;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _ = ProcessNFT(0);
            });

            StartNFTTimer();

            return view;
        }

        private void StartNFTTimer()
        {
            _nftTimer = new System.Timers.Timer(10000);
            _nftTimer.Elapsed += (s, e) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _ = ProcessNFT(0);
                });
            };
            _nftTimer.AutoReset = true;
            _nftTimer.Start();
        }

        private void OnBtnMinusClicked(object sender, EventArgs e)
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            if (_amount > 1)
                _amount--;
            UpdateControlUI();
        }

        private void OnBtnPlusClicked(object sender, EventArgs e)
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            if (UserInfo.Instance.player.Wallet.AvailableBalance > (_amount + 1) * 1000)
            {
                _amount++;
                UpdateControlUI();
            }
        }

        private void UpdateControlUI()
        {
            _entryLabel.Text = _amount.ToString();
            _costText.Text = $"Cost : {_amount} X 100 = {_amount * 100} LUDC";
        }

        private async void OnBtnMintClicked(object sender, EventArgs e)
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            await ProcessNFT(_amount);
        }

        private async Task ProcessNFT(int amount)
        {
            string result = await GlobalConstants.MatchMaker.MintNFT(amount);

            if (string.IsNullOrWhiteSpace(result))
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    CommunityToolkit.Maui.Alerts.Toast.Make("No result from minting.", ToastDuration.Long, 24).Show();
                });
                return;
            }

            if (result.Contains("Success"))
            {
                if (amount > 0)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        CommunityToolkit.Maui.Alerts.Toast.Make("Success!", ToastDuration.Short, 22).Show();
                    });
                }

                result = result.Replace(",Success", "");
                string[] ids = result.Split(',', StringSplitOptions.RemoveEmptyEntries);

                List<Task<CityNFT?>> loadTasks = new();
                foreach (var idStr in ids)
                {
                    if (int.TryParse(idStr, out int id))
                        loadTasks.Add(LoadNFTAsync(id));
                }

                var loadedNFTs = await Task.WhenAll(loadTasks);
                var validNFTs = loadedNFTs.Where(nft => nft != null).Select(nft => nft!).ToList();

                if (validNFTs.Count > 0)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (amount > 0)
                            CommunityToolkit.Maui.Alerts.Toast.Make($"Loaded {validNFTs.Count} NFTs successfully.", ToastDuration.Short, 22).Show();
                        BuildNFTCards(validNFTs);
                    });
                }
            }
            else
            {
                if (amount > 0)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        CommunityToolkit.Maui.Alerts.Toast.Make("Error Minting NFT!", ToastDuration.Short, 22).Show();
                    });
                }
            }
        }

        public async Task<CityNFT?> LoadNFTAsync(int id)
        {
            using var client = new HttpClient();
            string url = $"https://ludocities.com/mint/{id}.json";
            try
            {
                return await client.GetFromJsonAsync<CityNFT>(url);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading NFT {id}: {ex.Message}");
                return null;
            }
        }

        private void BuildNFTCards(List<CityNFT> nfts)
        {
            _mainContainer.RemoveAllViews();
            LinearLayout? currentRow = null;

            for (int i = 0; i < nfts.Count; i++)
            {
                if (i % CardsPerRow == 0)
                {
                    currentRow = new LinearLayout(Context)
                    {
                        Orientation = Orientation.Horizontal,
                        LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
                    };
                    _mainContainer.AddView(currentRow);
                }

                var nft = nfts[i];
                var card = new StatisticCardView(Context)
                {
                    LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1.0f)
                };
                card.SetTitle(nft.Name);
                card.SetValue(nft.Volume.ToString());

                currentRow?.AddView(card);
            }
        }

        public override void OnDestroyView()
        {
            _nftTimer?.Stop();
            _nftTimer?.Dispose();
            base.OnDestroyView();
        }
    }

    public class CityNFT
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Image { get; set; } = "";
        public int Population { get; set; }
        public int Users { get; set; }
        public double Volume { get; set; }
        public int Games_Played { get; set; }
        public int Tournaments_Played { get; set; }
        public string Contract { get; set; } = "";
        public int Token_Id { get; set; }
    }
}
