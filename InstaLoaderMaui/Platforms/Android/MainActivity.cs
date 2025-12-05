using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Xamarin.Google.UserMesssagingPlatform;
using UraniumUI.Material.Controls;
using Android.BillingClient.Api;
using System.Collections.Immutable;
using AndroidHUD;
using static InstaLoaderMaui.MainPage;
using Firebase.Analytics;
using MPowerKit.ProgressRing;
using AndroidX.Activity;

namespace InstaLoaderMaui;

[Activity(Theme = "@style/MainTheme.NoActionBar", MainLauncher = true, Exported = true, LaunchMode = LaunchMode.SingleInstance, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter(new[] { Intent.ActionSend },
          Categories = new[] {
              Intent.CategoryDefault
          },
          DataMimeType = "*/*")]
[MetaData(name: "com.google.android.play.billingclient.version", Value = "7.1.1")]
public class MainActivity : MauiAppCompatActivity
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

        EdgeToEdge.Enable(this);
        base.OnCreate(savedInstanceState);
        Platform.Init(this, savedInstanceState);

        // Fixes "strict-mode" error when fetching webpage... idek..
        StrictMode.ThreadPolicy policy = new StrictMode.ThreadPolicy.Builder().PermitAll().Build();
        StrictMode.SetThreadPolicy(policy);

        AskPermissions();
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
                        Console.WriteLine($"{Tag} found instaloader file: file.Name={file.Name}");

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
                    await Task.Delay(400);
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
}
