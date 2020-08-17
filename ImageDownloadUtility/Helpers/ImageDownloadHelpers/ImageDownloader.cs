using ImageDownloadingUtility.ActionResults;
using ImageDownloadingUtility.Entities;
using ImageDownloadingUtility.Repositories;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ImageDownloadingUtility.Helpers.DownloadHelpers
{
    public abstract class ImageDownloader
    {
        public Store Store { get; set; }

        private bool CancellationRequested { get; set; }
        private StorageRespository StorageRepository { get; set; }
        private int AbortTimeInMinutes { get; }
        private bool ShouldWaitBeforeNextCapture { get; }

        public Action<ImageDownloader, Camera> OnImageCaptured { get; set; }
        public Action<ImageDownloader, Camera> OnImageSkipped { get; set; }
        public Action<ImageDownloader, Store> OnDownloadingCompleted { get; set; }

        public ImageDownloader(Store store, StorageRespository storageRespository, int abortTimeInMinutes, bool shouldWaitBeforeNextCapture,
            Action<ImageDownloader, Camera> onImageCaptured, Action<ImageDownloader, Camera> onImageSkipped, Action<ImageDownloader, Store> onDownloadingCompleted)
        {
            if (storageRespository == null)
            {
                throw new ArgumentNullException(nameof(storageRespository));
            }

            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            if (store.Cameras == null || store.Cameras.Count() == 0)
            {
                throw new ArgumentNullException(nameof(store.Cameras));
            }

            this.Store = store;
            this.CancellationRequested = false;
            this.AbortTimeInMinutes = abortTimeInMinutes;
            this.ShouldWaitBeforeNextCapture = shouldWaitBeforeNextCapture;
            this.StorageRepository = storageRespository;
            this.OnImageCaptured = onImageCaptured;
            this.OnImageSkipped = onImageSkipped;
            this.OnDownloadingCompleted = onDownloadingCompleted;
        }

        public async Task StartDownloading()
        {
            this.CreateDirectoriesIfNotExists();

            int abortTimeInMilliseconds = this.AbortTimeInMinutes * 60 * 1000;

            while (!this.IsDownloadingCompleted && !this.CancellationRequested)
            {
                foreach (Camera camera in this.Store.Cameras)
                {
                    CancellationTokenSource tokenSource = new CancellationTokenSource(abortTimeInMilliseconds - 20000);

                    _ = Task.Run(() => DownloadAndSave(camera, tokenSource.Token), tokenSource.Token);
                }

                if (this.ShouldWaitBeforeNextCapture)
                {
                    await Task.Delay(abortTimeInMilliseconds);
                }
            }

            if (this.IsDownloadingCompleted)
            {
                this.OnDownloadingCompleted?.Invoke(this, this.Store);
            }
        }

        public void StopDownloading()
        {
            this.CancellationRequested = true;
        }

        private async Task DownloadAndSave(Camera camera, CancellationToken cancellationToken)
        {
            DownloadResult result = await this.DownloadImage(camera, cancellationToken);

            if (result.IsSuccess)
            {
                string blobDirectory = $"{result.ImageCapturedFor.ToString("yyyy-MM-dd")}/{camera.Store.ClientId}/{camera.Store.Id}/{camera.Name}";

                await this.StorageRepository.UploadFileAsync(result.FileName, blobDirectory, result.ImageCapturedFor.ToString("yyyyMMddTHHmm00Z") + ".jpg");

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
                string directoryName = $"{camera.Store.SavePath}/{camera.Name}";

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