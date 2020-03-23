using ImageDownloadingUtility.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ImageDownloadingUtility.DownloadHelper
{
    public delegate void DownloadEventDelegate(ImageDownloadHelper sender, Camera camera);
    public delegate void DownloadCompleteEventDelegate(ImageDownloadHelper sender, Store store);

    public abstract class ImageDownloadHelper
    {
        public Store Store { get; set; }
        private CancellationTokenSource CancellationTokenSource { get; set; }
        private StorageRespository CloudStorage { get; set; }
        private int AbortTimeInMilliSeconds { get; }
        private bool ShouldWaitBeforeNextCapture { get; }

        public DownloadEventDelegate OnImageCaptured { get; set; }
        public DownloadEventDelegate OnImageSkipped { get; set; }
        public DownloadCompleteEventDelegate OnDownloadingCompleted { get; set; }

        public ImageDownloadHelper(Store store, int abortTimeInMilliseconds, bool shouldWaitBeforeNextCapture
            , StorageRespository storageRespository, CancellationTokenSource cancellationTokenSource)
        {
            if (storageRespository == null)
            {
                throw new ArgumentNullException("StorageRepository can't be null");
            }

            if (store == null)
            {
                throw new ArgumentNullException("Store can't be null or empty");
            }

            if (store.Cameras == null || store.Cameras.Count() == 0)
            {
                throw new Exception("There should alteast be 1 camera assigned to start with");
            }

            this.Store = store;
            this.AbortTimeInMilliSeconds = abortTimeInMilliseconds;
            this.ShouldWaitBeforeNextCapture = shouldWaitBeforeNextCapture;
            this.CloudStorage = storageRespository;
            this.CancellationTokenSource = cancellationTokenSource;
        }

        public async Task StartDownloading()
        {
            this.CreateDirectoriesIfNotExists();

            while (!this.IsDownloadingCompleted && !this.CancellationTokenSource.Token.IsCancellationRequested)
            {
                foreach (Camera camera in this.Store.Cameras)
                {
                    CancellationTokenSource tokenSource = new CancellationTokenSource(AbortTimeInMilliSeconds - 20000);

                    _ = Task.Run(() => DownloadAndSave(camera, tokenSource.Token), tokenSource.Token);
                }

                if (this.ShouldWaitBeforeNextCapture)
                {
                    await Task.Delay(AbortTimeInMilliSeconds);
                }
            }

            if (this.IsDownloadingCompleted)
            {
                this.OnDownloadingCompleted?.Invoke(this, this.Store);
            }
        }

        public void StopDownloading()
        {
            this.CancellationTokenSource.Cancel();
        }

        private async Task DownloadAndSave(Camera camera, CancellationToken cancellationToken)
        {
            DownloadResult result = await this.DownloadImage(camera, cancellationToken);

            if (result.IsSuccess)
            {
                string blobDirectory = string.Format("{0}/{1}/{2}/{3}", result.CapturedInstance.ToString("yyyy-MM-dd"), camera.Store.ClientId, camera.Store.Id, camera.Name);

                await this.CloudStorage.UploadFileAsync(result.FileName, blobDirectory, result.CapturedInstance.ToString("yyyyMMddThhmmssZ") + ".jpg");

                this.OnImageCaptured?.Invoke(this, camera);
            }
            else
            {
                this.OnImageSkipped?.Invoke(this, camera);
            }
        }

        private void CreateDirectoriesIfNotExists()
        {
            foreach (Camera camera in this.Store.Cameras)
            {
                string directoryName = string.Concat(camera.Store.SavingPath, "/", camera.Name);

                if (!Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }
            }
        }

        protected abstract bool IsDownloadingCompleted { get; }
        protected abstract Task<DownloadResult> DownloadImage(Camera camera, CancellationToken cancellationToken);
    }
}