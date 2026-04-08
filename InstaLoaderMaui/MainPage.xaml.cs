using Android.Content;
using Android.OS;
using Android.Views.InputMethods;
using Android.Webkit;
using AndroidHUD;
using Firebase;
using Firebase.Analytics;
using Microsoft.Maui.Handlers;
using MPowerKit.ProgressRing;
using System.Text.RegularExpressions;
using UraniumUI.Material.Controls;
using static Android.Icu.Text.CaseMap;
using CookieManager = Android.Webkit.CookieManager;

namespace InstaLoaderMaui
{
    public partial class MainPage : ContentPage
    {
        private static readonly string Tag = nameof(MainPage);

        public Microsoft.Maui.Controls.WebView pwv;

        public static readonly string AdmobIdApp = "ca-app-pub-7417392682402637~6569990152";
        public static readonly string AdmobIdInterTest = "ca-app-pub-3940256099942544/1033173712";
        public static readonly string AdmobIdInterReal = "ca-app-pub-7417392682402637/9248124383";
        public static readonly string AdmobIdBannerTest = "ca-app-pub-3940256099942544/9214589741";
        public static readonly string AdmobIdBannerReal = "ca-app-pub-7417392682402637/5503771756";

        public static string admobIdInter = AdmobIdInterTest;
        public string mAdmobIdBanner = AdmobIdBannerTest;
        public string MAdmobIdBanner
        {
            get { return mAdmobIdBanner; }
            set
            {
                if (value == mAdmobIdBanner)
                {
                    return;
                }

                mAdmobIdBanner = value;
                OnPropertyChanged(nameof(MAdmobIdBanner));
            }
        }

        private uint ANIM_LENGTH = 333;
        private readonly string INPUT_REGEX = "^$|(?:instagram\\.com\\/)";
        public static readonly string UA_DESKTOP_CHROME = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36",
           UA_DESKTOP_OPERA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36 OPR/119.0.0.0",
            UA_MOBILE_CHROME = "Mozilla/5.0 (Linux; Android 10; K) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Mobile Safari/537.36";
        public static readonly string INSTALOADER_URL = "https://play.google.com/store/apps/details?id=green.mobileapps.instaloader";
        public static readonly string[] CharsToRemove = new string[] { "\"", "=", "\\", ":", "*", "?", "<", ">", "|", ".", "#" };

        public static string AbsPathDocs = "";
        public static string AbsPathDocsTemp = "";
        public static string MInput = "";
        public static bool MIsProfile = false;
        public static string IgId = "";
        public static string MCookies = Preferences.Default.Get("COOKIES", "");
        public static bool MIsAlreadyLoading = false;
        public static List<string> MDownloadUrls = new List<string>();

        public IServiceDownload Services;
        public static int successfulRuns = 0;
        string mTitle = "";
        public string MTitle
        {
            get { return mTitle; }
            set
            {
                if (value == mTitle)
                {
                    return;
                }

                mTitle = value;
                OnPropertyChanged("MTitle");
            }
        }

        string mArtist = "";
        public string MArtist
        {
            get { return mArtist; }
            set
            {
                if (value == mArtist)
                {
                    return;
                }

                mArtist = value;
                OnPropertyChanged("MArtist");
            }
        }
        string mThumbnailUrl = "";
        public string MThumbnailUrl
        {
            get { return mThumbnailUrl; }
            set
            {
                if (value == mThumbnailUrl)
                {
                    return;
                }

                mThumbnailUrl = value;
                OnPropertyChanged("MThumbnailUrl");
            }
        }

        string mMessageProgress = "";
        public string MMessageProgress
        {
            get { return mMessageProgress; }
            set
            {
                Console.WriteLine($"Setting mMessageProgress={value}");
                if (value == mMessageProgress)
                {
                    return;
                }

                mMessageProgress = value;
                OnPropertyChanged("MMessageProgress");
            }
        }

        string mMessageToast = "";
        public string MMessageToast
        {
            get { return mMessageToast; }
            set
            {
                Console.WriteLine($"Setting mMessageToast={value}");
                if (value == mMessageToast)
                {
                    return;
                }

                mMessageToast = value;
                OnPropertyChanged("MMessageToast");
            }
        }

        string mFragmentTitle = "";
        public string MFragmentTitle
        {
            get { return mFragmentTitle; }
            set
            {
                if (value == mFragmentTitle)
                {
                    return;
                }

                mFragmentTitle = value;
                OnPropertyChanged("MFragmentTitle");
            }
        }

        string mFragmentSubtitle = "";
        public string MFragmentSubtitle
        {
            get { return mFragmentSubtitle; }
            set
            {
                if (value == mFragmentSubtitle)
                {
                    return;
                }

                mFragmentSubtitle = value;
                OnPropertyChanged("MFragmentSubtitle");
            }
        }

