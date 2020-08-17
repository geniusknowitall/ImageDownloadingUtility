using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageDownloadingUtility.Entities
{
    public class Store
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ClientId { get; set; }
        public TimeSpan OpenTime { get; private set; }
        public TimeSpan CloseTime { get; private set; }
        public DateTime BusinessDate { get; set; }
        public int MaxRetryCount { get; private set; }
        public int IntervalInMinutes { get; private set; }
        public string BaseUrl { get; private set; }
        public string DownloadingMethod { get; private set; }
        public string TimeZoneCode { get; set; }
        public TimeZoneInfo TimeZone
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.TimeZoneCode))
                {
                    throw new ArgumentNullException(nameof(this.TimeZoneCode));
                }

                return TimeZoneInfo.FindSystemTimeZoneById(this.TimeZoneCode);
            }
        }
        public int TotalImages
        {
            get
            {
                return (int)((this.CloseTime - this.OpenTime).TotalMinutes / this.IntervalInMinutes);
            }
        }

        public string SavePath
        {
            get
            {
                return $"{this.ClientId}/{this.Id}";
            }
        }

        public List<Camera> Cameras { get; private set; }
        public int UIIndex { get; set; }

        public Store(string storeConfig)
        {
            if (string.IsNullOrWhiteSpace(storeConfig))
            {
                throw new ArgumentNullException(nameof(storeConfig));
            }

            string[] splits = storeConfig.Split(',');

            if (splits.Length < 11)
            {
                throw new Exception("Invalid Store config, a config file must have a minimum of 11 columns");
            }

            this.ClientId = int.Parse(splits[0]);
            this.Id = int.Parse(splits[1]);
            this.DownloadingMethod = splits[2];
            this.BusinessDate = this.DownloadingMethod != "RTSPLive" ? DateTime.Parse(splits[3]) : DateTime.Now;
            this.OpenTime = TimeSpan.Parse(splits[4]);
            this.CloseTime = TimeSpan.Parse(splits[5]);
            this.BaseUrl = splits[6];
            this.IntervalInMinutes = int.Parse(splits[7]);
            this.TimeZoneCode = splits[8];
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

        public Store(DataRow dr)
        {
            if (dr == null)
            {
                throw new ArgumentNullException(nameof(dr));
            }

            if (dr["OpenTime"] == null || dr["CloseTime"] == null)
            {
                throw new ArgumentNullException($"CloseTime or OpenTime is not set for store {dr["Name"]}");
            }

            if (dr["CameraInfo"] == null)
            {
                throw new ArgumentNullException($"Cameras are not set for store {dr["Name"]}");
            }

            this.Id = (int)dr["Id"];
            this.Name = (string)dr["Name"];
            this.ClientId = (int)dr["ClientId"];
            this.BaseUrl = (string)dr["BaseUrl"];
            this.TimeZoneCode = (string)dr["TimeZoneCode"];
            this.MaxRetryCount = (int)dr["MaxRetryCount"];
            this.OpenTime = (TimeSpan)dr["OpenTime"];
            this.CloseTime = (TimeSpan)dr["CloseTime"];
            this.DownloadingMethod = (string)dr["DownloadingMethod"];
            this.IntervalInMinutes = (int)dr["Interval"];
            this.Cameras = new List<Camera>();

            foreach (string cameraName in ((string)dr["CameraInfo"]).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                this.Cameras.Add(new Camera(cameraName)
                {
                    Store = this
                });
            }
        }

        public Store(int id, string name, int clientId, string baseUrl, string timeZoneCode, int maxRetry, int intervalInMinutes, TimeSpan openTime, TimeSpan closeTime, List<Camera> cameras)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new ArgumentNullException(nameof(baseUrl));
            }

            if (openTime == null || CloseTime == null)
            {
                throw new ArgumentNullException($"{nameof(CloseTime)} or {closeTime}");
            }

            if (cameras == null || cameras.Count() == 0)
            {
                throw new ArgumentNullException(nameof(cameras));
            }

            this.Id = id;
            this.Name = name;
            this.ClientId = clientId;
            this.BaseUrl = baseUrl;
            this.TimeZoneCode = timeZoneCode;
            this.MaxRetryCount = maxRetry;
            this.OpenTime = openTime;
            this.CloseTime = closeTime;
            this.Cameras = cameras;
            this.IntervalInMinutes = intervalInMinutes;
            this.Cameras.ForEach(o => o.Store = this);
        }
    }
}
