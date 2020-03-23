using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageDownloadingUtility.Storage
{
    public class BlobStorage : StorageRespository
    {
        private CloudBlobContainer cloudBlobContainer { get; set; }
        private string storageConnectionString
        {
            get
            {
                return @"DefaultEndpointsProtocol=https;AccountName=cmsimagerepository;AccountKey=VsCFi5RhGSh0ImwBlKdtkfYk6Qh1JZbNTi2kYdWhdfQgRt5lYTLok8I2K4Bua1o0QV+L3DP6Cw64V2kjh1oVIw==;EndpointSuffix=core.windows.net";
            }
        }

        public BlobStorage(string containerName)
        {
            CloudBlobClient cloudBlobClient = CloudStorageAccount.Parse(storageConnectionString).CreateCloudBlobClient();

            this.cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);
        }

        public async Task<bool> CreateContainerAsync()
        {
            return await this.cloudBlobContainer.CreateIfNotExistsAsync();
        }

        public override async Task UploadFileAsync(string filePath, string blobDirectory, string blobName = null)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new Exception("filePath can't be empty. Please specify path to your file in this parameter");
            }

            if (string.IsNullOrWhiteSpace(blobDirectory))
            {
                throw new Exception("blobDirectory can't be empty. Please specify directory path of your blob in this parameter");
            }

            if (string.IsNullOrWhiteSpace(blobName))
            {
                string[] splits = filePath.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

                blobName = splits[splits.Length - 1];
            }

            CloudBlobDirectory directoryReference = this.cloudBlobContainer.GetDirectoryReference(blobDirectory);
            CloudBlockBlob cloudBlockBlob = directoryReference.GetBlockBlobReference(blobName);

            using (var fileStream = File.OpenRead(filePath))
            {
                await cloudBlockBlob.UploadFromStreamAsync(fileStream);
            }
        }

        public async Task DownloadFileAsync(string blobPath, string savePath)
        {
            if (string.IsNullOrWhiteSpace(blobPath))
            {
                throw new Exception("blobPath can't be empty. Please specify path to your blob in this parameter");
            }

            if (string.IsNullOrWhiteSpace(savePath))
            {
                throw new Exception("savePath can't be empty. Please specify the path where you want to save your blob in this parameter");
            }

            string blobDirectory = blobPath.Substring(0, blobPath.LastIndexOf('/') + 1);
            string blobName = blobPath.Substring(blobPath.LastIndexOf('/') + 1);

            CloudBlobDirectory directoryReference = cloudBlobContainer.GetDirectoryReference(blobDirectory);
            CloudBlockBlob cloudBlockBlob = directoryReference.GetBlockBlobReference(blobName);

            using (var fileStream = File.OpenWrite(savePath))
            {
                await cloudBlockBlob.DownloadToStreamAsync(fileStream);
            }
        }

        public IEnumerable<IListBlobItem> ListBlobs(string blobDirectory)
        {
            if (string.IsNullOrWhiteSpace(blobDirectory))
            {
                throw new Exception("blobDirectory can't be empty. Please specify directory path to your blob in this parameter");
            }

            return cloudBlobContainer.GetDirectoryReference(blobDirectory).ListBlobs();
        }
    }
}
