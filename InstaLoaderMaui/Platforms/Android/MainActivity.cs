using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Ads.Interstitial;
using Android.Gms.Ads;
using Android.OS;
using Android.Util;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Xamarin.Google.UserMesssagingPlatform;
using Plugin.MauiMTAdmob;
using UraniumUI.Material.Controls;
using Android.BillingClient.Api;
using System.Collections.Immutable;
using AndroidHUD;
using Plugin.MauiMTAdmob.Controls;
using static InstaLoaderMaui.MainPage;
using Firebase.Analytics;
using MPowerKit.ProgressRing;

namespace InstaLoaderMaui;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, Exported = true, LaunchMode = LaunchMode.SingleInstance, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter(new[] { Intent.ActionSend },
          Categories = new[] {
              Intent.CategoryDefault
          },
          DataMimeType = "*/*")]
[MetaData(name: "com.google.android.play.billingclient.version", Value = "7.1.1")]
public class MainActivity : MauiAppCompatActivity, IPurchasesUpdatedListener
{
    private static string Tag = nameof(MainActivity);

    public static FinishReceiver MFinishReceiver = new();
    public static DownloadReceiver MDownloadReceiver = new();

    public BillingClient MBillingClient;
    public BillingFlowParams MBillingFlowParams;
    public IConsentInformation consentInformation;
    private IConsentForm googleUMPConsentForm = null;
    private IConsentInformation googleUMPConsentInformation = null;

    public static MainActivity ActivityCurrent { get; set; }
    public MainActivity()
    {
        ActivityCurrent = this;
    }

    protected override async void OnCreate(Bundle? savedInstanceState)
    {
        Console.WriteLine($"{Tag}: OnCreate");
        base.OnCreate(savedInstanceState);
        Platform.Init(this, savedInstanceState);

        // Fixes "strict-mode" error when fetching webpage... idek..
        StrictMode.ThreadPolicy policy = new StrictMode.ThreadPolicy.Builder().PermitAll().Build();
        StrictMode.SetThreadPolicy(policy);

        /*// log ANRs
        StrictMode.SetVmPolicy(new StrictMode.VmPolicy.Builder()
                       .DetectAll()
                       .PenaltyLog()
                       //.PenaltyDeath()
                       .Build());
        */

        AskPermissions();

        LoadBillingClient();

        // look for intent data
        Intent intent = this.Intent;
        if (intent != null)
        {
            var data = intent.GetStringExtra(Intent.ExtraText);
            if (data != null)
            {
                Console.WriteLine($"{Tag}: received data from intent: {data}");

                MainPage mp = (MainPage)Shell.Current.CurrentPage;
                await mp.ClearTextfield();
                await mp.ShowEmptyUI();

                MIsShared = true;

                string SharedText = data.ToString();
                TextField mTextField = (TextField)mp.FindByName("main_textfield");
                if (mTextField != null)
                {
                    mTextField.Text = SharedText;
                }
                else
                {
                    Console.WriteLine($"{Tag} null textfield!");
                }
            }
        }
    }

    protected override void OnResume()
    {
        base.OnResume();

        HandlePendingTransactions();
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);

        if (intent != null)
        {
            Console.WriteLine($"{Tag}: received new intent");
            var data = intent.GetStringExtra(Intent.ExtraText);
            if (data != null)
            {
                Console.WriteLine($"{Tag}: received data from new intent: {data}");

                MainPage mp = (MainPage)Shell.Current.CurrentPage;
                mp.ClearTextfield();
                mp.ShowEmptyUI();

                MainPage.MIsShared = true;

                string SharedText = data.ToString();
                TextField mTextField = (TextField)mp.FindByName("main_textfield");
                if (mTextField != null)
                {
                    mTextField.Text = SharedText;
                }
            }
        }


