using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageDownloadingUtility.Repositories
{
    public class BlobStorageEmulatedRepository : StorageRespository
    {
        public override async Task UploadFileAsync(string filePath, string blobDirectory, string blobName = null)
        {
            await Task.FromResult(true);
        }
    }
}
