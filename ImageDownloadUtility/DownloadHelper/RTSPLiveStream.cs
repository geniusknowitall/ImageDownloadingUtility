using ImageDownloadingUtility.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageDownloadingUtility.DownloadHelper
{
    public class RTSPLiveStream : ImageDownloadHelper
    {
        private const bool SHOULD_WAIT_BEFORE_NEXT_CAPTURE = true;
        protected override bool IsDownloadingCompleted
        {
            get
            {
                return TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Local, this.Store.TimeZone).TimeOfDay >= this.Store.CloseTime.TimeOfDay;
            }
        }

        public RTSPLiveStream(Store store) 
            : base(store, store.IntervalInMilliSeconds, SHOULD_WAIT_BEFORE_NEXT_CAPTURE, new BlobStorage("imagescontainer"), new CancellationTokenSource())
        {
        }

        protected override async Task<DownloadResult> DownloadImage(Camera camera, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            DateTime currentTimeInTimezone = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Local, this.Store.TimeZone);
            string fileName = string.Concat(camera.Store.SavingPath, "/", camera.Name, "/", currentTimeInTimezone.ToString("yyyyMMddThhmmssZ"), ".jpg");

            Process process = new Process();
            process.StartInfo.FileName = @"C:\Users\User\Downloads\ffmpeg\bin\ffmpeg.exe";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.Arguments = $"-y -rtsp_transport tcp -i { camera.DownloadingUrl } -vframes 1 \"{ fileName }\"";

            int retryCount = 0;
            bool isFileDownloaded = false;

            while (retryCount < camera.Store.MaxRetryCount && !isFileDownloaded && !cancellationToken.IsCancellationRequested)
            {
                process.Start();
                string stderrx = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();

                isFileDownloaded = File.Exists(fileName);
                retryCount++;
            }

            if (isFileDownloaded)
            {
                camera.SavedCount++;
            }
            else
            {
                camera.SkippedImages.Add(currentTimeInTimezone);
            }

            return new DownloadResult()
            {
                FileName = fileName,
                IsSuccess = isFileDownloaded,
                CapturedInstance = currentTimeInTimezone
            };
        }
    }
}