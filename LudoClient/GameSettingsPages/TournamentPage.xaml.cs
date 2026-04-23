using LudoClient.Constants;
using LudoClient.ControlView;
using LudoClient.Models;
using SharedCode;
using SharedCode.Constants;
using System.Collections.Generic;
using System.Text.Json;
#if ANDROID
using LudoClient.Platforms.Android;
#endif
namespace LudoClient;
public partial class TournamentPage : ContentPage
{
    private List<TournamentDTO>? _allTournaments;
    private Task<List<TournamentDTO>>? _tournamentLoadTask;
    private int _loadVersion;
    private string _activeTabType = "Local";

    public TournamentPage()
    {
        InitializeComponent();
        Tab1.SwitchSource = Tab1.SwitchOn;
        Tab2.SwitchSource = Tab2.SwitchOff;
        Tab3.SwitchSource = Tab3.SwitchOff;
        Tab4.SwitchSource = Tab4.SwitchOff;

        MainThread.BeginInvokeOnMainThread(async () => await InitializeTournamentsAsync("Local"));
    }

    public async Task InitializeTournamentsAsync(string tabType)
    {
        _activeTabType = tabType;
        var version = ++_loadVersion;
        ReleaseCurrentCards();

        if (_allTournaments == null)
            _allTournaments = await GetTournamentCacheAsync();

        if (version != _loadVersion)
            return;

        var tournaments = FilterTournaments(_allTournaments ?? new List<TournamentDTO>(), tabType);
        await PopulateTournamentsAsync(tournaments, version);
    }

    private List<TournamentDTO> FilterTournaments(List<TournamentDTO> tournaments, string tabType)
    {
        if (tabType == "Local")
        {
            var userCity = UserInfo.Instance.player.City?.Trim() ?? string.Empty;
            return tournaments
                .Where(t => string.Equals(t.City?.Trim(), userCity, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (tabType == "Global")
        {
            return tournaments.ToList();
        }

        if (tabType == "Ended")
        {
            return tournaments
                .Where(t => t.EndDate <= t.ServerDateTime)
                .ToList();
        }

        if (tabType == "Active")
        {
            return tournaments
                .Where(t => t.ServerDateTime >= t.StartDate &&
                            t.ServerDateTime <= t.EndDate)
                .ToList();
        }

        return tournaments.ToList();
    }

    private Task<List<TournamentDTO>> GetTournamentCacheAsync()
    {
        _tournamentLoadTask ??= GlobalConstants.MatchMaker.GetAllTournaments("All");
        return _tournamentLoadTask;
    }

    protected override void OnDisappearing()
    {
        _loadVersion++;
        ReleaseCurrentCards();
        base.OnDisappearing();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (TournamentListStack.Children.Count == 0)
            await InitializeTournamentsAsync(_activeTabType);
    }

    private async Task PopulateTournamentsAsync(List<TournamentDTO> tournaments, int version)
    {
        for (var i = 0; i < tournaments.Count; i++)
        {
            if (version != _loadVersion)
                return;

#if ANDROID
            TournamentListStack.Children.Add(new NativeTournamentCard(tournaments[i]));
#else
            TournamentListStack.Children.Add(new TournamentDetailList(tournaments[i]));
#endif

            if (i % 4 == 3)
                await Task.Yield();
        }
    }

    private void ReleaseCurrentCards()
    {
        foreach (var child in TournamentListStack.Children.OfType<TournamentDetailList>())
            child.Release();

#if ANDROID
        foreach (var child in TournamentListStack.Children.OfType<NativeTournamentCard>())
            child.Handler?.DisconnectHandler();
#endif

        TournamentListStack.Children.Clear();
    }

    private async void TabRequestedActivate(object sender, EventArgs e)
    {
        if (sender is ImageSwitch activeTab)
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            Tab1.SwitchSource = Tab1 == activeTab ? Tab1.SwitchOn : Tab1.SwitchOff;
            Tab2.SwitchSource = Tab2 == activeTab ? Tab2.SwitchOn : Tab2.SwitchOff;
            Tab3.SwitchSource = Tab3 == activeTab ? Tab3.SwitchOn : Tab3.SwitchOff;
            Tab4.SwitchSource = Tab4 == activeTab ? Tab4.SwitchOn : Tab4.SwitchOff;
            // Add logic here to change the content based on the active tab
            // 1) Note which tab is active (for example, store an index)
            if (activeTab == Tab1)
                await InitializeTournamentsAsync("Local");
            else if (activeTab == Tab2)
                await InitializeTournamentsAsync("Global");
            else if (activeTab == Tab3)
                await InitializeTournamentsAsync("Active");
            else // activeTab == Tab3
                await InitializeTournamentsAsync("Ended");
        }
    }
}
