using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageDownloadingUtility.DownloadHelper
{
    public class Camera
    {
        public string Name { get; private set; }
        public int SavedCount { get; set; }
        public Store Store { get; set; }
        public DateTime CurrentInstance { get; set; }
        public List<DateTime> SkippedImages { get; set; }
        public int Index { get; set; }

        public int TotalCount
        {
            get
            {
                if (Store == null)
                {
                    throw new Exception("Store has to be assigned first before accessing this property");
                }

                return (int)(this.Store.CloseTime - this.Store.StartTime).TotalMinutes / this.Store.IntervalInMinutes;
            }
        }

        public int SkippedCount
        {
            get
            {
                return this.SkippedImages.Count;
            }
        }

        public string DownloadingUrl
        {
            get
            {
                if (Store == null)
                {
                    throw new Exception("Store has to be assigned first before accessing this property");
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
            this.SkippedImages = new List<DateTime>();
        }
    }
}
