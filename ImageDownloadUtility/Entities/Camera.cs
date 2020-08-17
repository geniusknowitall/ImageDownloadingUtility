using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageDownloadingUtility.Entities
{
    public class Camera
    {
        public string Name { get; private set; }
        public Store Store { get; set; }
        public int SkippedImagesCount { get; set; }
        public int SavedImagesCount { get; set; }
        public string DownloadingUrl
        {
            get
            {
                if (this.Store == null)
                {
                    throw new ArgumentNullException(nameof(Store));
                }

                return string.Concat(this.Store.BaseUrl, this.Name);
            }
        }

        public Camera(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new Exception("Camera name can't be null or empty");
            }

            this.Name = name;
        }

        public int UIIndex { get; set; }
    }
}