        string mFragmentBody = "";
        public string MFragmentBody
        {
            get { return mFragmentBody; }
            set
            {
                if (value == mFragmentBody)
                {
                    return;
                }

                mFragmentBody = value;
                OnPropertyChanged("MFragmentBody");
            }
        }

        string mFragmentPositive = "";
        public string MFragmentPositive
        {
            get { return mFragmentPositive; }
            set
            {
                if (value == mFragmentPositive)
                {
                    return;
                }

                mFragmentPositive = value;
                OnPropertyChanged("MFragmentPositive");
            }
        }

        string mFragmentDismiss = "";
        public string MFragmentDismiss
        {
            get { return mFragmentDismiss; }
            set
            {
                if (value == mFragmentDismiss)
                {
                    return;
                }

                mFragmentDismiss = value;
                OnPropertyChanged("MFragmentDismiss");
            }
        }

        bool mIsNotGold = true;
        public bool MIsNotGold
        {
            get { return mIsNotGold; }
            set
            {
                if (value == mIsNotGold)
                {
                    return;
                }

                mIsNotGold = value;
                Preferences.Default.Set("IS_GOLD", !value);

                OnPropertyChanged("MIsNotGold");

                UpdateUpgradeItem();
            }
        }

        public MainPage(IServiceDownload s)
        {
            InitializeComponent();
            BindingContext = this;
            Services = s;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // prepare destination file dirs
            PrepareFileDirs();

            // check gold
            MIsNotGold = !Preferences.Default.Get("IS_GOLD", false);
            Console.WriteLine($"{Tag}, IS_GOLD={!MIsNotGold}");

            // init firebase
            FirebaseApp.InitializeApp(MainActivity.ActivityCurrent);

            MainActivity.ActivityCurrent.LoadAdmob();

            // check for shared intent
            MainActivity.ActivityCurrent.CheckForIntent();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }

        protected override bool OnBackButtonPressed()
        {
            // Use Dispatcher to run async code on the main thread for UI interactions
            Dispatcher.Dispatch(async () =>
            {
                HidePopup();
            });

            // Return true to prevent the default back button action immediately
            return true;
        }

        public static void PrepareFileDirs()
        {
            Console.WriteLine($"{Tag}: {nameof(PrepareFileDirs)}");
            AbsPathDocs = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, Android.OS.Environment.DirectoryDocuments);
            AbsPathDocsTemp = AbsPathDocs + "/temp";
            // docs directory
            Java.IO.File files = new Java.IO.File(AbsPathDocs);
            files.SetWritable(true);
            Directory.CreateDirectory(Path.GetDirectoryName(AbsPathDocs));
            // temp directory
            Java.IO.File temp_files = new Java.IO.File(AbsPathDocsTemp);
            temp_files.SetWritable(true);
            Directory.CreateDirectory(Path.GetDirectoryName(AbsPathDocsTemp));
        }

        public static void ResetVars()
        {
            Console.WriteLine($"{Tag} ResetVars");
            MainPage mp = (MainPage)Shell.Current.CurrentPage;
            MIsProfile = false;
            
            Instaloader.MIsShared = false;
            MIsAlreadyLoading = false;
            MDownloadUrls = new List<string>();
            MainActivity.DownloadReceiver.MCount = 0;
            mp.MThumbnailUrl = "";
            mp.MTitle = "";
            mp.MArtist = "";
            mp.MMessageProgress = "";
            mp.MMessageToast = "";
        }

        // BILLING 
        public void UpdateUpgradeItem()
        {
            Console.WriteLine($"{Tag} UpdateUpgradeItem() MIsNotGold={MIsNotGold}");

            // todo remove
            return;
            
            if (MIsNotGold)
            {
                // white toolbar item
                FontImageSource fis = (FontImageSource)FindByName("upgrade_fontimagesource");
                ResourceDictionary ColorResource = Microsoft.Maui.Controls.Application.Current.Resources.MergedDictionaries.FirstOrDefault() as ResourceDictionary;
                fis.Color = ColorResource["White"] as Color;
            }
            else
            {
                // gold toolbar item
                FontImageSource fis = (FontImageSource)FindByName("upgrade_fontimagesource");
                ResourceDictionary ColorResource = Microsoft.Maui.Controls.Application.Current.Resources.MergedDictionaries.FirstOrDefault() as ResourceDictionary;
                fis.Color = ColorResource["Gold"] as Color;
            }
        }

