using System.Text.RegularExpressions;
using UraniumUI.Material.Controls;
using MPowerKit.ProgressRing;
using Firebase;
using Microsoft.Maui.Handlers;
using InstaLoaderMaui.Platforms.Android;
using Android.Util;
using Android.Webkit;


#if ANDROID
using Android.Content;
using Android.Views.InputMethods;
using Android.OS;
using AndroidHUD;
using Firebase.Analytics;
using Green.Mobileapps.Instaloader;
#endif

namespace InstaLoaderMaui
{
    public partial class MainPage : ContentPage
    {
        private static readonly string Tag = nameof(MainPage);

        public Microsoft.Maui.Controls.WebView pwv;

        private uint ANIM_LENGTH = 400;
        private readonly string INPUT_REGEX = "^$|((?:https?:\\/\\/)((?:www\\.)|(?:m\\.))?instagram\\.com\\/)";

        public static InstaLoader MInstaLoader;
        public static string AbsPathDocs = "";
        public static string AbsPathDocsTemp = "";
        public static String PostId = ""; // post id to download

        public static string AdmobIdApp = "ca-app-pub-7417392682402637~9405504691";
        public static string admobIdInterTest = "ca-app-pub-3940256099942544/1033173712";
        public static string admobIdInterReal = "ca-app-pub-7417392682402637/1763043737";
        public static string admobIdBannerTest = "ca-app-pub-3940256099942544/9214589741";
        public static string admobIdBannerReal = "ca-app-pub-7417392682402637/9820437660";
        public static string admobIdInter = admobIdInterTest;
        public string mAdmobIdBanner = admobIdBannerTest;
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
                OnPropertyChanged("MAdmobIdBanner");
            }
        }

        public IServiceDownload Services;
        public static int successfulRuns = 0;
        string currentText = "";
        public static bool MFailedShowInter = false;
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

            // check gold
            MIsNotGold = !Preferences.Default.Get("IS_GOLD", false);
            Console.WriteLine($"{Tag}, IS_GOLD={!MIsNotGold}");

#if ANDROID
            // init firebase
            FirebaseApp.InitializeApp(MainActivity.ActivityCurrent);
            // FirebaseAnalytics.GetInstance(MainActivity.ActivityCurrent);

            // init admob
            MainActivity.ActivityCurrent.LoadAdmob();
#endif

            // prepare destination file dirs
            PrepareFileDirs();
        }

        public static void PrepareFileDirs()
        {
            Console.WriteLine($"{Tag}: {nameof(PrepareFileDirs)}");

            // init InstaLoader
            MInstaLoader = new InstaLoader(Android.App.Application.Context);

            // set destination paths
            AbsPathDocs = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, Android.OS.Environment.DirectoryDocuments);
            AbsPathDocsTemp = AbsPathDocs + "/temp";

            // prepare destination file dirs
            Java.IO.File files = new Java.IO.File(AbsPathDocs);
            files.SetWritable(true);
            Directory.CreateDirectory(Path.GetDirectoryName(AbsPathDocs));

            // prepare temp dir
            Java.IO.File temp_files = new Java.IO.File(AbsPathDocsTemp);
            temp_files.SetWritable(true);
            Directory.CreateDirectory(Path.GetDirectoryName(AbsPathDocsTemp));

            Console.WriteLine($"{Tag}: {nameof(PrepareFileDirs)} AbsPathDocs={AbsPathDocs} AbsPathDocsTemp={AbsPathDocsTemp}");
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

#if ANDROID
            if (null != pwv)
            {
                ((IWebViewHandler)pwv.Handler).PlatformView.SetWebViewClient(null);
                ((IWebViewHandler)pwv.Handler).PlatformView.Destroy();
                pwv = null;
            }
