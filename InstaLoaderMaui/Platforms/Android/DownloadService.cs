using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using static InstaLoaderMaui.MainActivity;

namespace InstaLoaderMaui.Platforms.Android
{
    [Service]
    public class DownloadService : Service, IServiceDownload
    {
        private static string Tag = nameof(DownloadService);

        public static DownloadReceiver MDownloadReceiver = new DownloadReceiver();

        public const int NOTIFICATION_ID = 3699;
        const string channelId = "instaloader_channel";
        const string channelName = "InstaLoader";
        const string channelDescription = "InstaLoader's channel for notifications.";
        const string notificationTitle = "Downloading…";

        int max_progress = 100;
        int progress = 0;

        bool channelInitialized = false;
        int messageId = 0;
        int pendingIntentId = 0;

        PendingIntent pendingIntent;

        public List<string> inputs = new List<string>();

        public override IBinder OnBind(Intent intent)
        {
            Console.WriteLine($"{Tag} OnBind");

            return null;
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            Console.WriteLine($"{Tag} OnStartCommand");

            if (intent.Action == "START_SERVICE")
            {
                RegisterReceiver();

                Task.Run(async () =>
                {
                    // download media
                    for (int i = 0; i < MainPage.MDownloadUrls.Count; i++)
                    {
                        //Task.Delay(333).Wait();
                        await Instaloader.DownloadFile(MainPage.MDownloadUrls[i], i);
                    }
                });

            }
            else if (intent.Action == "STOP_SERVICE")
            {
                StopForeground(true);
                StopSelfResult(startId);
            }

            return StartCommandResult.NotSticky;
        }

        //Start and Stop Intents, set the actions for the MainActivity to get the state of the foreground service
        //Setting one action to start and one action to stop the foreground service
        public void Start()
        {
            Console.WriteLine($"{Tag} Start()");
            Intent startService = new Intent(MainActivity.ActivityCurrent, typeof(DownloadService));
            startService.SetAction("START_SERVICE");
            MainActivity.ActivityCurrent.StartService(startService);
        }

        public void Stop()
        {
            Console.WriteLine($"{Tag} Stop()");
            Intent stopIntent = new Intent(MainActivity.ActivityCurrent, Class);
            stopIntent.SetAction("STOP_SERVICE");
            MainActivity.ActivityCurrent.StartService(stopIntent);
        }

        private void RegisterReceiver()
        {
            MDownloadReceiver = new DownloadReceiver();
            if ((int)Build.VERSION.SdkInt >= 33)
            {
                //MainActivity.ActivityCurrent.RegisterReceiver(MainActivity.MFinishReceiver, new IntentFilter("69"), Android.Content.ReceiverFlags.Exported);
                RegisterReceiver(MDownloadReceiver, new IntentFilter(DownloadManager.ActionDownloadComplete), ReceiverFlags.Exported);
            }
            else
            {
                //MainActivity.ActivityCurrent.RegisterReceiver(MainActivity.MFinishReceiver, new IntentFilter("69"));
                RegisterReceiver(MDownloadReceiver, new IntentFilter(DownloadManager.ActionDownloadComplete));
            }
        }

        private void RegisterNotification()
        {
            Console.WriteLine($"{Tag} RegisterNotification");

            Intent intent = new Intent(Platform.AppContext, typeof(MainActivity));
            //intent.PutExtra(TitleKey, title);
            //intent.PutExtra(MessageKey, message);
            intent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);

            var pendingIntentFlags = Build.VERSION.SdkInt >= BuildVersionCodes.S
                ? PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
                : PendingIntentFlags.UpdateCurrent;

            pendingIntent = PendingIntent.GetActivity(Platform.AppContext, pendingIntentId++, intent, pendingIntentFlags);

            NotificationChannel channel = new NotificationChannel(channelId, channelName, NotificationImportance.Max);
            NotificationManager manager = (NotificationManager)MainActivity.ActivityCurrent.GetSystemService(NotificationService);
            manager.CreateNotificationChannel(channel);
            Notification notification = new Notification.Builder(this, channelId)
               .SetContentTitle(notificationTitle)
               .SetSmallIcon(Resource.Drawable.material_ic_menu_arrow_down_black_24dp)
               .SetProgress(max_progress, progress, false)
               .SetOngoing(true)
               .SetContentIntent(pendingIntent)
               .Build();

            manager.Notify(NOTIFICATION_ID, notification);
            //StartForeground(NOTIFICATION_ID, notification);
        }

        public void UpdateNotification(int progress, int max_progress)
        {
            Console.WriteLine($"{Tag} UpdateNotification");

            this.progress = progress;
            this.max_progress = max_progress;

            Intent intent = new Intent(Platform.AppContext, typeof(MainActivity));
            //intent.PutExtra(TitleKey, title);
            //intent.PutExtra(MessageKey, message);
            //intent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);

            var pendingIntentFlags = Build.VERSION.SdkInt >= BuildVersionCodes.S
                ? PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
                : PendingIntentFlags.UpdateCurrent;

            pendingIntent = PendingIntent.GetActivity(Platform.AppContext, pendingIntentId++, intent, pendingIntentFlags);

            var notification = GetNotification(pendingIntent);

            NotificationManager notificationManager = (NotificationManager)GetSystemService(NotificationService);
            notificationManager.Notify(NOTIFICATION_ID, notification);
        }

        Notification GetNotification(PendingIntent pIntent)
        {
            Console.WriteLine($"{Tag} GetNotification");

            if (pendingIntent == null)
            {
                Intent i = new Intent(Platform.AppContext, typeof(MainActivity));
                //intent.PutExtra(TitleKey, title);
                //intent.PutExtra(MessageKey, message);
                //intent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);

                var pendingIntentFlags = Build.VERSION.SdkInt >= BuildVersionCodes.S
                    ? PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
                    : PendingIntentFlags.UpdateCurrent;
                pendingIntent = PendingIntent.GetActivity(Platform.AppContext, pendingIntentId, i, pendingIntentFlags);
            }
            return new Notification.Builder(this, channelId)
                    .SetContentTitle(notificationTitle)
               .SetSmallIcon(Resource.Drawable.material_ic_menu_arrow_down_black_24dp)
               .SetProgress(max_progress, progress, false)
               .SetOngoing(true)
               .SetContentIntent(pendingIntent)
               .Build();
        }

    }
}
