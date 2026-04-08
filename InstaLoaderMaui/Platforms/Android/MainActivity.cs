using Android.App;
using Android.BillingClient.Api;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Ads;
using Android.Gms.Ads.Interstitial;
using Android.OS;
using Android.Util;
using AndroidHUD;
using AndroidX.Activity;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Firebase.Analytics;
using MPowerKit.ProgressRing;
using Plugin.MauiMTAdmob;
using Plugin.MauiMTAdmob.Controls;
using System.Collections.Immutable;
using UraniumUI.Material.Controls;
using Xamarin.Google.UserMesssagingPlatform;
using static InstaLoaderMaui.MainPage;

namespace InstaLoaderMaui;

[Activity(Theme = "@style/MainTheme.NoActionBar", MainLauncher = true, Exported = true, LaunchMode = LaunchMode.SingleInstance, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
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

    IConsentInformation MConsentInformation;
    public BillingClient MBillingClient;
    public BillingFlowParams MBillingFlowParams, YBillingFlowParams;
    //private IConsentForm googleUMPConsentForm = null;
    //private IConsentInformation googleUMPConsentInformation = null;

    public static MainActivity ActivityCurrent { get; set; }
    public MainActivity()
    {
        ActivityCurrent = this;
    }

    protected override async void OnCreate(Bundle? savedInstanceState)
    {
        Console.WriteLine($"{Tag}: OnCreate");

        EdgeToEdge.Enable(this);
        base.OnCreate(savedInstanceState);
        Platform.Init(this, savedInstanceState);

        // Fixes "strict-mode" error when fetching webpage... idek..
        StrictMode.ThreadPolicy policy = new StrictMode.ThreadPolicy.Builder().PermitAll().Build();
        StrictMode.SetThreadPolicy(policy);

        AskPermissions();

        LoadBillingClient();
    }

    protected override void OnResume()
    {
        base.OnResume();
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);

        Console.WriteLine($"{Tag}: OnNewIntent");

        CheckForIntent(intent);

    }

    public async Task CheckForIntent()
    {
        CheckForIntent(this.Intent);
    }

    public async Task CheckForIntent(Intent intent)
    {
        MainPage mp = (MainPage)Shell.Current.CurrentPage;
        await mp.ClearTextfield();
        await mp.ShowEmptyUI();

        if (intent != null)
        {

            var data = intent.GetStringExtra(Intent.ExtraText);
            if (data != null)
            {
                Console.WriteLine($"{Tag}: received data from intent: {data}");

                Instaloader.MIsShared = true;

                string SharedText = data.ToString();
                TextField mTextField = (TextField)mp.FindByName("main_textfield");
                if (mTextField != null)
                {
                    mTextField.Text = SharedText;
                    mp.HandleInput(SharedText);
                }
                else
                {
                    Console.WriteLine($"{Tag} null textfield!");
                }
            }
        }
    }

    private void AskPermissions()
    {
        if ((int)Build.VERSION.SdkInt < 33
            && ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.WriteExternalStorage) != Permission.Granted)
        {
            ActivityCompat.RequestPermissions(
            MainActivity.ActivityCurrent, new string[] { Android.Manifest.Permission.ReadExternalStorage, Android.Manifest.Permission.WriteExternalStorage }, 101);
        }
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
                        Console.WriteLine($"{Tag} found insave file: file.Name={file.Name}");

                        Console.WriteLine($"{Tag} scanning file at: file.AbsolutePath={file.AbsolutePath}");
                        ScanDownload(file.AbsolutePath);
                    }
                }
            }

            // close service and unregister receiver
            MainPage mp = ((MainPage)Shell.Current.CurrentPage);
            mp.Services.Stop();
            context.UnregisterReceiver(this);

            // finish activity if shared
            if (Instaloader.MIsShared)
            {
                Console.WriteLine($"{Tag} finishing activity...");

                ResetVars();
                Instaloader.MIsShared = false;
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    // increment successful runs
                    int runs = 1;
                    if (Preferences.Default.ContainsKey("SUCCESSFUL_RUNS"))
                    {
                        runs += Preferences.Default.Get("SUCCESSFUL_RUNS", 0);
                    }
                    successfulRuns = runs;

                    // set in prefs
                    Preferences.Default.Set("SUCCESSFUL_RUNS", runs);
                    Console.WriteLine($"{Tag} SUCCESSFUL_RUNS={runs}");

                    // show success message
                    mp.MMessageToast = $"Saved! In {AbsPathDocs}";
                    AndHUD.Shared.ShowSuccess(MainActivity.ActivityCurrent, mp.MMessageToast, MaskType.Black, TimeSpan.FromMilliseconds(1600));

                    // clear views
                    await ((MainPage)Shell.Current.CurrentPage).ClearTextfield();
                    await ((MainPage)Shell.Current.CurrentPage).ShowEmptyUI();
                    await Task.Delay(333);

                    // finish activity
                    Platform.CurrentActivity.FinishAfterTransition();
                });
            }
            else
            {
                ((MainPage)Shell.Current.CurrentPage).ShowFinishUI();
            }


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

    // ADMOB
    public void LoadAdmob()
    {
        // log event 
        try
        {
            Bundle bundle = new Bundle();
            bundle.PutString("admob", "load");
            bundle.PutString("app_name", "apotiflyer");
            FirebaseAnalytics.GetInstance(MainActivity.ActivityCurrent).LogEvent("admob_load", bundle);
        }
        catch (Exception e)
        {
            Console.WriteLine($"{Tag} failed to log event: {e.Message}");
        }

        // check if gold
        if (!((MainPage)Shell.Current.CurrentPage).MIsNotGold)
        {
            Console.WriteLine($"{Tag} Skipping LoadAdmob");
            return;
        }
        Console.WriteLine($"{Tag} LoadAdmob");

        CrossMauiMTAdmob.Current.Init(MainActivity.ActivityCurrent, MainPage.AdmobIdApp);
        CrossMauiMTAdmob.Current.UserPersonalizedAds = true;
        SetGDPR();
        LoadBannerAd();
        if (!CrossMauiMTAdmob.Current.IsInterstitialLoaded())
        {
            CrossMauiMTAdmob.Current.LoadInterstitial(MainPage.admobIdInter);
        }
    }

    private void SetGDPR()
    {
        Console.WriteLine("SetGDPR");
        try
        {
#if DEBUG
            Log.Info(Tag, "running DEBUG branch");
            var debugSettings = new ConsentDebugSettings.Builder(MainActivity.ActivityCurrent)
            .SetDebugGeography(ConsentDebugSettings
                    .DebugGeography
                    .DebugGeographyEea)
            //.AddTestDeviceHashedId("see logcat...")
            .AddTestDeviceHashedId(Android.Provider.Settings.Secure.GetString(Platform.CurrentActivity.ContentResolver,
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

            MConsentInformation = UserMessagingPlatform.GetConsentInformation(Platform.CurrentActivity);

            MConsentInformation.RequestConsentInfoUpdate(
                Platform.CurrentActivity,
                requestParameters,
                new GoogleUMPConsentUpdateSuccessListener(
                    () =>
                    {
                        // The consent information state was updated.
                        // You are now ready to check if a form is available.
                        if (MConsentInformation.IsConsentFormAvailable)
                        {
                            UserMessagingPlatform.LoadConsentForm(
                                Platform.CurrentActivity,
                                new GoogleUMPFormLoadSuccessListener((IConsentForm f) => {
                                    googleUMPConsentForm = f;
                                    googleUMPConsentInformation = MConsentInformation;
                                    Console.WriteLine("Consent management flow: LoadConsentForm has loaded a form, which will be shown if necessary, once the ViewModel is ready.");
                                    DisplayAdvertisingConsentFormIfNecessary();
                                }),
                                new GoogleUMPFormLoadFailureListener((FormError e) => {
                                    // Handle the error.
                                    Console.WriteLine("failed in LoadConsentForm with error " + e.Message);
                                }));
                        }
                        else
                        {
                            Console.WriteLine("Consent management flow: RequestConsentInfoUpdate succeeded but no consent form was available.");
                        }
                    }),
                new GoogleUMPConsentUpdateFailureListener(
                    (FormError e) =>
                    {
                        // Handle the error.
                        Console.WriteLine("ERROR: Consent management flow: failed in RequestConsentInfoUpdate with error " + e.Message);
                    })
                );
        }
        catch (System.Exception ex)
        {
            Console.WriteLine("ERROR: Exception thrown during consent management flow: ", ex);
        }
    }

    public void LoadBannerAd()
    {
        Console.WriteLine($"{Tag} LoadBannerAd");
        ((MTAdView)((MainPage)Shell.Current.CurrentPage).FindByName("banner_ad")).LoadAd();
    }

    public void LoadInterstitalAd()
    {
        Console.WriteLine($"{Tag} LoadInterstitialAd");
        CrossMauiMTAdmob.Current.LoadInterstitial(admobIdInter);
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

    private IConsentForm googleUMPConsentForm = null;
    private IConsentInformation googleUMPConsentInformation = null;
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

                googleUMPConsentForm.Show(Platform.CurrentActivity, new GoogleUMPConsentFormDismissedListener(
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

    public class GoogleUMPConsentFormDismissedListener : Java.Lang.Object, IConsentFormOnConsentFormDismissedListener
    {
        public GoogleUMPConsentFormDismissedListener(Action<FormError> failureAction)
        {
            a = failureAction;
        }
        public void OnConsentFormDismissed(FormError f)
        {
            a(f);
        }

        private Action<FormError> a = null;
    }

    public class GoogleUMPConsentUpdateFailureListener : Java.Lang.Object, IConsentInformationOnConsentInfoUpdateFailureListener
    {
        public GoogleUMPConsentUpdateFailureListener(Action<FormError> failureAction)
        {
            a = failureAction;
        }
        public void OnConsentInfoUpdateFailure(FormError f)
        {
            a(f);
        }

        private Action<FormError> a = null;
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

    public class GoogleUMPFormLoadFailureListener : Java.Lang.Object, UserMessagingPlatform.IOnConsentFormLoadFailureListener
    {
        public GoogleUMPFormLoadFailureListener(Action<FormError> failureAction)
        {
            a = failureAction;
        }
        public void OnConsentFormLoadFailure(FormError e)
        {
            a(e);
        }

        private Action<FormError> a = null;
    }

    public class GoogleUMPFormLoadSuccessListener : Java.Lang.Object, UserMessagingPlatform.IOnConsentFormLoadSuccessListener
    {
        public GoogleUMPFormLoadSuccessListener(Action<IConsentForm> successAction)
        {
            a = successAction;
        }
        public void OnConsentFormLoadSuccess(IConsentForm f)
        {
            a(f);
        }

        private Action<IConsentForm> a = null;
    }

    // BILLING
    private void LoadBillingClient()
    {
        Console.WriteLine($"{Tag}: LoadBillingClient");

        // create billing client
        MBillingClient = BillingClient.NewBuilder(Platform.CurrentActivity)
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
            MainPage mp = (MainPage)Shell.Current.CurrentPage;
            mp.MIsNotGold = !await CheckSubscriptionStatusAsync();

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
                MainPage mp = (MainPage)Shell.Current.CurrentPage;
                if (purchases.Count == 0)
                {
                    mp.MIsNotGold = true;
                    return false;
                }
                else
                {
                    mp.MIsNotGold = false;
                }

                foreach (var purchase in purchases)
                {
                    string purchaseProductId = purchase.Products[0];
                    Log.Info(Tag, "found purchase product id: " + purchaseProductId);

                    if (purchaseProductId == "insave_gold")
                    {
                        if (!purchase.IsAcknowledged)
                        {
                            ((MainActivity)Platform.CurrentActivity).HandlePurchase(purchase);
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
            Console.WriteLine("QueryProductDetailsAsync");
            // query available product details
            QueryProductDetailsParams queryProductDetailsParams =
                    QueryProductDetailsParams.NewBuilder()
                            .SetProductList(
                                    ImmutableList.Create(
                                            QueryProductDetailsParams.Product.NewBuilder()
                                                    .SetProductId("insave_gold")
                                                    .SetProductType(BillingClient.ProductType.Subs)
                                                    .Build()))
                            .Build();


            var result = await MainActivity.ActivityCurrent.MBillingClient.QueryProductDetailsAsync(
                    queryProductDetailsParams);

            if (result != null  && result.ProductDetails.Count > 0)
            {
                Console.WriteLine("query products result is not null");

                /*
                 * ImmutableList<BillingFlowParams.ProductDetailsParams> yProductDetailsParamsList =
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
                Console.WriteLine($"size of yProductDetailsParamsList: {yProductDetailsParamsList.Count}");
                 */

                ImmutableList<BillingFlowParams.ProductDetailsParams> mProductDetailsParamsList =
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

                Console.WriteLine($"size of mProductDetailsParamsList: {mProductDetailsParamsList.Count}");

                MainActivity.ActivityCurrent.MBillingFlowParams = BillingFlowParams.NewBuilder()
                        .SetProductDetailsParamsList(mProductDetailsParamsList)
                        .Build();
                //MainActivity.ActivityCurrent.YBillingFlowParams = BillingFlowParams.NewBuilder()
                //        .SetProductDetailsParamsList(yProductDetailsParamsList)
                //        .Build();

                return result.ProductDetails[0];
            }
            else
            {
                Log.Error(Tag, "QueryProductDetailsAsync returned null or empty list of products");
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
                MainThread.InvokeOnMainThreadAsync(() => {
                    // show purchased toast
                    //AndHUD.Shared.ShowToast(Platform.CurrentActivity, "Thank you for your support <3", MaskType.None, TimeSpan.FromMilliseconds(12000), false);

                    MainPage mp = ((MainPage)Shell.Current.CurrentPage);
                    mp.MIsNotGold = false;
                    mp.HidePopup();
                    mp.ClearTextfield();
                    mp.ShowEmptyUI();
                });
            }
        }
    }

    public void LaunchBillingFlow(String plan_type)
    {
        if (plan_type.Equals("yearly") && YBillingFlowParams != null)
        {
            Console.WriteLine($"{Tag}: LaunchBillingFlow yearly");
            // launch billing with yearly params
            BillingResult BillingResult = MBillingClient.LaunchBillingFlow(MainActivity.ActivityCurrent, YBillingFlowParams);
            Console.WriteLine($"{Tag}: BillingResult.ResponseCode=={BillingResult.ResponseCode}");
            Console.WriteLine($"{Tag}: {BillingResult.DebugMessage}");
        }
        else if (MBillingFlowParams != null)
        {
            Console.WriteLine($"{Tag}: LaunchBillingFlow monthly");
            // launch billing with monthly params
            BillingResult BillingResult = MBillingClient.LaunchBillingFlow(MainActivity.ActivityCurrent, MBillingFlowParams);
            Console.WriteLine($"{Tag}: BillingResult.ResponseCode=={BillingResult.ResponseCode}");
            Console.WriteLine($"{Tag}: {BillingResult.DebugMessage}");
        }
        else
        {
            Console.WriteLine($"{Tag}: LaunchBillingFlow failed");
            Log.Error(Tag, "BillingFlowParams == null");
        }
    }

    public void EstablishBillingConnection()
    {
        Log.Info(Tag, "EstablishBillingConnection");
        MBillingClient.StartConnection(new BillingClientStateListener());
    }

    public void OnPurchasesUpdated(BillingResult billingResult, IList<Purchase>? purchases)
    {
        Console.WriteLine($"{Tag}: OnPurchasesUpdated");

        var billingResponseCode = billingResult.ResponseCode;
        Log.Info(Tag, "billing response code: " + billingResponseCode);

        if (billingResult.ResponseCode == BillingResponseCode.Ok && purchases != null)
        {
            foreach (Purchase purchase in purchases)
            {
                Log.Info(Tag, "purchase found!");

                string purchaseProductId = purchase.Products[0];
                Log.Info(Tag, "found purchase product id: " + purchaseProductId);

                if (purchaseProductId == "insave_gold")
                {
                    if (purchase.PurchaseState != PurchaseState.Purchased)
                    {
                        Log.Warn(Tag, "purchase != purchased");
                    }
                    else if (!purchase.IsAcknowledged)
                    {
                        Log.Info(Tag, "purchase == purchased; IsAcknowledged == false");

                        HandlePurchase(purchase);
                    }
                }
            }
        }
        else
        {
            Log.Info(Tag, "no purchases found");
        }
    }

    public void HandlePurchase(Purchase purchase)
    {
        Log.Info(Tag, "HandlePurchase");

        AcknowledgePurchaseParams acknowledgePurchaseParams = AcknowledgePurchaseParams
                .NewBuilder()
                .SetPurchaseToken(purchase.PurchaseToken)
                .Build();

        MBillingClient.AcknowledgePurchase(acknowledgePurchaseParams, new AcknowledgePurchaseResponseListener());
    }
}
