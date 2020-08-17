using ImageDownloadingUtility.ActionResults;
using ImageDownloadingUtility.Entities;
using ImageDownloadingUtility.Repositories;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageDownloadingUtility.Helpers.DownloadHelpers
{
    public class RTSPLiveStream : ImageDownloader
    {
        private const bool SHOULD_WAIT_BEFORE_NEXT_CAPTURE = true;
        protected override bool IsDownloadingCompleted
        {
            get
            {
                return TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Local, this.Store.TimeZone).TimeOfDay >= this.Store.CloseTime;
            }
        }

        public RTSPLiveStream(Store store, StorageRespository storageRepository, Action<ImageDownloader, Camera> onImageCaptured, Action<ImageDownloader, Camera> onImageSkipped, Action<ImageDownloader, Store> onDownloadingCompleted) 
            : base(store, storageRepository, store.IntervalInMinutes, SHOULD_WAIT_BEFORE_NEXT_CAPTURE, onImageCaptured, onImageSkipped, onDownloadingCompleted)
        {
        }

        protected override async Task<DownloadResult> DownloadImage(Camera camera, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            DateTime currentTimeInTimezone = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Local, this.Store.TimeZone);
            string fileName = $"{camera.Store.SavePath}/{camera.Name}/{currentTimeInTimezone.ToString("yyyyMMddTHHmm00Z")}.jpg";

            Process process = new Process();
            process.StartInfo.FileName = @"C:\Users\IT Desktop\Downloads\ffmpeg\bin\ffmpeg.exe";
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
                camera.SavedImagesCount++;
            }
            else
            {
                camera.SkippedImagesCount++;
            }

            return new DownloadResult()
            {
                FileName = fileName,
                IsSuccess = isFileDownloaded,
                ImageCapturedFor = currentTimeInTimezone
            };
        }
    }
}