        string shareText = Intent.GetStringExtra(Intent.ExtraText);


    }

    private void AskPermissions()
    {
        if ((int)Build.VERSION.SdkInt >= 33
            && ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.ReadMediaVideo) != Permission.Granted)
        {
            ActivityCompat.RequestPermissions(
                MainActivity.ActivityCurrent, new string[] { Android.Manifest.Permission.ReadMediaVideo }, 101);

        }
        if ((int)Build.VERSION.SdkInt >= 33
            && ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.ReadMediaImages) != Permission.Granted)
        {
            ActivityCompat.RequestPermissions(
                MainActivity.ActivityCurrent, new string[] { Android.Manifest.Permission.ReadMediaImages }, 101);

        }
        else if ((int)Build.VERSION.SdkInt < 33
            && ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.WriteExternalStorage) != Permission.Granted)
        {
            ActivityCompat.RequestPermissions(
            MainActivity.ActivityCurrent, new string[] { Android.Manifest.Permission.ReadExternalStorage, Android.Manifest.Permission.WriteExternalStorage }, 101);
        }
    }

    // ADMOB
    public void LoadAdmob()
    {
        // log event
        // TODO move to after gold check
        try
        {
            Bundle bundle = new Bundle();
            bundle.PutString("admob", "load");
            bundle.PutString("app_name", "instaloader");
            FirebaseAnalytics.GetInstance((MainActivity)Platform.CurrentActivity).LogEvent("admob_load", bundle);
        }
        catch (Exception e)
        {
            Console.WriteLine($"{Tag} failed to log event: {e.Message}");
        }

        if (Preferences.Default.Get("IS_GOLD", false))
        {
            Console.WriteLine($"{Tag} Skipping LoadAdmob");
            return;
        }
        Console.WriteLine($"{Tag} LoadAdmob");

        CrossMauiMTAdmob.Current.Init(this, AdmobIdApp);
        CrossMauiMTAdmob.Current.UserPersonalizedAds = true;
        SetGDPR();
        LoadBannerAd();
        if (!CrossMauiMTAdmob.Current.IsInterstitialLoaded())
        {
            CrossMauiMTAdmob.Current.LoadInterstitial(MainPage.admobIdInter);
        }
    }

    public void LoadBannerAd()
    {
        Console.WriteLine($"{Tag} LoadBannerAd");
        ((MTAdView)((MainPage)Shell.Current.CurrentPage).FindByName("banner_ad"))
            .LoadAd();

    }

    private void SetGDPR()
    {
        Console.WriteLine("starting consent management flow");
        try
        {
#if DEBUG
            Log.Info(Tag, "running DEBUG branch");
            var debugSettings = new ConsentDebugSettings.Builder(MainActivity.ActivityCurrent)
            .SetDebugGeography(ConsentDebugSettings
                    .DebugGeography
                    .DebugGeographyEea)
            //.AddTestDeviceHashedId("see logcat...")
            .AddTestDeviceHashedId(Android.Provider.Settings.Secure.GetString(((MainActivity)Platform.CurrentActivity).ContentResolver,
                                    Android.Provider.Settings.Secure.AndroidId))
            .Build();
#endif

            var requestParameters = new ConsentRequestParameters
                .Builder()
                .SetTagForUnderAgeOfConsent(false)
#if DEBUG
        .SetConsentDebugSettings(debugSettings)
#endif
                .Build();
            MainActivity ma = (MainActivity)Platform.CurrentActivity;
            consentInformation = UserMessagingPlatform.GetConsentInformation(ma);

            consentInformation.RequestConsentInfoUpdate(
                ma,
                requestParameters,
                new GoogleUMPConsentUpdateSuccessListener(
                    () =>
                    {
                        // The consent information state was updated.
                        // You are now ready to check if a form is available.
                        if (consentInformation.IsConsentFormAvailable)
                        {
                            Xamarin.Google.UserMesssagingPlatform.UserMessagingPlatform.LoadConsentForm(
                                MainActivity.ActivityCurrent,
                                new GoogleUMPFormLoadSuccessListener((Xamarin.Google.UserMesssagingPlatform.IConsentForm f) =>
                                {
                                    googleUMPConsentForm = f;
                                    googleUMPConsentInformation = consentInformation;
                                    Console.WriteLine("DEBUG: MainActivity.OnCreate: Consent management flow: LoadConsentForm has loaded a form, which will be shown if necessary, once the ViewModel is ready.");
                                    DisplayAdvertisingConsentFormIfNecessary();
                                }),
                                new GoogleUMPFormLoadFailureListener((Xamarin.Google.UserMesssagingPlatform.FormError e) =>
                                {
                                    // Handle the error.
                                    Console.WriteLine("ERROR: MainActivity.OnCreate: Consent management flow: failed in LoadConsentForm with error " + e.Message);
                                }));
                        }
                        else
                        {
                            Console.WriteLine("DEBUG: MainActivity.OnCreate: Consent management flow: RequestConsentInfoUpdate succeeded but no consent form was available.");
                        }
                    }),
                new GoogleUMPConsentUpdateFailureListener(
                    (Xamarin.Google.UserMesssagingPlatform.FormError e) =>
                    {
                        // Handle the error.
                        Console.WriteLine("ERROR: MainActivity.OnCreate: Consent management flow: failed in RequestConsentInfoUpdate with error " + e.Message);
                    })
                );
        }
        catch (System.Exception ex)
        {
            Console.WriteLine("ERROR: MainActivity.OnCreate: Exception thrown during consent management flow: ", ex);
        }
    }

    public interface IAdmobInterstitial
    {
        void Show(string adUnit);

        void Give();
    }

    public class InterstitialAdListener : AdListener
    {
        readonly InterstitialAd _ad;

        public InterstitialAdListener(InterstitialAd ad)
        {
            _ad = ad;
        }

        public override void OnAdLoaded()
        {
            base.OnAdLoaded();

            //if (_ad.IsLoaded)
            //    _ad.Show();
        }
    }

    public void DisplayAdvertisingConsentFormIfNecessary()
    {
        try
        {
            if (googleUMPConsentForm != null && googleUMPConsentInformation != null)
            {
                /* ConsentStatus:
                    Unknown = 0,
                    NotRequired = 1,
                    Required = 2,
                    Obtained = 3
                */
                if (googleUMPConsentInformation.ConsentStatus == 2)
                {
                    Console.WriteLine("DEBUG: MainActivity.DisplayAdvertisingConsentFormIfNecessary: Consent form is being displayed.");
                    DisplayAdvertisingConsentForm();
                }
                else
                {
                    Console.WriteLine("DEBUG: MainActivity.DisplayAdvertisingConsentFormIfNecessary: Consent form is not being displayed because consent status is " + googleUMPConsentInformation.ConsentStatus.ToString());
                }
            }
            else
            {
                Console.WriteLine("ERROR: MainActivity.DisplayAdvertisingConsentFormIfNecessary: consent form or consent information missing.");
            }
        }
        catch (System.Exception ex)
        {
            Console.WriteLine("ERROR: MainActivity.DisplayAdvertisingConsentFormIfNecessary: Exception thrown: ", ex);
        }
    }

    public void DisplayAdvertisingConsentForm()
    {
        try
        {
            if (googleUMPConsentForm != null && googleUMPConsentInformation != null)
            {
                Log.Debug(Tag, "displaying consent form");

                googleUMPConsentForm.Show(MainActivity.ActivityCurrent, new GoogleUMPConsentFormDismissedListener(
                        (Xamarin.Google.UserMesssagingPlatform.FormError f) =>
                        {
                            if (googleUMPConsentInformation.ConsentStatus == 2) // required
                            {
                                Console.WriteLine("ERROR: MainActivity.DisplayAdvertisingConsentForm: Consent was dismissed; showing it again because consent is still required.");
                                DisplayAdvertisingConsentForm();
                            }
                        }));
            }
            else
            {
                Log.Error(Tag, "Consent form or consent information are missing!");
            }
        }
        catch (System.Exception ex)
        {
            Log.Error(Tag, "consent request failed!\ncaught exception: " + ex);
        }
    }

    public class GoogleUMPConsentFormDismissedListener : Java.Lang.Object, Xamarin.Google.UserMesssagingPlatform.IConsentFormOnConsentFormDismissedListener
    {
        public GoogleUMPConsentFormDismissedListener(Action<Xamarin.Google.UserMesssagingPlatform.FormError> failureAction)
        {
            a = failureAction;
        }
        public void OnConsentFormDismissed(Xamarin.Google.UserMesssagingPlatform.FormError f)
        {
            a(f);
        }

        private Action<Xamarin.Google.UserMesssagingPlatform.FormError> a = null;
    }

    public class GoogleUMPConsentUpdateFailureListener : Java.Lang.Object, Xamarin.Google.UserMesssagingPlatform.IConsentInformationOnConsentInfoUpdateFailureListener
    {
        public GoogleUMPConsentUpdateFailureListener(Action<Xamarin.Google.UserMesssagingPlatform.FormError> failureAction)
        {
            a = failureAction;
        }
        public void OnConsentInfoUpdateFailure(Xamarin.Google.UserMesssagingPlatform.FormError f)
        {
            a(f);
        }

        private Action<Xamarin.Google.UserMesssagingPlatform.FormError> a = null;
    }

    public class GoogleUMPConsentUpdateSuccessListener : Java.Lang.Object, Xamarin.Google.UserMesssagingPlatform.IConsentInformationOnConsentInfoUpdateSuccessListener
    {
        public GoogleUMPConsentUpdateSuccessListener(Action successAction)
        {
            a = successAction;
        }

        public void OnConsentInfoUpdateSuccess()
        {
            a();
        }

        private Action a = null;
    }

    public class GoogleUMPFormLoadFailureListener : Java.Lang.Object, Xamarin.Google.UserMesssagingPlatform.UserMessagingPlatform.IOnConsentFormLoadFailureListener
    {
        public GoogleUMPFormLoadFailureListener(Action<Xamarin.Google.UserMesssagingPlatform.FormError> failureAction)
        {
            a = failureAction;
        }
        public void OnConsentFormLoadFailure(Xamarin.Google.UserMesssagingPlatform.FormError e)
        {
            a(e);
        }

        private Action<Xamarin.Google.UserMesssagingPlatform.FormError> a = null;
    }

    public class GoogleUMPFormLoadSuccessListener : Java.Lang.Object, Xamarin.Google.UserMesssagingPlatform.UserMessagingPlatform.IOnConsentFormLoadSuccessListener
    {
        public GoogleUMPFormLoadSuccessListener(Action<Xamarin.Google.UserMesssagingPlatform.IConsentForm> successAction)
        {
            a = successAction;
        }
        public void OnConsentFormLoadSuccess(Xamarin.Google.UserMesssagingPlatform.IConsentForm f)
        {
            a(f);
        }

        private Action<Xamarin.Google.UserMesssagingPlatform.IConsentForm> a = null;
    }

    // BILLING
    private void LoadBillingClient()
    {
        Console.WriteLine($"{Tag}: InitBillingClient");

        // create billing client
        MBillingClient = BillingClient.NewBuilder(MainActivity.ActivityCurrent)
                .EnablePendingPurchases()
                .SetListener(this)
                // Configure other settings.
                .Build();

        // establish connection w/ google play billing
        EstablishBillingConnection();
    }

    private class BillingClientStateListener : Java.Lang.Object, IBillingClientStateListener
    {

        public void OnBillingSetupFinished(BillingResult billingResult)
        {
            Log.Info(Tag, "OnBillingSetupFinished");
            if (billingResult.ResponseCode == BillingResponseCode.Ok)
            {
                UpdatePurchasesAndProducts();
            }

        }
        public void OnBillingServiceDisconnected()
        {
            Log.Info(Tag, "OnBillingServiceDisconnected");

            MainActivity.ActivityCurrent.EstablishBillingConnection();
        }

        public async Task UpdatePurchasesAndProducts()
        {
            Log.Info(Tag, "UpdatePurchasesAndProducts");

            // query available product details
            var details = await QueryProductDetailsAsync();
            if (details != null)
            {
                Log.Info(Tag, "product details received!");
            }
            else
            {
                Log.Error(Tag, "product details not received!");
            }

            // check if gold is purchased
            var IsGold = await CheckSubscriptionStatusAsync();

            // set IS_GOLD preference
            Preferences.Default.Set("IS_GOLD", IsGold);
        }

        public async Task<bool> CheckSubscriptionStatusAsync()
        {
            Console.WriteLine($"{Tag}: CheckSubscriptionStatusAsync");

            var queryPurchasesParams = QueryPurchasesParams.NewBuilder()
                .SetProductType(BillingClient.SkuType.Subs)
                .Build();

            var purchasesResult = await MainActivity.ActivityCurrent.MBillingClient.QueryPurchasesAsync(queryPurchasesParams);

            if (purchasesResult.Result.ResponseCode == BillingResponseCode.Ok)
            {
                var purchases = purchasesResult.Purchases;

                if (purchases.Count == 0)
                {
                    Log.Info(Tag, "no purchases found");
                    return false;
                }

                foreach (var purchase in purchases)
                {
                    string purchaseProductId = purchase.Products[0];
                    Log.Info(Tag, "found purchase product id: " + purchaseProductId);

                    if (purchaseProductId == "instaloader_gold")
                    {
                        if (purchase.PurchaseState != PurchaseState.Purchased)
                        {
                            Log.Warn(Tag, "Purchase state != purchased");
                        }
                        else if (!purchase.IsAcknowledged)
                        {
                            MainActivity.ActivityCurrent.HandlePurchase(purchase);
                        }
                        return true;
                    }

                    return false;
                }
            }

            Log.Error(Tag, "ResponseCode != Ok");
            return false;
        }

        public async Task<ProductDetails> QueryProductDetailsAsync()
        {
            // query available product details
            QueryProductDetailsParams queryProductDetailsParams =
                    QueryProductDetailsParams.NewBuilder()
                            .SetProductList(
                                    ImmutableList.Create(
                                            QueryProductDetailsParams.Product.NewBuilder()
                                                    .SetProductId("instaloader_gold")
                                                    .SetProductType(BillingClient.ProductType.Subs)
                                                    .Build()))
                            .Build();

            var result = await MainActivity.ActivityCurrent.MBillingClient.QueryProductDetailsAsync(
                    queryProductDetailsParams);

            if (result != null)
            {
                ImmutableList<BillingFlowParams.ProductDetailsParams> productDetailsParamsList =
                        ImmutableList.Create(
                                BillingFlowParams.ProductDetailsParams.NewBuilder()
                                        // retrieve a value for "productDetails" by calling queryProductDetailsAsync()
                                        .SetProductDetails(result.ProductDetails[0])
                                        // For one-time products, "setOfferToken" method shouldn't be called.
                                        // For subscriptions, to get an offer token, call
                                        // ProductDetails.subscriptionOfferDetails() for a list of offers
                                        // that are available to the user.
                                        .SetOfferToken(result.ProductDetails[0]
                                        .GetSubscriptionOfferDetails()[0]
                                        .OfferToken)
                                        .Build()
                        );

                Log.Info(Tag, "size of productDetailsParamsList: " + productDetailsParamsList.Count);

                MainActivity.ActivityCurrent.MBillingFlowParams = BillingFlowParams.NewBuilder()
                        .SetProductDetailsParamsList(productDetailsParamsList)
                        .Build();

                return result.ProductDetails[0];
            }
            else
            {
                Log.Error(Tag, "QueryProductDetailsAsync returned null");
            }
            return null;
        }
    }

    async Task HandlePendingTransactions()
    {
        Log.Info(Tag, "HandlePendingTransactions");

        // handle pending transactions
        QueryPurchasesResult result = await MBillingClient.QueryPurchasesAsync(
                QueryPurchasesParams.NewBuilder()
                .SetProductType(BillingClient.ProductType.Subs)
                .Build()
        );

        if (result.Purchases.Count > 0)
        {
            foreach (Purchase purchase in result.Purchases)
            {
                if (purchase.PurchaseState == PurchaseState.Purchased && !purchase.IsAcknowledged)
                {
                    HandlePurchase(purchase);
                }
            }
        }
    }

    public class AcknowledgePurchaseResponseListener : Java.Lang.Object, IAcknowledgePurchaseResponseListener
    {
        public void OnAcknowledgePurchaseResponse(BillingResult billingResult)
        {
            Log.Info(Tag, "OnAcknowledgePurchaseResponse");
            if (billingResult.ResponseCode == BillingResponseCode.Ok)
            {
                Log.Info(Tag, "purchase acknowledged!");

                // run on ui thread
                MainThread.BeginInvokeOnMainThread(() => {
                    // show purchased toast
                    AndHUD.Shared.ShowToast(MainActivity.ActivityCurrent, "Thank you for your support <3", MaskType.None, TimeSpan.FromMilliseconds(12000), false);
                    MainPage mp = ((MainPage)Shell.Current.CurrentPage);
                    mp.MIsNotGold = false;
                    mp.CloseFragment();
                    mp.ClearTextfield();
                    mp.ShowEmptyUI();
                });
            }
        }
    }

    public void LaunchBillingFlow()
    {
        Console.WriteLine($"{Tag}: LaunchBillingFlow");

        if (MBillingFlowParams != null)
        {
            BillingResult BillingResult = MBillingClient.LaunchBillingFlow(MainActivity.ActivityCurrent, MBillingFlowParams);
            Console.WriteLine($"{Tag}: BillingResult.ResponseCode=={BillingResult.ResponseCode}");
            Console.WriteLine($"{Tag}: {BillingResult.DebugMessage}");
        }
        else
        {
            Log.Error(Tag, "MBillingFlowParams == null");
        }
    }

    public void EstablishBillingConnection()
    {
        Log.Info(Tag, "EstablishBillingConnection");
        MBillingClient.StartConnection(new BillingClientStateListener());
    }

    public void OnPurchasesUpdated(BillingResult billingResult, IList<Purchase>? purchases)
    {
        var billingResponseCode = billingResult.ResponseCode;
        Console.WriteLine($"{Tag}: OnPurchasesUpdated  billingResponseCode={billingResponseCode}");

        if (billingResult.ResponseCode == BillingResponseCode.Ok && purchases != null)
        {
            foreach (Purchase purchase in purchases)
            {
                string purchaseProductId = purchase.Products[0];
                Console.WriteLine($"{Tag} purchase found!  purchaseProductId={purchaseProductId} purchase.PurchaseState={purchase.PurchaseState}");

                if (purchaseProductId == "instaloader_gold")
                {
                    if (purchase.PurchaseState != PurchaseState.Purchased)
                    {
                        Console.WriteLine($"{Tag} ");
                    }
                    else if (!purchase.IsAcknowledged)
                    {
                        Console.WriteLine($"{Tag} purchase.IsAcknowledged={purchase.IsAcknowledged}");

                        HandlePurchase(purchase);
                    }
                }
            }
        }
        else
        {
            Console.WriteLine($"{Tag} no purchases found");
        }
    }

    public void HandlePurchase(Purchase purchase)
    {
        Console.WriteLine($"{Tag} HandlePurchase");

        AcknowledgePurchaseParams acknowledgePurchaseParams = AcknowledgePurchaseParams
                .NewBuilder()
                .SetPurchaseToken(purchase.PurchaseToken)
                .Build();

        MBillingClient.AcknowledgePurchase(acknowledgePurchaseParams, new AcknowledgePurchaseResponseListener());
    }

    // DOWNLOAD RECEIVER
    [BroadcastReceiver(Enabled = true, Exported = false)]
    public class DownloadReceiver : BroadcastReceiver
    {
        private static readonly string Tag = nameof(DownloadReceiver);
        public static int MCount = 0;

        public override void OnReceive(Context context, Intent intent)
        {
            Console.WriteLine($"{Tag} OnReceive MCount={++MCount} MainPage.MDownloadUrls.Count={MainPage.MDownloadUrls.Count}");
            MainPage mp = ((MainPage)Shell.Current.CurrentPage);
            string action = intent.Action;
            if (DownloadManager.ActionDownloadComplete.Equals(action))
            {
                Console.WriteLine($"{Tag} downloaded file");
                if (MCount >= MainPage.MDownloadUrls.Count)
                {
                    Console.WriteLine($"{Tag} last file downloaded!");
                    // update progress
                    ProgressRing pr = ((ProgressRing)mp.FindByName("progress_ring"));
                    double progress = MCount / (double)MainPage.MDownloadUrls.Count;
                    int percent = (int)(progress * 100.0);
                    pr.Progress = progress;
                    pr.IsIndeterminate = false;
                    mp.MMessageProgress = $"Finishing…";

                    // send finish broadcast
                    MainActivity.ActivityCurrent.SendBroadcast(new Intent("69"));

                    // unregister self
                    Console.WriteLine($"{Tag} unregistering self");
                    try
                    {
                        context.UnregisterReceiver(this);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{Tag} already unregistered");
                    }
                }
                else
                {
                    Console.WriteLine($"{Tag} media downloaded");

                    // update progress
                    ProgressRing pr = ((ProgressRing)mp.FindByName("progress_ring"));
                    double progress = MCount / (double)MainPage.MDownloadUrls.Count;
                    int percent = (int)(progress * 100.0);
                    pr.Progress = progress;
                    pr.IsIndeterminate = false;
                    mp.MMessageProgress = $"Downloading…\n{percent}%";
                }
            }
        }
    }

    // FINISH RECEIVER
    [BroadcastReceiver(Enabled = true, Exported = false)]
    public class FinishReceiver : BroadcastReceiver
    {
        string Tag = nameof(FinishReceiver);
        public override void OnReceive(Context context, Intent intent)
        {
            // log event
            try
            {
                Bundle bundle = new Bundle();
                bundle.PutString("input", "finish");
                bundle.PutString("app_name", "instaloader");
                FirebaseAnalytics.GetInstance((MainActivity)Platform.CurrentActivity).LogEvent("input_finish", bundle);
            }
            catch (Exception)
            {
                Console.WriteLine($"{Tag} failed to log event");
            }

            // cleanup files
            string filepath = MainPage.AbsPathDocs + MainPage.IgId;
            Java.IO.File docs = new Java.IO.File(MainPage.AbsPathDocs);
            if (docs.IsDirectory)
            {
                Java.IO.File[] allContents = docs.ListFiles();
                foreach (Java.IO.File file in allContents)
                {
                    if (file.Name.StartsWith(IgId))
                    {
                        Console.WriteLine($"{Tag} found instaloader file: file.Name={file.Name}");

                        Console.WriteLine($"{Tag} scanning file at: file.AbsolutePath={file.AbsolutePath}");
                        ScanDownload(file.AbsolutePath);
                    }
                }
            }

            // close service and unregister receiver
            ((MainPage)Shell.Current.CurrentPage).Services.Stop();
            context.UnregisterReceiver(this);

            // update ui
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await ((MainPage)Shell.Current.CurrentPage).ShowFinishUI();
                
                // finish activity if shared
                if (MIsShared)
                {
                    Console.WriteLine($"{Tag} calling FinishAfterTransition()");
                    Platform.CurrentActivity.FinishAfterTransition();
                }
            });

            
        }
    }

    private static async Task ScanDownload(string filepath)
    {
        // scan media file
        Console.WriteLine($"{Tag} scanning new media file at MFilePath={filepath}");
        Android.Net.Uri uri = Android.Net.Uri.Parse("file://" + filepath);
        Intent scanFileIntent = new Intent(Intent.ActionMediaScannerScanFile, uri);
        MainActivity.ActivityCurrent.SendBroadcast(scanFileIntent);
    }
}
