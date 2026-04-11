using Android.Content;
using System;

namespace LudoClient.Platforms.Android
{
    public static class WalletLauncher
    {
        public const string SchemeMobileWalletAdapter = "solana-wallet";
        public const string LocalPathSuffix = "v1/associate/local";

        public static void Launch(string associationToken, int port)
        {
            try
            {
                var activity = global::Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
                if (activity == null) return;

                var intent = new Intent();
                intent.SetAction(Intent.ActionView);
                intent.AddCategory(Intent.CategoryBrowsable);

                var url = $"{SchemeMobileWalletAdapter}:/" +
                          $"{LocalPathSuffix}?association={associationToken}&port={port}";

                intent.SetData(global::Android.Net.Uri.Parse(url));
                
                activity.StartActivity(intent);
                Console.WriteLine($"[WalletLauncher] Successfully launched intent: {url}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WalletLauncher] Error launching wallet: {ex.Message}");
            }
        }
    }
}
