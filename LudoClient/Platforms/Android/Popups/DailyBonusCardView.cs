using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.ConstraintLayout.Widget;
using Android.Graphics.Drawables;
using System;

namespace LudoClient.Platforms.Android.Popups
{
    [Register("ludoclient.platforms.android.DailyBonusCardView")]
    public class DailyBonusCardView : ConstraintLayout
    {
        private global::Android.Views.View _cardBgColor;
        private ImageView _cardBgImage;
        private TextView _dayText;
        private TextView _bonusText;

        public DailyBonusCardView(Context context) : base(context)
        {
            Initialize(context);
        }

        public DailyBonusCardView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Initialize(context);
        }

        public DailyBonusCardView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
            Initialize(context);
        }

        protected DailyBonusCardView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        private void Initialize(Context context)
        {
            var inflater = LayoutInflater.FromContext(context);
            var view = inflater.Inflate(Resource.Layout.item_daily_bonus_card, this, true);

            _cardBgColor = view.FindViewById<global::Android.Views.View>(Resource.Id.cardBgColor);
            _cardBgImage = view.FindViewById<ImageView>(Resource.Id.cardBgImage);
            _dayText = view.FindViewById<TextView>(Resource.Id.dayText);
            _bonusText = view.FindViewById<TextView>(Resource.Id.bonusText);
        }

        public void Init(string day, string state, int bonus)
        {
            _dayText.Text = day;
            _bonusText.Text = bonus.ToString();

            // Handle background color tinting for rounded corners
            var bgDrawable = _cardBgColor.Background as GradientDrawable;
            
            switch (state)
            {
                case "Claimed":
                    _cardBgImage.SetImageResource(Resource.Drawable.days_current_bg);
                    bgDrawable?.SetColor(global::Android.Graphics.Color.ParseColor("#008000")); // Green
                    break;
                case "InActive":
                    _cardBgImage.SetImageResource(Resource.Drawable.days_current_bg);
                    bgDrawable?.SetColor(global::Android.Graphics.Color.ParseColor("#FFFFFF")); // White
                    break;
                case "Active":
                    _cardBgImage.SetImageResource(Resource.Drawable.days_gold_bg);
                    bgDrawable?.SetColor(global::Android.Graphics.Color.ParseColor("#DAA520")); // Goldenrod
                    break;
                case "Missed":
                    _cardBgImage.SetImageResource(Resource.Drawable.days_gray_bg);
                    bgDrawable?.SetColor(global::Android.Graphics.Color.ParseColor("#808080")); // Gray
                    break;
            }
        }
    }
}