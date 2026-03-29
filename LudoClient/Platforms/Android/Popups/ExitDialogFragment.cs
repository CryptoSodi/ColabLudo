using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;

namespace LudoClient.Platforms.Android.Popups
{
    public class ExitDialogFragment : DialogFragment
    {
        private readonly Action _onExit;

        public ExitDialogFragment(Action onExit)
        {
            _onExit = onExit;
        }

        public override global::Android.Views.View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Remove the default dialog title/border
            Dialog.Window.RequestFeature(WindowFeatures.NoTitle);
            Dialog.Window.SetBackgroundDrawable(new ColorDrawable(global::Android.Graphics.Color.Transparent));

            var view = inflater.Inflate(Resource.Layout.dialog_exit_confirm, container, false);

            var btnCancel = view.FindViewById<global::Android.Views.View>(Resource.Id.btnCancel);
            var btnExit = view.FindViewById<global::Android.Views.View>(Resource.Id.btnExit);

            btnCancel.Click += (s, e) => Dismiss();
            
            btnExit.Click += (s, e) =>
            {
                _onExit?.Invoke();
                Dismiss();
            };

            return view;
        }

        public override void OnStart()
        {
            base.OnStart();
            // Ensure the dialog is large enough to show the content
            if (Dialog != null)
            {
                Dialog.Window.SetLayout(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            }
        }
    }
}
