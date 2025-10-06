using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using LudoClient.Constants;
using LudoClient.ControlView;
using SharedCode.Constants;
using System.Net.Http.Json;

namespace LudoClient.Popups;

public partial class MintingPage : BasePopup
{
    int amount = 1;
    private System.Timers.Timer nftTimer;
    public MintingPage()
	{
		InitializeComponent();
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ProcessNFT(0);
        });
        StartNFTTimer();
    }
    private void StartNFTTimer()
    {
        nftTimer = new System.Timers.Timer(30000); // 30 seconds = 30000 ms
        nftTimer.Elapsed += (s, e) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ProcessNFT(0);
            });
        };
        nftTimer.AutoReset = true;
        nftTimer.Start();
    }
    private void BtnMinus(object sender, EventArgs e)
    {
        if (!GlobalConstants.MatchMaker.Connected)
            return;
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        if (amount > 1)
            amount--;
        Cost.Text = $"Cost : {amount} X 10000 = {amount * 1000} LUDC";
        EntryLabel.Text = amount.ToString();
    }
    private void BtnPlus(object sender, EventArgs e)
    {
        if (!GlobalConstants.MatchMaker.Connected)
            return;
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        if(UserInfo.Instance.player.Wallet.AvailableBalance > (amount+1) * 1000)
        {
            amount++;
            Cost.Text = $"Cost : {amount} X 100 = {amount * 100} LUDC";

            EntryLabel.Text = amount.ToString();
        }
    }
    private async void Mint_Clicked(object sender, EventArgs e)
    {
        if (!GlobalConstants.MatchMaker.Connected)
            return;
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        ProcessNFT(amount);
    }
    // ---------------- NFT Mint + UI Build ----------------
    //182,184,Success
    private async Task ProcessNFT(int amount)
    {
        string result = await GlobalConstants.MatchMaker.MintNFT(amount);

        if (string.IsNullOrWhiteSpace(result))
        {
            await Toast.Make("No result from minting.", ToastDuration.Long, 24).Show();
            return;
        }

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (result.Contains("Success"))
            {
                result = result.Replace(",Success", "");
                string[] ids = result.Split(',', StringSplitOptions.RemoveEmptyEntries);

                List<Task<CityNFT?>> loadTasks = new();

                foreach (var idStr in ids)
                {
                    if (int.TryParse(idStr, out int id))
                        loadTasks.Add(LoadNFTAsync(id));
                }

                // Wait for all tasks to finish
                var loadedNFTs = await Task.WhenAll(loadTasks);

                // Filter out nulls
                var validNFTs = loadedNFTs.Where(nft => nft != null).ToList();

                if (validNFTs.Count > 0)
                {
                    BuildNFTCards(validNFTs);
                   // await Toast.Make($"Loaded {validNFTs.Count} NFTs successfully.", ToastDuration.Short, 24).Show();
                }
                else
                {
                    await Toast.Make("No valid NFTs found.", ToastDuration.Long, 24).Show();
                }
            }
            else
            {
                await Toast.Make("Error Minting NFT!", ToastDuration.Long, 24).Show();
            }
        });
    }

    // ---------------- Load NFTs from Server ----------------

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
    // ---------------- Build Dynamic UI ----------------
    private const int CardsPerRow = 3;

    private void BuildNFTCards(List<CityNFT> nfts)
    {
        MainContainer.Children.Clear();
        StackLayout? currentRow = null;

        for (int i = 0; i < nfts.Count; i++)
        {
            if (i % CardsPerRow == 0)
            {
                currentRow = new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    Spacing = 8,
                    HorizontalOptions = LayoutOptions.FillAndExpand
                };
                MainContainer.Children.Add(currentRow);
            }

            var nft = nfts[i];
            var card = new StatisticCard
            {
                Title = nft.Name,
                WidthRequest=85,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };
            card.setValue(nft.Volume + "");
            // Optional: call your StatisticCard.SetStats() if implemented
            // card.SetStats(nft.Population, nft.Users, nft.Volume, nft.Games_Played, nft.Tournaments_Played);
            // card.ImageSource = nft.Image;

            currentRow?.Children.Add(card);
        }
    }
}
public class CityNFT
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Image { get; set; }
    public int Population { get; set; }
    public int Users { get; set; }
    public double Volume { get; set; }
    public int Games_Played { get; set; }
    public int Tournaments_Played { get; set; }
    public string Contract { get; set; } = "";
    public int Token_Id { get; set; }
}