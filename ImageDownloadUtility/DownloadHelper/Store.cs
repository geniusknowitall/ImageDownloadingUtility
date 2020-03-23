using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageDownloadingUtility.DownloadHelper
{
    public class Store
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string TimeZoneCode { get; set; }
        public DateTime BusinessDate { get; set; }
        public string Name { get; set; }
        public string BaseUrl { get; private set; }
        public int MaxRetryCount { get; private set; }
        public int IntervalInMinutes { get; private set; }
        public int IntervalInMilliSeconds { get; set; }
        public TimeZoneInfo TimeZone { get; private set; }
        public DateTime StartTime { get; private set; }
        public DateTime CloseTime { get; private set; }
        public List<Camera> Cameras { get; private set; }
        public string DownloadingMethod { get; private set; }
        public int Index { get; set; }

        public int TotalImages
        {
            get
            {
                return (int) ((CloseTime - StartTime).TotalMinutes / IntervalInMinutes);
            }
        }

        public string SavingPath
        {
            get
            {
                return string.Format("{0}/{1}", this.ClientId, this.Id);
            }
        }

        public Store (string storeConfig)
        {
            if (string.IsNullOrWhiteSpace(storeConfig))
            {
                throw new Exception("storeConfig can't be null or empty");
            }

            string[] splits = storeConfig.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (splits.Length < 8)
            {
                throw new Exception("Invalid Store config");
            }

            this.ClientId = int.Parse(splits[0]);
            this.Id = int.Parse(splits[1]);
            this.DownloadingMethod = splits[2];
            this.BusinessDate = DateTime.Parse(splits[3]);
            this.StartTime = DateTime.Parse(splits[4]);
            this.CloseTime = DateTime.Parse(splits[5]);
            this.BaseUrl = splits[6];
            this.IntervalInMinutes = int.Parse(splits[7]);
            this.IntervalInMilliSeconds = this.IntervalInMinutes * 60 * 1000;
            this.TimeZoneCode = splits[8];
            this.TimeZone = TimeZoneInfo.FindSystemTimeZoneById(this.TimeZoneCode);
            this.MaxRetryCount = int.Parse(splits[9]);

            this.Cameras = new List<Camera>();

            for (int i = 10; i < splits.Length; i++)
            {
                this.Cameras.Add(new Camera(splits[i])
                {
                    Store = this
                });
            }
        }

        public Store(int id, string name, int clientId, string baseUrl, string timeZoneCode, int maxRetry, int intervalInMinutes, DateTime startTime, DateTime closeTime, List<Camera> cameras)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new Exception("Base Url can't be null or empty");
            }

            if (cameras == null || cameras.Count() == 0)
            {
                throw new Exception("There atleast have to be one camera to start with");
            }

            if (startTime == null || CloseTime == null)
            {
                throw new Exception("Start time and close time can't be null");
            }

            this.Id = id;
            this.Name = name;
            this.ClientId = clientId;
            this.BaseUrl = baseUrl;
            this.TimeZoneCode = timeZoneCode;
            this.MaxRetryCount = maxRetry;
            this.StartTime = startTime;
            this.CloseTime = closeTime;
            this.Cameras = cameras;
            this.IntervalInMinutes = intervalInMinutes;
            this.IntervalInMilliSeconds = intervalInMinutes * 60 * 1000;
            this.TimeZone = TimeZoneInfo.FindSystemTimeZoneById(this.TimeZoneCode);
            this.Cameras.ForEach(o => o.Store = this);
        }
    }
}
