using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.ConstraintLayout.Widget;
using System;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;

namespace LudoClient.Platforms.Android.Popups
{
    [Register("ludoclient.platforms.android.Popups.PlayerBoxLongView")]
    public class PlayerBoxLongView : ConstraintLayout
    {
        private ImageView _playerImage;
        private TextView _playerNameText;
        private ImageView _verificationImage;
        private TextView _scoreText;
        private global::Android.Views.View _orangeBar;
        private TextView _reminderScoreText;
        private global::Android.Views.View _scoreBarBg;

        private int _remainderScore = 10;

        public PlayerBoxLongView(Context context) : base(context)
        {
            Initialize(context);
        }

        public PlayerBoxLongView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Initialize(context);
        }

        public PlayerBoxLongView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
            Initialize(context);
        }

        protected PlayerBoxLongView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        private void Initialize(Context context)
        {
            var inflater = LayoutInflater.FromContext(context);
            var view = inflater.Inflate(Resource.Layout.layout_player_box_long, this, true);

            _playerImage = view.FindViewById<ImageView>(Resource.Id.playerImage);
            _playerNameText = view.FindViewById<TextView>(Resource.Id.playerNameText);
            _verificationImage = view.FindViewById<ImageView>(Resource.Id.verificationImage);
            _scoreText = view.FindViewById<TextView>(Resource.Id.scoreText);
            _orangeBar = view.FindViewById<global::Android.Views.View>(Resource.Id.orangeBar);
            _reminderScoreText = view.FindViewById<TextView>(Resource.Id.reminderScoreText);
            _scoreBarBg = view.FindViewById<global::Android.Views.View>(Resource.Id.scoreBarBg);
        }

        public void SetPlayerName(string name)
        {
            if (_playerNameText != null)
                _playerNameText.Text = name;
        }

        public void SetPlayerImage(string url)
        {
            // Note: In a real app, use an image loading library like Glide or Picasso.
            // For now, we assume UserInfo.Instance.ProfileImageSource is handled or use native loading.
            if (_playerImage != null && !string.IsNullOrEmpty(url))
            {
                // Simple placeholder logic or direct URI if supported
                // _playerImage.SetImageURI(global::Android.Net.Uri.Parse(url));
            }
        }

        public void SetPlayerImageBitmap(global::Android.Graphics.Bitmap bitmap)
        {
            if (_playerImage != null)
                _playerImage.SetImageBitmap(bitmap);
        }

        public void SetScore(int score, bool verified)
        {
            _remainderScore = score % 10000;
            int dividedScore = score / 10000;
            
            if (_scoreText != null)
                _scoreText.Text = dividedScore.ToString();

            if (_verificationImage != null)
            {
                _verificationImage.SetImageResource(verified ? Resource.Drawable.lbl_verified : Resource.Drawable.lbl_unverified);
            }

            UpdateOrangeBarWidth();
        }

        private void UpdateOrangeBarWidth()
        {
            _scoreBarBg?.Post(() =>
            {
                int fullWidth = _scoreBarBg.Width;
                if (fullWidth > 0)
                {
                    float ratio = _remainderScore / 10000.0f;
                    int targetWidth = (int)(fullWidth * ratio);

                    var lp = _orangeBar.LayoutParameters;
                    lp.Width = targetWidth;
                    _orangeBar.LayoutParameters = lp;

                    if (_reminderScoreText != null)
                        _reminderScoreText.Text = _remainderScore.ToString();
                }
            });
        }
    }
}