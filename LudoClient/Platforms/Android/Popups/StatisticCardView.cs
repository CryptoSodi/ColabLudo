using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.ConstraintLayout.Widget;
using System;

namespace LudoClient.Platforms.Android.Popups
{
    [Register("ludoclient.platforms.android.Popups.StatisticCardView")]
    public class StatisticCardView : ConstraintLayout
    {
        private TextView _titleText;
        private TextView _valueText;

        public StatisticCardView(Context context) : base(context)
        {
            Initialize(context);
        }

        public StatisticCardView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Initialize(context);
        }

        public StatisticCardView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
            Initialize(context);
        }

        protected StatisticCardView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        private void Initialize(Context context)
        {
            var inflater = LayoutInflater.FromContext(context);
            var view = inflater.Inflate(Resource.Layout.item_statistic_card, this, true);

            _titleText = view.FindViewById<TextView>(Resource.Id.titleText);
            _valueText = view.FindViewById<TextView>(Resource.Id.valueText);
        }

        public void SetTitle(string title)
        {
            if (_titleText != null)
                _titleText.Text = title;
        }

        public void SetValue(string value)
        {
            if (_valueText != null)
                _valueText.Text = value;
        }
    }
}