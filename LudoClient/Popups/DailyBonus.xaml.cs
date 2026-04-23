using LudoClient.Constants;
using SharedCode.Constants;
using System.Text.Json;
namespace LudoClient.Popups;
public partial class DailyBonus : BasePopup
{
    public DailyBonus()
    {
        InitializeComponent();        
    }
    public void DailyBonus_Loaded(object sender, EventArgs e)
    {
        DailyBonusDto dto = LoadDailyBonus();
        if (dto != null)
            UpdateFromDto(dto);
        FetchDailyBonusAsync();
    }
    public void SaveDailyBonus(DailyBonusDto dto)
    {
        if (dto == null) return;
        string json = JsonSerializer.Serialize(dto);
        Preferences.Default.Set("DailyBonusDto", json);
    }
    public DailyBonusDto LoadDailyBonus()
    {
        if (Preferences.Default.ContainsKey("DailyBonusDto"))
        {
            string json = Preferences.Default.Get("DailyBonusDto", string.Empty);
            if (!string.IsNullOrWhiteSpace(json))
            {
                return JsonSerializer.Deserialize<DailyBonusDto>(json);
            }
        }

        return null; // Or return a default
    }
    // Fetch current daily bonus state
    private async Task FetchDailyBonusAsync()
    {
        DailyBonusDto dto = null;
        try
        {
            dto = await GlobalConstants.MatchMaker.GetDailyBonus<DailyBonusDto>().ConfigureAwait(false);
            UpdateFromDto(dto);
        }
        catch (Exception ex)
        {
            // Handle exceptions (e.g. show alert)
            Console.WriteLine(ex);
            if (ex.Message == "" && dto != null)
                UpdateFromDto(dto);
        }
    }
    private async void ClaimDaily_Clicked(object sender, EventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        try
        {
            var dto = await GlobalConstants.MatchMaker.ClaimTodayBonus<DailyBonusDto>().ConfigureAwait(false);
            UpdateFromDto(dto);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
    private void UpdateFromDto(DailyBonusDto dto)
    {
        if (dto == null)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            bool ShowClaim = false;
            // Day flags array for ease of indexing
            bool[] flags = new[] { dto.Day1, dto.Day2, dto.Day3, dto.Day4, dto.Day5, dto.Day6, dto.Day7 };
            int dc = dto.DayCounter; // 0-based index of current day

            // Loop through 7 days
            for (int i = 0; i < 7; i++)
            {
                string state;
                if (i < dc || (i == dc && flags[i]))
                    state = flags[i] ? "Claimed" : "Missed";
                else if (i == dc)
                {
                    state = "Active";
                    ShowClaim = true;
                }
                else
                    state = "InActive";

                // Select the appropriate card
                switch (i)
                {
                    case 0: D1.init($"Day {i + 1}", state, dto.Bonus); break;
                    case 1: D2.init($"Day {i + 1}", state, dto.Bonus); break;
                    case 2: D3.init($"Day {i + 1}", state, dto.Bonus); break;
                    case 3: D4.init($"Day {i + 1}", state, dto.Bonus); break;
                    case 4: D5.init($"Day {i + 1}", state, dto.Bonus); break;
                    case 5: D6.init($"Day {i + 1}", state, dto.Bonus); break;
                    case 6: D7.init($"Day {i + 1}", state, dto.Bonus); break;
                }
            }
            ClaimBTN.IsVisible = ShowClaim;
        });
        SaveDailyBonus(dto);
    }
    // DTO matching the server-side definition
    public class DailyBonusDto
    {
        public int DailyBonusId { get; set; }
        public int PlayerId { get; set; }
        public bool Day1 { get; set; }
        public bool Day2 { get; set; }
        public bool Day3 { get; set; }
        public bool Day4 { get; set; }
        public bool Day5 { get; set; }
        public bool Day6 { get; set; }
        public bool Day7 { get; set; }
        public int Bonus { get; set; }
        public int DayCounter { get; set; }
    }
}