        // USER INTERFACE
        public async Task ShowEmptyUI()
        {
            Console.WriteLine($"{Tag}: ShowEmptyUI");

            ResetVars();
            UpdateUpgradeItem();
            HidePopup();

            // clear downloader views
            Console.WriteLine($"{Tag} clearing downloader views");

            ButtonView dlBtn = (ButtonView)FindByName("dl_btn");
            dlBtn.Opacity = 0.0;
            dlBtn.IsVisible = false;
            ButtonView finishBtn = (ButtonView)FindByName("finish_btn");
            finishBtn.Opacity = 0.0;
            finishBtn.IsVisible = false;
            ((Frame)FindByName("downloader_frame")).Opacity = 0.0;
            ((Image)FindByName("preview_img")).Opacity = 0.0;
            ((ButtonView)FindByName("dl_btn")).Opacity = 0.0;
            ((ProgressRing)FindByName("progress_ring")).Opacity = 0.0;
            ((Label)FindByName("progress_label")).Opacity = 0.0;

            // init webview
            var pmv = (Microsoft.Maui.Controls.WebView)FindByName("preview_webview");
            pmv.IsVisible = false;

            // set width cookie
            CookieManager.Instance.SetCookie("https://www.instagram.com/?hl=en", "wd=1680x881");
            CookieManager.Instance.SetCookie(MInput, "wd=1680x881");
        }

        public async Task ShowLoadingUI()
        {
            HideKeyboard();

            // change progress message
            MMessageProgress = "Loading…";

            // clear downloader views
            ((Frame)FindByName("downloader_frame")).Opacity = 0.0;
            ButtonView dlBtn = (ButtonView)FindByName("dl_btn");
            dlBtn.Opacity = 0.0;
            dlBtn.IsVisible = false;
            ButtonView finishBtn = (ButtonView)FindByName("finish_btn");
            finishBtn.Opacity = 0.0;
            finishBtn.IsVisible = false;

            // show indeterminate progress ring
            ProgressRing pr = (ProgressRing)FindByName("progress_ring");
            pr.IsIndeterminate = true;
            pr.FadeTo(1.0, ANIM_LENGTH);
            await ((Label)FindByName("progress_label")).FadeTo(1.0, ANIM_LENGTH);
        }

        public async Task ShowPreviewUI()
        {
            // hide finish button
            ButtonView finishBtn = (ButtonView)FindByName("finish_btn");
            finishBtn.Opacity = 0.0;
            finishBtn.IsVisible = false;

            // show downloader
            ((Frame)FindByName("downloader_frame")).Opacity = 1.0;
            ButtonView dlBtn = (ButtonView)FindByName("dl_btn");
            dlBtn.IsEnabled = true;
            dlBtn.Opacity = 1.0;
            dlBtn.IsVisible = true;

            // hide progress ring
            MMessageProgress = "";
            ProgressRing pr = (ProgressRing)FindByName("progress_ring");
            pr.FadeTo(0.0, 1000);
            await ((Label)FindByName("progress_label")).FadeTo(0.0, 1000);

            ((ProgressRing)FindByName("progress_ring")).Opacity = 0.0;
            ((Label)FindByName("progress_label")).Opacity = 0.0;

            // increase thumbnail opacity
            await ((Image)FindByName("preview_img")).FadeTo(1.0, ANIM_LENGTH);
            ((Image)FindByName("preview_img")).Opacity = 1.0;
        }

        public async Task ShowDownloadingUI()
        {
            // change progress message
            MMessageProgress = "Downloading…";

            ProgressRing pr = (ProgressRing)FindByName("progress_ring");
            ProgressRing prd = (ProgressRing)FindByName("progress_ring_dlr");
            ButtonView dlBtn = (ButtonView)FindByName("dl_btn");

            pr.IsIndeterminate = true;

            // hide preview UI
            pr.FadeTo(1.0, ANIM_LENGTH);
            pr.FadeTo(1.0, ANIM_LENGTH);
            ((Label)FindByName("progress_label")).FadeTo(1.0, ANIM_LENGTH);
            await dlBtn.FadeTo(0.0, ANIM_LENGTH);

            // show downloading UI
            ((Image)FindByName("preview_img")).FadeTo(0.45, ANIM_LENGTH);
            dlBtn.IsVisible = false;
            prd.IsVisible = true;
            prd.FadeTo(1.0, ANIM_LENGTH);
        }

        public async Task ShowFinishUI()
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

            // sometimes show popup
            int cycle = successfulRuns % 12;
            if (MIsNotGold && (cycle == 1 || cycle == 7))
            {
                ShowPopup("Rate");
            }
            else if (MIsNotGold && (cycle == 9 || cycle == 4))
            {
                ShowPopup("VscoLoader");
            }

            // show success message
            MMessageToast = $"Saved! In {AbsPathDocs}";
            AndHUD.Shared.ShowSuccess(MainActivity.ActivityCurrent, MMessageToast, MaskType.Black, TimeSpan.FromMilliseconds(2500));

            // hide progress
            ProgressRing prd = (ProgressRing)FindByName("progress_ring_dlr");
            prd.FadeTo(0.0, ANIM_LENGTH);
            ((ProgressRing)FindByName("progress_ring")).FadeTo(0.0, ANIM_LENGTH);
            await ((Label)FindByName("progress_label")).FadeTo(0.0, ANIM_LENGTH);
            prd.IsVisible = false;