#endif
        }

        

        public static void ResetVars()
        {
            Console.WriteLine($"{Tag} ResetVars");
            MainPage mp = (MainPage)Shell.Current.CurrentPage;
            MainPage.MFailedShowInter = false;
            mp.MThumbnailUrl = "";
            mp.MTitle = "";
            mp.MArtist = "";
            mp.MMessageProgress = "";
            mp.MMessageToast = "";
        }

        // BILLING 
        public void UpdateUpgradeItem()
        {
            Console.WriteLine($"{Tag} UpdateUpgradeItem()\nMIsNotGold={MIsNotGold}");
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

            // hide buttons
            ButtonView finishBtn = (ButtonView)FindByName("finish_btn");
            ButtonView dlBtn = (ButtonView)FindByName("dl_btn");
            dlBtn.Opacity = 0.0;
            finishBtn.Opacity = 0.0;
            finishBtn.IsVisible = false;
            ((Image)FindByName("preview_img")).Opacity = 0.0;
            ((ButtonView)FindByName("dl_btn")).Opacity = 0.0;
            ((ProgressRing)FindByName("progress_ring")).Opacity = 0.0;
            ((Label)FindByName("progress_label")).Opacity = 0.0;
            ((Frame)FindByName("downloader_frame")).Opacity = 0.0;
        }

        public async Task ShowPreparingUI()
        {
            Console.WriteLine($"{Tag}: ShowPreparingUI");

            HideKeyboard();

            // change progress message
            MMessageProgress = "Loading…";

            // show interstitial if not gold
#if ANDROID
            if (MIsNotGold)
            {
                // check if interstitial is loaded
                // TODO
                /* bool IsInterLoaded = CrossMauiMTAdmob.Current.IsInterstitialLoaded();


                // show or load interstitial
                if (IsInterLoaded)
                {
                    MFailedShowInter = false;
                    // log event
                    try
                    {
                        Bundle b = new Bundle();
                        b.PutString("admob", "show_interstitial");
                        b.PutBoolean("is_loaded", IsInterLoaded);
                        b.PutString("app_name", "soundloader");
                        FirebaseAnalytics.GetInstance((MainActivity)Platform.CurrentActivity).LogEvent("admob_show_inter", b);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{Tag} failed to log event: {e.Message}");
                    }

                    CrossMauiMTAdmob.Current.ShowInterstitial();
                
                }
                else
                {
                    Log.Error(Tag, "interstitial not loaded!");

                    MFailedShowInter = true;

                    // log event
                    try
                    {
                        Bundle bundle = new Bundle();
                        bundle.PutString("admob", "failed_show");
                        bundle.PutString("app_name", "soundloader");
                        bundle.PutString("ad_type", "interstitial");
                        bundle.PutString("app_name", "soundloader");
                        FirebaseAnalytics.GetInstance((MainActivity)Platform.CurrentActivity).LogEvent("admob_failed_show", bundle);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine($"{Tag} failed to log event");
                    }
                }
                // load next interstitial
                CrossMauiMTAdmob.Current.LoadInterstitial(admobIdInter);
                */
            }
#endif

            // show indeterminate progress ring
            ProgressRing pr = (ProgressRing)FindByName("progress_ring");
            pr.IsIndeterminate = true;
            pr.FadeTo(1.0, ANIM_LENGTH);
            await ((Label)FindByName("progress_label")).FadeTo(1.0, ANIM_LENGTH);
        }

        public async Task ShowPreviewUI()
        {
            Console.WriteLine($"{Tag}: ShowPreviewUI");

            /* destroy webview
            if (null != pwv)
            {
                ((IWebViewHandler)pwv.Handler).PlatformView.SetWebViewClient(null);
                ((IWebViewHandler)pwv.Handler).PlatformView.Destroy();
                pwv = null;
            }
            */

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
            ((ProgressRing)FindByName("progress_ring")).Opacity = 0.0;
            ((Label)FindByName("progress_label")).Opacity = 0.0;

            // increase thumbnail opacity
            ((Image)FindByName("preview_img")).Opacity = 1.0;


        }

        public async Task ShowDownloadingUI()
        {
            Console.WriteLine($"{Tag}: ShowDownloadingUI");

#if ANDROID
            // TODO 
            /*
            // retry showing interstitial, if necessary
            if (MFailedShowInter)
            {
                // show or load interstitial
                if (CrossMauiMTAdmob.Current.IsInterstitialLoaded())
                {
                    MFailedShowInter = false;

                    // log event
                    try
                    {
                        Bundle b = new Bundle();
                        b.PutString("admob", "show_interstitial");
                        b.PutBoolean("is_loaded", true);
                        b.PutString("app_name", "soundloader");
                        FirebaseAnalytics.GetInstance((MainActivity)Platform.CurrentActivity).LogEvent("admob_show_inter", b);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{Tag} failed to log event: {e.Message}");
                    }

                    CrossMauiMTAdmob.Current.ShowInterstitial();
                }
            }
            else
            {
                Log.Error(Tag, "interstitial not loaded!");

                MFailedShowInter = true;

                // log event
                try
                {
                    Bundle bundle = new Bundle();
                    bundle.PutString("admob", "failed_show");
                    bundle.PutString("app_name", "soundloader");
                    bundle.PutBoolean("is_loaded", false);
                    bundle.PutString("ad_type", "interstitial");
                    bundle.PutString("app_name", "soundloader");
                    FirebaseAnalytics.GetInstance((MainActivity)Platform.CurrentActivity).LogEvent("admob_failed_show", bundle);
                }
                catch (Exception)
                {
                    Console.WriteLine($"{Tag} failed to log event");
                }
            }
            */
#endif

            // change progress message
            MMessageProgress = "Starting download…\nThis may take a moment.";

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
            Console.WriteLine($"{Tag}: ShowFinishUI");

            // count successful runs
            int runs = 1;
            if (Preferences.Default.ContainsKey("SUCCESSFUL_RUNS"))
            {
                runs += Preferences.Default.Get("SUCCESSFUL_RUNS", 0);
            }
            successfulRuns = runs;
            Console.WriteLine($"{Tag} SUCCESSFUL_RUNS={runs}");
            Preferences.Default.Set("SUCCESSFUL_RUNS", runs);

            // show rate fragment, sometimes
            if (MIsNotGold && (successfulRuns == 1 || successfulRuns % 3 == 0))
            {
                OpenFragment("Rate");
            }

            // show success message
            MMessageToast = $"Saved! In {AbsPathDocs}";
#if ANDROID
            AndHUD.Shared.ShowSuccess(MainActivity.ActivityCurrent, MMessageToast, MaskType.Black, TimeSpan.FromMilliseconds(2500));
#endif

            ProgressRing prd = (ProgressRing)FindByName("progress_ring_dlr");
            ButtonView finishBtn = (ButtonView)FindByName("finish_btn");

            // hide downloading UI
            ((Image)FindByName("preview_img")).FadeTo(0.85, ANIM_LENGTH);
            prd.FadeTo(0.0, ANIM_LENGTH);
            ((ProgressRing)FindByName("progress_ring")).FadeTo(0.0, ANIM_LENGTH);
            await ((Label)FindByName("progress_label")).FadeTo(0.0, ANIM_LENGTH);

            // show finish ui
            prd.IsVisible = false;
            finishBtn.IsVisible = true;
            finishBtn.FadeTo(1.0, ANIM_LENGTH);
        }
        public void HideKeyboard()
        {
#if ANDROID
            // hide keyboard
            var inputMethodManager = MainActivity.ActivityCurrent.GetSystemService(Context.InputMethodService) as InputMethodManager;
            if (inputMethodManager != null && MainActivity.ActivityCurrent is Android.App.Activity)
            {
                var activity = MainActivity.ActivityCurrent as Android.App.Activity;
                var token = activity.CurrentFocus?.WindowToken;
                inputMethodManager.HideSoftInputFromWindow(token, HideSoftInputFlags.None);

                activity.Window.DecorView.ClearFocus();
            }
#endif
        }

        private void OnAboutClicked(object sender, EventArgs e)
        {
            Console.WriteLine($"{Tag} OnAboutClicked");
            OpenFragment("About");
        }

        private void OnHelpClicked(object sender, EventArgs e)
        {
            Console.WriteLine($"{Tag} OnHelpClicked");
            OpenFragment("Help");
        }

        private void OnPrivacyPolicyClicked(object sender, EventArgs e)
        {
            Console.WriteLine($"{Tag} OnPrivacyPolicyClicked");
            // TODO open privacy policy
        }

        private void OnUpgradeClicked(object sender, EventArgs e)
        {
            Console.WriteLine($"{Tag} OnUpgradeClicked");
            if (Preferences.Default.ContainsKey("IS_GOLD"))
            {
                if (MIsNotGold)
                {
                    OpenFragment("Upgrade");
                }
                else
                {
                    // TODO open gold fragment
                }
            }

        }

        private void OnPositiveClicked(object sender, EventArgs e)
        {
            Console.WriteLine($"{Tag}: OnPurchaseClicked");

            if (MFragmentPositive == "Rate")
            {
                OnRateClicked();
            }
            else
            {
                Console.WriteLine($"{Tag} OnPurchaseClicked");
#if ANDROID
                // TODO
                // MainActivity.ActivityCurrent.LaunchBillingFlow();
#endif
            }
        }

        private void OnRateClicked(object sender, EventArgs e)
        {
            OnRateClicked();
        }

        private void OnRateClicked()
        {
            Console.WriteLine($"{Tag}: OnRateClicked");

#if ANDROID
            // open listing on google play
            var playStoreUrl = "https://play.google.com/store/apps/details?id=com.mvxgreen.downloader4soundcloud"; //Add here the url of your application on the store
            Intent intent = new Intent(Intent.ActionView, Android.Net.Uri.Parse(playStoreUrl));
            //intent.SetPackage("com.android.vending");
            MainActivity.ActivityCurrent.StartActivity(intent);
#endif
        }

        public void OpenFragment(string title)
        {
            Console.WriteLine($"{Tag}: OpenFragment({title})");

            // fill views
            MFragmentTitle = title;
            if (title == "Upgrade")
            {
                MFragmentSubtitle = "SoundLoader Gold";
                MFragmentBody = "✅  Playlists\n✅  Albums\n✅  Fastest download speed\n✅  No more ads!";
                MFragmentPositive = "Get It!";
                MFragmentDismiss = "Nah";
                ((Label)FindByName("fragment_body")).LineHeight = 1.5;
                ((HorizontalStackLayout)FindByName("fragment_btn_layout")).IsVisible = true;
            }
            else if (title == "Help")
            {
                MFragmentSubtitle = "How to Use SoundLoader";
                MFragmentBody = "➊  Copy a SoundCloud track link\n    ⓘ  Open track > \"Share\" > \"Copy link\"\n➋  Tap ⚡ (or paste link into searchbar)\n➌  Tap ⬇ when finished loading\n    ⓘ  Downloads in documents folder";
                ((Label)FindByName("fragment_body")).LineHeight = 1.25;
                ((HorizontalStackLayout)FindByName("fragment_btn_layout")).IsVisible = false;
            }
            else if (title == "Rate")
            {
                MFragmentSubtitle = "SoundLoader";
                MFragmentBody = "Enjoying the app?\nLet me know!";
                MFragmentPositive = "Rate";
                MFragmentDismiss = "Nah";
                ((Label)FindByName("fragment_body")).LineHeight = 1.25;
                ((HorizontalStackLayout)FindByName("fragment_btn_layout")).IsVisible = true;
            }

            // show fragment
            Microsoft.Maui.Controls.AbsoluteLayout fragment = (Microsoft.Maui.Controls.AbsoluteLayout)FindByName("fragment_layout");
            fragment.Opacity = 0.0;
            fragment.IsVisible = true;
            fragment.FadeTo(1.0, ANIM_LENGTH);
        }

        private void OnCloseClicked(object sender, EventArgs e)
        {
            CloseFragment();
        }

        public void CloseFragment()
        {
            Console.WriteLine($"{Tag}: CloseFragment");
            ((AbsoluteLayout)FindByName("fragment_layout")).IsVisible = false;
            // clear views
            MFragmentTitle = "";
            MFragmentSubtitle = "";
            MFragmentBody = "";
            MFragmentPositive = "";
            MFragmentDismiss = "";
        }

        private void OnPasteClicked(object sender, EventArgs e)
        {
            Console.WriteLine("OnPasteClicked");

            Task.Run(async () =>
            {
                //ResetVars();
                await ClearTextfield();
                // give ontextchanged handler time to call showEmptyUI
                await Task.Delay(250);

                string clip = Clipboard.GetTextAsync().Result;
                Console.WriteLine("clipboard text: " + clip);

                TextField mTextField = (TextField)FindByName("main_textfield");
                mTextField.Text = clip;
            });
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
            ShowDownloadingUI();

            // download input
            var input = main_textfield.Text?.Trim();
            DownloadPost(input);
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
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
                    currentText = "";
                    ShowEmptyUI();
                }
                else if (lengthDiff > 1 || lengthDiff == 0)
                {
                    Console.WriteLine("text field text pasted");
                    currentText = input;
                    LoadInput(input);
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
            LoadInput(input);
        }

        // LOAD / DOWNLOAD
        public void LoadInput(string input)
        {
            // check internet connection
            NetworkAccess accessType = Connectivity.Current.NetworkAccess;
            if (accessType != NetworkAccess.Internet)
            {
                MMessageToast = "Please connect to the internet.";
                Console.WriteLine($"{Tag} {MMessageToast}");
#if ANDROID
                AndHUD.Shared.ShowError(MainActivity.ActivityCurrent, MMessageToast, MaskType.Black, TimeSpan.FromSeconds(2));
#endif
                return;
            }

            // validate input
            var match = Regex.Match(input, INPUT_REGEX, RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                Console.WriteLine($"{Tag} input invalid");

                // log event
                try
                {
                    Bundle bun = new();
                    bun.PutString("input", "load");
                    bun.PutBoolean("input_valid", false);
                    bun.PutString("input_text", input);
                    bun.PutString("app_name", "soundloader");
                    FirebaseAnalytics.GetInstance((MainActivity)Platform.CurrentActivity).LogEvent("input_load", bun);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{Tag} failed to log event: {e.Message}");
                }
                return;
            }

            // update ui
            ShowPreparingUI();

            // trim input
            input = input[input.IndexOf("https://")..];
            string domain = input[(input.IndexOf("https://") + 8)..];
            if (domain.Contains('/'))
            {
                domain = domain[..domain.IndexOf('/')];
            }

            // log event
            try
            {
                Bundle bundle = new Bundle();
                bundle.PutString("input", "load");
                bundle.PutBoolean("input_valid", true);
                bundle.PutString("input_text", input);
                bundle.PutString("input_domain", domain);
                bundle.PutString("app_name", "soundloader");
                FirebaseAnalytics.GetInstance((MainActivity)Platform.CurrentActivity).LogEvent("input_load", bundle);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{Tag} failed to log event: {e.Message}");
            }
            // load url in webview
            MainThread.BeginInvokeOnMainThread(() => {
                pwv = (Microsoft.Maui.Controls.WebView)FindByName("preview_webview");
                ((IWebViewHandler)pwv.Handler).PlatformView
                    .SetWebViewClient(new MWebViewClient());
                ((IWebViewHandler)pwv.Handler).PlatformView.Post(() =>
                {
                    ((IWebViewHandler)pwv.Handler).PlatformView
                    .LoadUrl(input);
                });
            });

            try {
                Task.Run(async () =>
                {
                    MThumbnailUrl = await GetPostThumbnailUrl(input);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        ShowPreviewUI();
                    });

                });
            }
            catch (Exception e)
            {
                Console.WriteLine($"{Tag} failed to load url in webview: {e.Message}");
            }
            
        }

        
        private void DownloadProfile(string postUrl)
        {
            // TODO download profile if gold
        }

        private void DownloadPost(string postUrl)
        {
            // validate input
            var match = Regex.Match(postUrl, INPUT_REGEX, RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                Console.WriteLine($"{Tag} input invalid");

                // log event
#if ANDROID
                try
                {
                    Bundle bun = new();
                    bun.PutString("input", "load");
                    bun.PutBoolean("input_valid", false);
                    bun.PutString("input_text", postUrl);
                    bun.PutString("app_name", "soundloader");
                    FirebaseAnalytics.GetInstance((MainActivity)Platform.CurrentActivity).LogEvent("input_load", bun);
                }
                catch (Exception er)
                {
                    Console.WriteLine($"{Tag} failed to log event: {er.Message}");
                }
#endif
                return;
            }

            // register finish reciever
            MainActivity ma = (MainActivity)Platform.CurrentActivity;
            if ((int)Build.VERSION.SdkInt >= 33)
            {
                ma.RegisterReceiver(MainActivity.MFinishReceiver, new IntentFilter("69"), ReceiverFlags.Exported);
            }
            else
            {
                ma.RegisterReceiver(MainActivity.MFinishReceiver, new IntentFilter("69"));
            }

            // trim to post id
            if (postUrl.Contains("instagram.com/p/"))
            {
                postUrl = postUrl[(postUrl.IndexOf("instagram.com/p/") + 16)..];
                if (postUrl.Contains('/'))
                {
                    postUrl = postUrl[..postUrl.IndexOf('/')];
                }
            }
            else
            {
                Console.WriteLine($"{Tag} input is not an instagram post");
                return;
            }

            PostId = postUrl;

            Services.Start();
        }

        private async Task<string?> GetPostThumbnailUrl(string postUrl)
        {
            Console.WriteLine($"{Tag} GetThumbnailUrl postUrl={postUrl}");
            using var httpClient = new HttpClient();
            var html = await httpClient.GetStringAsync(postUrl);

            // print html
            IEnumerable<string> htmlChunks = Split(html, 3500);
            foreach (string v in htmlChunks)
            {
                Console.WriteLine($"{Tag} HTML:");
                Console.WriteLine($"{Tag} {v}");
            }

            // Look for og:image or og:video meta tags
            string turl = "";
            var imgMatch = Regex.Match(html, "<meta property=\"og:image\" content=\"([^\"]+)\"");
            if (imgMatch.Success)
                turl = imgMatch.Groups[1].Value.ToString();

            var videoMatch = Regex.Match(html, "<meta property=\"og:video\" content=\"([^\"]+)\"");
            if (videoMatch.Success)
                turl = videoMatch.Groups[1].Value.ToString();

            if (turl.Contains("&amp;"))
                turl = turl.Replace("&amp;", "&");

            Console.WriteLine($"{Tag} found thumbnail turl={turl}");

            return turl;
        }

        public static IEnumerable<string> Split(string str, int chunkSize)
        {
            return Enumerable.Range(0, str.Length / chunkSize)
                .Select(i => str.Substring(i * chunkSize, chunkSize));
        }

        public class MWebViewClient : WebViewClient
        {
            private static readonly string Tag = nameof(MWebViewClient);

            // TODO track cookie for login session (USER?)

            public override WebResourceResponse? ShouldInterceptRequest(global::Android.Webkit.WebView? view, IWebResourceRequest? request)
            {
                string url = request.Url.ToString();
                Console.WriteLine($"{Tag} ShouldInterceptRequest url={url}");
                MainPage mp = (MainPage)Shell.Current.CurrentPage;

                // remove self from webview when finished intercepting request
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ((IWebViewHandler)mp.pwv.Handler).PlatformView.SetWebViewClient(null);
                });

                return base.ShouldInterceptRequest(view, request);
            }

        }
    
    }

}
