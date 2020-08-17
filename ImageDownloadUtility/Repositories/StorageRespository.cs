using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageDownloadingUtility.Repositories
{
    public abstract class StorageRespository
    {
        public abstract Task UploadFileAsync(string filePath, string blobDirectory, string blobName = null);
    }
}