            // show finish ui
            ((Image)FindByName("preview_img")).FadeTo(0.9, ANIM_LENGTH);
            ButtonView finishBtn = (ButtonView)FindByName("finish_btn");
            finishBtn.IsVisible = true;
            finishBtn.FadeTo(1.0, ANIM_LENGTH);
        }
        public void HideKeyboard()
        {
            var inputMethodManager = MainActivity.ActivityCurrent.GetSystemService(Context.InputMethodService) as InputMethodManager;
            if (inputMethodManager != null && MainActivity.ActivityCurrent is Android.App.Activity)
            {
                var activity = MainActivity.ActivityCurrent as Android.App.Activity;
                var token = activity.CurrentFocus?.WindowToken;
                inputMethodManager.HideSoftInputFromWindow(token, HideSoftInputFlags.None);
                activity.Window.DecorView.ClearFocus();
            }

        }

        private void OnAboutClicked(object sender, EventArgs e)
        {
            var aboutUrl = "https://mobileapps.green/";
            Intent intent = new Intent(Intent.ActionView, Android.Net.Uri.Parse(aboutUrl));
            MainActivity.ActivityCurrent.StartActivity(intent);
        }

        private void OnHelpClicked(object sender, EventArgs e)
        {
            ShowPopup("Help");
        }

        private void OnPrivacyPolicyClicked(object sender, EventArgs e)
        {
            var privacyUrl = "https://mobileapps.green/privacy-policy";
            Intent intent = new Intent(Intent.ActionView, Android.Net.Uri.Parse(privacyUrl));
            MainActivity.ActivityCurrent.StartActivity(intent);
        }

        private void OnUpgradeClicked(object sender, EventArgs e)
        {
            if (Preferences.Default.ContainsKey("IS_GOLD"))
            {
                if (MIsNotGold)
                {
                    ShowPopup("Upgrade");
                }
            }
        }

        private void OnPositiveClicked(object sender, EventArgs e)
        {
            string title = MFragmentTitle;
            string playStoreUrl = "";

            if (title == "Rate")
            {
                OnRateClicked();
            }
            else if (title == "SpotiFlyer")
            {
                playStoreUrl = "https://play.google.com/store/apps/details?id=com.mvxgreen.spotloader";
            }
            else if (title == "VscoLoader")
            {
                playStoreUrl = "https://play.google.com/store/apps/details?id=xom.xxxgreen.mvx.downloader4vsco";
            }
            else if (title == "SaveFrom")
            {
                playStoreUrl = "https://play.google.com/store/apps/details?id=com.mvxgreen.ytdloader";
            }
            else if (title == "SoundLoader")
            {
                playStoreUrl = "https://play.google.com/store/apps/details?id=com.mvxgreen.downloader4soundcloud";
            }

            if (playStoreUrl != "")
            {
                Intent intent = new Intent(Intent.ActionView, Android.Net.Uri.Parse(playStoreUrl));
                MainActivity.ActivityCurrent.StartActivity(intent);
            }
        }

        private void OnRateClicked(object sender, EventArgs e)
        {
            OnRateClicked();
        }

        private void OnRateClicked()
        {
            var playStoreUrl = "https://play.google.com/store/apps/details?id=green.mobileapps.downloader4inflact"; //Add here the url of your application on the store
            Intent intent = new Intent(Intent.ActionView, Android.Net.Uri.Parse(playStoreUrl));
            //intent.SetPackage("com.android.vending");
            MainActivity.ActivityCurrent.StartActivity(intent);
        }

        public async void ShowPopup(string title)
        {
            MFragmentTitle = title;
            if (title == "Upgrade")
            {
                MFragmentSubtitle = "InSave Gold";
                MFragmentBody = "✅  Profiles\n✅  Collections\n✅  Batch downloads\n✅  No more ads!";
                MFragmentPositive = "Coming Soon";
                MFragmentDismiss = "Nah";
                ((Label)FindByName("fragment_body")).LineHeight = 1.5;
                ((HorizontalStackLayout)FindByName("fragment_btn_layout")).IsVisible = true;
            }
            else if (title == "Help")
            {
                MFragmentSubtitle = "How to Use InSave:";
                MFragmentBody = "➊  Copy a link\n  ⓘ  Open media >> \"Share\" >> \"Copy link\"\n➋  Tap ⚡ (paste into search bar)\n➌  Tap download (⬇)\n  ⓘ  Files saved [in Documents folder]";
                ((Label)FindByName("fragment_body")).LineHeight = 1.25;
                ((HorizontalStackLayout)FindByName("fragment_btn_layout")).IsVisible = false;
            }
            else if (title == "Rate")
            {
                MFragmentSubtitle = "InSave";
                MFragmentBody = "Enjoying the app?\nLet me know!";
                MFragmentPositive = "Rate";
                MFragmentDismiss = "Nah";
                ((Label)FindByName("fragment_body")).LineHeight = 1.25;
                ((HorizontalStackLayout)FindByName("fragment_btn_layout")).IsVisible = true;
            }
            else if (title == "SoundLoader")
            {
                MFragmentSubtitle = "Downloader for Soundcloud";
                MFragmentBody = "You might like this app too\n\n✦Ad by Green Mobile✦";
                MFragmentPositive = "Free";
                MFragmentDismiss = "Nah";
            }
            else if (title == "InSave")
            {
                MFragmentSubtitle = "Downloader for Instagram";
                MFragmentBody = "You might like this too\n\n✦Ad by Green Mobile✦";
                MFragmentPositive = "Free";
                MFragmentDismiss = "Nah";
            }
            else if (title == "SaveFrom")
            {
                MFragmentSubtitle = "Downloader for Videos";
                MFragmentBody = "You might like this too\n\n✦Ad by Green Mobile✦";
                MFragmentPositive = "Free";
                MFragmentDismiss = "Nah";
            }
            else if (title == "VscoLoader")
            {
                MFragmentSubtitle = "Downloader for VSCO";
                MFragmentBody = "You might like this too\n\n✦Ad by Green Mobile✦";
                MFragmentPositive = "Free";
                MFragmentDismiss = "Nah";
            }
            else if (title == "Musi")
            {
                MFragmentSubtitle = "Music Player";
                MFragmentBody = "Your music.\nFree and ad-free, forever.\n\n🎧Ad by Green Mobile🎧";
                MFragmentPositive = "Free";
                MFragmentDismiss = "Nah";
            }

            // show fragment
            AbsoluteLayout fragment = (AbsoluteLayout)FindByName("fragment_layout");
            fragment.Opacity = 0.0;
            fragment.IsVisible = true;
            await fragment.FadeTo(1.0, ANIM_LENGTH);
        }

        private void OnCloseClicked(object sender, EventArgs e)
        {
            HidePopup();
        }

        public async void HidePopup()
        {
            Console.WriteLine($"{Tag} HidePopup");

            // fade out
            AbsoluteLayout fragment = (AbsoluteLayout)FindByName("fragment_layout");
            fragment.Opacity = 1.0;
            await fragment.FadeTo(0.0, ANIM_LENGTH);
            fragment.IsVisible = false;

            // clear text
            MFragmentTitle = "";
            MFragmentSubtitle = "";
            MFragmentBody = "";
            MFragmentPositive = "";
            MFragmentDismiss = "";
        }

        private void OnPasteClicked(object sender, EventArgs e)
        {
            ResetVars();
            ClearTextfield();

            // get clipboard text
            string clip = Clipboard.GetTextAsync().Result;
            Console.WriteLine("clipboard text: " + clip);

            // paste to textfield
            TextField mTextField = (TextField)FindByName("main_textfield");
            mTextField.Text = clip;
        }

        public async Task ClearTextfield()
        {
            TextField mTextField = (TextField)FindByName("main_textfield");
            if (mTextField != null)
            {
                mTextField.Text = "";
            }
        }

        private async void OnDownloadClicked(object sender, EventArgs e)
        {
            OnDownloadClicked();
        }

        public void OnDownloadClicked()
        {
            ShowDownloadingUI();

            // register finish reciever
            MainActivity ma = (MainActivity)Platform.CurrentActivity;
            if ((int)Build.VERSION.SdkInt >= 33)
            {
                ma.RegisterReceiver(MainActivity.MFinishReceiver, new IntentFilter("69"), ReceiverFlags.Exported);
                //ma.RegisterReceiver(MainActivity.MDownloadReceiver, new IntentFilter(DownloadManager.ActionDownloadComplete), ReceiverFlags.Exported);
            }
            else
            {
                ma.RegisterReceiver(MainActivity.MFinishReceiver, new IntentFilter("69"));
                //ma.RegisterReceiver(MainActivity.MDownloadReceiver, new IntentFilter(DownloadManager.ActionDownloadComplete));
            }

            Services.Start();
        }

        private void OnTextChanged(object sender, Microsoft.Maui.Controls.TextChangedEventArgs e)
        {
            Console.WriteLine($"{Tag} OnTextChanged");

            string oldText = e.OldTextValue;
            string newText = e.NewTextValue;
            string input = ((TextField)sender).Text;

            if (input != null)
            {
                int lengthDiff;
                if (oldText == null)
                {
                    lengthDiff = newText.Length;
                }
                else
                {
                    lengthDiff = newText.Length - oldText.Length;
                }

                if (input.Length == 0)
                {
                    Console.WriteLine("text field text cleared");
                    ShowEmptyUI();
                }
                else if (lengthDiff > 1 || lengthDiff == 0)
                {
                    Console.WriteLine("text field text pasted");
                    if (input != null && !Instaloader.MIsShared)
                    {
                        HandleInput(input);
                    }
                }
                else if (lengthDiff == 1)
                {
                    // character typed
                }
                else
                {
                    // character deleted
                }
            }
            else
            {
                Console.WriteLine("input is null!");
            }
        }

        private void OnTextCompleted(object sender, EventArgs e)
        {
            Console.WriteLine("OnTextCompleted");
            string input = ((TextField)FindByName("main_textfield")).Text.ToString();
            HandleInput(input);
        }

        // LOAD / DOWNLOAD
        public void HandleInput(string input)
        {
            // check internet connection
            NetworkAccess accessType = Connectivity.Current.NetworkAccess;
            if (accessType != NetworkAccess.Internet)
            {
                MMessageToast = "Please connect to the internet.";
                Console.WriteLine($"{Tag} {MMessageToast}");
                AndHUD.Shared.ShowError(MainActivity.ActivityCurrent, MMessageToast, MaskType.Black, TimeSpan.FromSeconds(2));
                return;
            }

            // validate input
            var match = Regex.Match(input, INPUT_REGEX, RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                // log invalid input
                try
                {
                    Bundle bun = new();
                    bun.PutString("input", "load");
                    bun.PutBoolean("input_valid", false);
                    bun.PutString("input_text", input);
                    bun.PutString("app_name", "instaloader");
                    FirebaseAnalytics.GetInstance((MainActivity)Platform.CurrentActivity).LogEvent("input_load", bun);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{Tag} failed to log event: {e.Message}");
                }
                return;
            }

            // update ui
            ShowLoadingUI();

            // trim input
            input = input[input.IndexOf("https://")..];
            MInput = input + "&size=1";
            Console.WriteLine($"{Tag} MInput={MInput}");

            // log event
            try
            {
                Bundle bundle = new Bundle();
                bundle.PutString("app_name", "instaloader");
                bundle.PutString("event_name", "input_load");
                bundle.PutBoolean("input_valid", true);
                bundle.PutString("input_text", input);
                FirebaseAnalytics.GetInstance((MainActivity)Platform.CurrentActivity).LogEvent("input_load", bundle);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{Tag} failed to log event: {e.Message}");
            }

            // get id
            if (input.Contains(".com/p/") || input.Contains(".com/reel/"))
            {
                // post
                IgId = input[..input.LastIndexOf('/')];
                if (IgId.Contains('/'))
                {
                    IgId = IgId[(IgId.LastIndexOf('/') + 1)..];

                }
            } else if (input[input.LastIndexOf('/')..].Contains('?'))
            {
                IgId = input[(input.LastIndexOf('/')+1)..input.LastIndexOf('?')];
            } else
            {
                IgId = input[(input.LastIndexOf('/') + 1)..];
            }
            //MTitle = IgId;
            Console.WriteLine($"{Tag} IgId={IgId}");

            // check url type
            if (input.Contains("instagram.com/p/"))
            {
                Console.WriteLine($"{Tag} input is an instagram post");
                MIsProfile = false;
            }
            else if (input.Contains("instagram.com/reel/"))
            {
                Console.WriteLine($"{Tag} input is an instagram reel");
                MIsProfile = false;
            }
            else if (input.Contains("instagram.com/s/"))
            {
                Console.WriteLine($"{Tag} input is an instagram story");
                MIsProfile = false;
            }
            else 
            {
                Console.WriteLine($"{Tag} input is an instagram profile");
                MIsProfile = true;
            }

            // init webview
            pwv = (Microsoft.Maui.Controls.WebView)FindByName("preview_webview");
            pwv.IsEnabled = true;

            // init webview client
            ((IWebViewHandler)pwv.Handler).PlatformView
                .SetWebViewClient(new MWebViewClient());

            // load home url
            //((IWebViewHandler)pwv.Handler).PlatformView
            //    .LoadUrl("https://www.instagram.com/?hl=en");

            // check for session id
            if (MCookies.Contains("sessionid="))
            {
                Console.WriteLine($"{Tag} already logged in! MCookies={MCookies}");

                // load media page
                ((IWebViewHandler)pwv.Handler).PlatformView.Post(() =>
                {
                    ((IWebViewHandler)pwv.Handler).PlatformView
                    .LoadUrl(MInput);
                    // alt: https://www.instagram.com/?flo=true
                });
            }
            else
            {
                Console.WriteLine($"{Tag} not logged in! MCookies={MCookies}");

                // show webview
                pwv.IsVisible = true;

                // load login page
                ((IWebViewHandler)pwv.Handler).PlatformView.Post(() =>
                {
                    ((IWebViewHandler)pwv.Handler).PlatformView
                    .LoadUrl("https://www.instagram.com/accounts/login/?hl=en");
                    // alt: https://www.instagram.com/?flo=true
                });
            }

        }

        private static string ExtractThumbnailUrl(string html)
        {
            Console.WriteLine($"{Tag} ExtractThumbnailUrl");

            // extract content from og:image
            string thumbUrl = "";
            var imgMatch = Regex.Match(html, "<meta property=\"og:image\" content=\"([^\"]+)\"");
            if (imgMatch.Success)
            {
                Console.WriteLine($"{Tag} found thumbnail");
                thumbUrl = imgMatch.Groups[1].Value.ToString();
            }

            // format url 
            if (thumbUrl.Contains("&amp;"))
                thumbUrl = thumbUrl.Replace("&amp;", "&");

            Console.WriteLine($"{Tag} found thumbUrl={thumbUrl}");
            return thumbUrl;
        }

        public static IEnumerable<string> Split(string str, int chunkSize)
        {
            return Enumerable.Range(0, str.Length / chunkSize)
                .Select(i => str.Substring(i * chunkSize, chunkSize));
        }

        public class MWebViewClient : WebViewClient
        {
            private static readonly string Tag = nameof(MWebViewClient);

            public MWebViewClient()
            {
                
            }

            public static List<string> ExtractUrlsFromHtml(string html)
            {
                Console.WriteLine($"{Tag} ExctractUrlsFromHtml");
                if (string.IsNullOrEmpty(html))
                {
                    Console.WriteLine("html is empty!");
                    return new List<string>();
                }

                // Regex to match URLs in href/src attributes and plain text
                var urlPattern = @"(?i)\b((?:https?|ftp):\/\/[^\s""'<>]+)";
                

                if (html.Contains("https:\\\\/\\\\/")) { 
                    html = html.Replace("https:\\\\/\\\\/", "https://");
                }

                if (html.Contains("\\u0026"))
                {
                    html = html.Replace("https:\\\\/\\\\/", "https://");
                }

                var matches = Regex.Matches(html, urlPattern);
                Console.WriteLine($"{Tag} matches={matches.Count}");
                var urls = new List<string>();
                foreach (Match match in matches)
                {
                    if (match.Success)
                    {
                        // get image and video URLs
                        if (match.Value.Contains(".jpg?") || match.Value.Contains(".mp4?"))
                        {
                            // format url
                            var url = match.Value;
                            url = url.Replace("\\%", "%");
                            url = url.Replace("\\u0025", "%");
                            url = url.Replace("\\u0026", "&");
                            url = url.Replace("\\\\", "");
                            url = url.Replace("&amp;", "&");
                            if (url.Contains('\\'))
                            {
                                url = url.Replace("\\", "");
                            }

                            // filter out low resolutions and duplicates
                            if (!urls.Contains(url)
                                && !url.Contains("1080x1080")
                                && !url.Contains("720x720")
                                && !url.Contains("640x640")
                                && !url.Contains("480x480")
                                && !url.Contains("320x320")
                                && !url.Contains("240x240")
                                && !url.Contains("150x150")
                                && !url.Contains(".jpg?_nc_ht")
                                && !url.Contains("BaseURL"))
                            {
                                urls.Add(url);
                            }
                        }
                    }       
                }
                Console.WriteLine($"{Tag} extracted urls count: {urls.Count}");
                return urls;
            }

            public override void OnPageFinished(Android.Webkit.WebView? view, string? url)
            {
                Console.WriteLine($"{Tag} OnPageFinished url={url}");

                // save cookies
                MCookies = CookieManager.Instance.GetCookie(url);
                Preferences.Default.Set("COOKIES", MCookies);
                Console.WriteLine($"{Tag} MCookies={MCookies}");

                // set width cookie
                CookieManager.Instance.SetCookie("https://www.instagram.com/?hl=en", "wd=1680x881");
                CookieManager.Instance.SetCookie(MInput, "wd=1680x881");
                Console.WriteLine($"{Tag} adjusted width MCookies={MCookies}");

                if (url.Contains(".com/accounts/login"))
                {
                    Console.WriteLine($"{Tag} hiding progress & image...");
                    MainPage mp = (MainPage)Shell.Current.CurrentPage;
                    var pmv = (Microsoft.Maui.Controls.WebView)mp.FindByName("preview_webview");

                    // scroll to bottom
                    pmv.EvaluateJavaScriptAsync("(function() { window.scrollTo(0, document.body.scrollHeight); })();");

                    // hide progress & thumbnail
                    ProgressRing pr = (ProgressRing)mp.FindByName("progress_ring");
                    Image previewImg = (Image)mp.FindByName("preview_img");
                    Label pl = (Label)mp.FindByName("progress_label");
                    pr.IsVisible = false;
                    pl.IsVisible = false;
                    previewImg.IsVisible = false;

                    // show webview
                    Console.WriteLine($"{Tag} showing webview...");
                    Microsoft.Maui.Controls.WebView pwv = (Microsoft.Maui.Controls.WebView)mp.FindByName("preview_webview");
                    pwv.IsVisible = true;
                } else
                {
                    MainPage mp = (MainPage)Shell.Current.CurrentPage;

                    // hide webview
                    Console.WriteLine($"{Tag} hiding webview...");
                    Microsoft.Maui.Controls.WebView pwv = (Microsoft.Maui.Controls.WebView)mp.FindByName("preview_webview");
                    pwv.IsVisible = false;

                    // show progress & thumbnail
                    ProgressRing pr = (ProgressRing)mp.FindByName("progress_ring");
                    Image previewImg = (Image)mp.FindByName("preview_img");
                    Label pl = (Label)mp.FindByName("progress_label");
                    pr.IsVisible = true;
                    pl.IsVisible = true;
                    previewImg.IsVisible = true;
                }

                if (!url.Contains(".com/accounts/login") && !url.Contains("instagram.com/?"))
                {
                    Console.WriteLine($"{Tag} finished loading content page url={url} MIsAlreadyLoading={MIsAlreadyLoading}");
                    Console.WriteLine($"{Tag} hiding webview...");

                    // get webview
                    MainPage mp = (MainPage)Shell.Current.CurrentPage;
                    var pmv = (Microsoft.Maui.Controls.WebView)mp.FindByName("preview_webview");

                    // remove self from webview when finished
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        // get html with javascript
                        var res = await ((Microsoft.Maui.Controls.WebView)pmv).EvaluateJavaScriptAsync("(function() { return ('<html>'+document.getElementsByTagName('html')[0].innerHTML+'</html>'); })();");

                        if (res == null || res.Length == 0)
                        {
                            Console.WriteLine($"{Tag} empty js html!");
                            return;
                        }

                        // print html
                        IEnumerable<string> htmlChunks = Split(res, 3500);
                        Console.WriteLine($"{Tag} JS HTML:");
                        foreach (string v in htmlChunks)
                        {
                            Console.WriteLine($"{Tag} {v}");
                        }

                        // extract thumbnail
                        mp.MThumbnailUrl = ExtractThumbnailUrl(res);
                        Console.WriteLine($"{Tag} MThumbnailUrl={mp.MThumbnailUrl}");

                        // extract profile name
                        string pn = "";
                        if (res.Contains("og:url"))
                        {
                            pn = res[res.IndexOf("og:url")..];
                            pn = pn[(pn.IndexOf("instagram.com/") + 14)..];
                            pn = pn[..pn.IndexOf('/')];
                            if (pn.Contains('\\'))
                            {
                                pn = pn[..pn.IndexOf('\\')];
                            }
                        }
                        mp.MTitle = pn;
                        Console.WriteLine($"{Tag} extracted profile name MTitle={mp.MTitle}");

                        Bundle bundle = new Bundle();
                        bundle.PutString("app_name", "instaloader");
                        bundle.PutString("event_name", "input_loaded");
                        bundle.PutString("input_url", url);
                        bundle.PutString("filename", mp.MTitle);
                        FirebaseAnalytics.GetInstance((MainActivity)Platform.CurrentActivity).LogEvent("input_load", bundle);

                        // extract download urls
                        MDownloadUrls = ExtractUrlsFromHtml(res);
                        foreach (string url in MDownloadUrls)
                        {
                            Console.WriteLine($"{Tag} found content url: {url}");
                        }

                        // return if empty
                        if (MDownloadUrls == null || MDownloadUrls.Count == 0)
                        {
                            return;
                        }

                        // update thumbnail
                        ((MainPage)Shell.Current.CurrentPage).MThumbnailUrl = MDownloadUrls.FirstOrDefault();

                        // hide webview
                        Console.WriteLine($"{Tag} hiding webview...");
                        Microsoft.Maui.Controls.WebView pwv = (Microsoft.Maui.Controls.WebView)mp.FindByName("preview_webview");
                        pwv.IsVisible = false;

                        // show progress & thumbnail
                        ProgressRing pr = (ProgressRing)mp.FindByName("progress_ring");
                        Image previewImg = (Image)mp.FindByName("preview_img");
                        Label pl = (Label)mp.FindByName("progress_label");
                        pr.IsVisible = true;
                        pl.IsVisible = true;
                        previewImg.IsVisible = true;

                        // start download if shared
                        if (Instaloader.MIsShared)
                        {
                            mp.OnDownloadClicked();
                        }
                        else
                        {
                            ((MainPage)Shell.Current.CurrentPage).ShowPreviewUI();
                        }
                    });


                }

                base.OnPageFinished(view, url);
            }

            public override bool ShouldOverrideUrlLoading(Android.Webkit.WebView? view, string? url)
            {
                return false;
            }

            public override WebResourceResponse? ShouldInterceptRequest(global::Android.Webkit.WebView? view, IWebResourceRequest? request)
            {
                Console.WriteLine($"{Tag} ShouldInterceptRequest request url={request.Url.ToString()}");

                // check if logged in, or if already loading media page
                if (request.Url.ToString().Contains("graphql/query")
                && MCookies.Contains("sessionid=")
                && !MIsAlreadyLoading)
                {
                    // load media page
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Console.WriteLine($"{Tag} loading MInput={MInput}");
                        MIsAlreadyLoading = true;
                        view.Settings.CacheMode = CacheModes.NoCache;
                        view.Settings.UseWideViewPort = true;
                        //view.Settings.LoadWithOverviewMode = true;
                        view.Settings.JavaScriptEnabled = true;
                        view.LoadUrl(MInput);
                    });
                }

                return base.ShouldInterceptRequest(view, request);
            }

        }
    
    }

}
