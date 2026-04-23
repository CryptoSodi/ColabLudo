using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using AndroidX.Fragment.App;
using LudoClient.Constants;
using SharedCode.Constants;
using System.Text.Json;

namespace LudoClient.Platforms.Android.Popups
{
    public class DailyBonusDialogFragment : DialogFragment
    {
        private DailyBonusCardView[] _dayCards;
        private global::AndroidX.ConstraintLayout.Widget.ConstraintLayout _claimBtn;

        public override global::Android.Views.View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            if (Dialog != null && Dialog.Window != null)
            {
                Dialog.Window.SetBackgroundDrawable(new ColorDrawable(global::Android.Graphics.Color.Transparent));
                Dialog.Window.RequestFeature(WindowFeatures.NoTitle);
            }

            var view = inflater.Inflate(Resource.Layout.dialog_daily_bonus, container, false);

            _dayCards = new DailyBonusCardView[7];
            _dayCards[0] = view.FindViewById<DailyBonusCardView>(Resource.Id.D1);
            _dayCards[1] = view.FindViewById<DailyBonusCardView>(Resource.Id.D2);
            _dayCards[2] = view.FindViewById<DailyBonusCardView>(Resource.Id.D3);
            _dayCards[3] = view.FindViewById<DailyBonusCardView>(Resource.Id.D4);
            _dayCards[4] = view.FindViewById<DailyBonusCardView>(Resource.Id.D5);
            _dayCards[5] = view.FindViewById<DailyBonusCardView>(Resource.Id.D6);
            _dayCards[6] = view.FindViewById<DailyBonusCardView>(Resource.Id.D7);

            _claimBtn = view.FindViewById<AndroidX.ConstraintLayout.Widget.ConstraintLayout>(Resource.Id.claimBtn);
            _claimBtn.Click += ClaimDaily_Clicked;

            LoadAndFetchData();

            return view;
        }

        private void LoadAndFetchData()
        {
            // Initial load from Preferences
            var dto = LoadDailyBonus();
            if (dto != null)
                UpdateFromDto(dto);

            // Fetch latest from server
            Task.Run(async () => await FetchDailyBonusAsync());
        }

        private async Task FetchDailyBonusAsync()
        {
            try
            {
                var dto = await GlobalConstants.MatchMaker.GetDailyBonus<DailyBonusDto>().ConfigureAwait(false);
                if (dto != null)
                {
                    MainThread.BeginInvokeOnMainThread(() => UpdateFromDto(dto));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async void ClaimDaily_Clicked(object sender, EventArgs e)
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            try
            {
                var dto = await GlobalConstants.MatchMaker.ClaimTodayBonus<DailyBonusDto>().ConfigureAwait(false);
                if (dto != null)
                {
                    MainThread.BeginInvokeOnMainThread(() => UpdateFromDto(dto));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void UpdateFromDto(DailyBonusDto dto)
        {
            if (dto == null) return;

            bool showClaim = false;
            bool[] flags = new[] { dto.Day1, dto.Day2, dto.Day3, dto.Day4, dto.Day5, dto.Day6, dto.Day7 };
            int dc = dto.DayCounter;

            for (int i = 0; i < 7; i++)
            {
                string state;
                if (i < dc || (i == dc && flags[i]))
                    state = flags[i] ? "Claimed" : "Missed";
                else if (i == dc)
                {
                    state = "Active";
                    showClaim = true;
                }
                else
                    state = "InActive";

                _dayCards[i]?.Init($"Day {i + 1}", state, dto.Bonus);
            }

            _claimBtn.Visibility = showClaim ? ViewStates.Visible : ViewStates.Gone;
            
            SaveDailyBonus(dto);
        }

        private void SaveDailyBonus(DailyBonusDto dto)
        {
            if (dto == null) return;
            string json = JsonSerializer.Serialize(dto);
            Preferences.Default.Set("DailyBonusDto", json);
        }

        private DailyBonusDto LoadDailyBonus()
        {
            if (Preferences.Default.ContainsKey("DailyBonusDto"))
            {
                string json = Preferences.Default.Get("DailyBonusDto", string.Empty);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    try { return JsonSerializer.Deserialize<DailyBonusDto>(json); } catch { }
                }
            }
            return null;
        }

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
}