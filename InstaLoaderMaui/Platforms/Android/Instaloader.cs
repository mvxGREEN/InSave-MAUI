using Android.App;
using Android.Content;
using Android.OS;
using Firebase.Analytics;
using InstaLoaderMaui;

public static class Instaloader
{
    private static readonly string Tag = nameof(Instaloader);

    public static bool MIsShared = false;
    public static async Task DownloadFile(string url, int index)
    {
        Console.WriteLine($"{Tag} DownloadFile url={url} index={index}");

        // log download event
        Bundle bundle = new Bundle();
        bundle.PutString("app_name", "instaloader");
        bundle.PutString("event_name", "download_file");
        //bundle.PutString("filename", );
        FirebaseAnalytics.GetInstance((MainActivity)Platform.CurrentActivity).LogEvent("input_load", bundle);

        // init download manager
        DownloadManager downloadManager = (DownloadManager)
                MainActivity.ActivityCurrent.GetSystemService(Context.DownloadService);
        Android.Net.Uri fileUri = Android.Net.Uri.Parse(url);

        // set destination
        string fileDir = Android.OS.Environment.DirectoryDocuments;
        string fileExt = ".jpg";
        if (url.Contains(".mp4"))
            fileExt = ".mp4";
        string fileName = ((MainPage)Shell.Current.CurrentPage).MTitle + "_" + index + fileExt;
        Console.WriteLine($"{Tag} fileName={fileName}");

        // start download
        DownloadManager.Request request = new DownloadManager.Request(fileUri);
        request.SetTitle(fileName);
        request.SetDescription("");
        request.SetNotificationVisibility(DownloadVisibility.VisibleNotifyCompleted);
        request.SetDestinationInExternalPublicDir(fileDir, fileName);
        downloadManager.Enqueue(request);
    }

}
