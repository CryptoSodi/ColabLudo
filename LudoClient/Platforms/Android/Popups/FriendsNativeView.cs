using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.ConstraintLayout.Widget;
using LudoClient.Constants;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Maui.ApplicationModel;
using SharedCode;
using SharedCode.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LudoClient.Platforms.Android.Popups
{
    [Register("ludoclient.platforms.android.Popups.FriendsNativeView")]
    public class FriendsNativeView : ConstraintLayout
    {
        private LinearLayout _friendsListContainer;
        private global::Android.Views.View _tabFriends;
        private global::Android.Views.View _tabBlocked;
        private ImageView _imgTabFriends;
        private ImageView _imgTabBlocked;
        
        private string _filter = "Normal";

        public FriendsNativeView(Context context) : base(context)
        {
            this.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            Initialize(context);
        }

        public FriendsNativeView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Initialize(context);
        }

        public FriendsNativeView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
            Initialize(context);
        }

        protected FriendsNativeView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        private void Initialize(Context context)
        {
            var inflater = LayoutInflater.FromContext(context);
            var view = inflater.Inflate(Resource.Layout.dialog_friends, this, true);

            _friendsListContainer = view.FindViewById<LinearLayout>(Resource.Id.FriendsListContainer);
            _tabFriends = view.FindViewById<global::Android.Views.View>(Resource.Id.tabFriends);
            _tabBlocked = view.FindViewById<global::Android.Views.View>(Resource.Id.tabBlocked);
            _imgTabFriends = view.FindViewById<ImageView>(Resource.Id.imgTabFriends);
            _imgTabBlocked = view.FindViewById<ImageView>(Resource.Id.imgTabBlocked);

            _tabFriends.Click += (s, e) => ActivateTab("Normal");
            _tabBlocked.Click += (s, e) => ActivateTab("BLOCK");

            // Use Post to ensure the view is laid out before scaling
            this.Post(() => {
                ApplyProportionalScaling(this, Resource.Id.mainPopupContainer, Resource.Drawable.wallet_bg);
            });

            InitializeFriendsAsync();
        }

        private void ActivateTab(string filter)
        {
            Constants.ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            _filter = filter;
            
            if (filter == "Normal")
            {
                _imgTabFriends.SetImageResource(Resource.Drawable.tab_active);
                _imgTabBlocked.SetImageResource(Resource.Drawable.tab_normal);
            }
            else
            {
                _imgTabFriends.SetImageResource(Resource.Drawable.tab_normal);
                _imgTabBlocked.SetImageResource(Resource.Drawable.tab_active);
            }

            InitializeFriendsAsync();
        }

        public async void InitializeFriendsAsync()
        {
            try 
            {
                // Wait for connection if needed
                int retry = 0;
                while ((GlobalConstants.MatchMaker == null || !GlobalConstants.MatchMaker.Connected) && retry < 10)
                {
                    await Task.Delay(1000);
                    retry++;
                }

                List<PlayerCard> playerCards = await GetPlayerCards();
                
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (_friendsListContainer == null) return;
                    _friendsListContainer.RemoveAllViews();
                    foreach (var pc in playerCards)
                    {
                        var friendView = new FriendDetailView(Context);
                        friendView.SetDetails(pc, "Friend");
                        _friendsListContainer.AddView(friendView);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FriendsNativeView] Error initializing friends: {ex}");
            }
        }

        private async Task<List<PlayerCard>> GetPlayerCards()
        {
            try
            {
                if (GlobalConstants.MatchMaker?._hubConnection == null) return new List<PlayerCard>();
                
                List<PlayerCard> friends = await GlobalConstants.MatchMaker._hubConnection.InvokeAsync<List<PlayerCard>>("GetFriends", "All").ConfigureAwait(false);
                if (_filter == "BLOCK")
                    friends = friends.Where(f => f.status == "BLOCK").ToList();
                else
                    friends = friends.Where(f => f.status != "BLOCK").ToList();
                return friends;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<PlayerCard>();
            }
        }

        private void ApplyProportionalScaling(global::Android.Views.View view, int containerId, int drawableId)
        {
            var container = view.FindViewById<ConstraintLayout>(containerId);
            var drawable = global::Android.App.Application.Context.GetDrawable(drawableId);

            if (container != null && drawable != null && container.Parent is ConstraintLayout parentConstraint)
            {
                string ratioString = $"{drawable.IntrinsicWidth}:{drawable.IntrinsicHeight}";
                var set = new ConstraintSet();
                set.Clone(parentConstraint);
                set.SetDimensionRatio(containerId, ratioString);
                set.ApplyTo(parentConstraint);
            }
        }
    }
}