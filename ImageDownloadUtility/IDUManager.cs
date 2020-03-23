using ImageDownloadingUtility.DownloadHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageDownloadUtility
{
    public partial class IDUManager : Form
    {
        private const int MINUMUM_CONFIG_COLUMNS_REQUIRED = 9;
        private List<ImageDownloadHelper> DownloadTasks;
        private List<string> fileContents = new List<string>()
            {
                "1,1,RTSPLive,03/22/2020,08:00 AM,11:59 PM,rtsp://96.77.128.118:5445/Live/Channel=,5,Pakistan Standard Time,3,0,1,2,3",
                "1,2,RTSPLive,03/22/2020,08:00 AM,08:00 PM,rtsp://75.148.187.50:5445/Live/Channel=,5,Pakistan Standard Time,3,0,1,2,3",
                "1,3,RTSPLive,03/22/2020,08:00 AM,11:59 PM,rtsp://73.77.148.187:5445/Live/Channel=,5,Pakistan Standard Time,3,0,1,2,3"
            };

        public IDUManager()
        {
            this.DownloadTasks = new List<ImageDownloadHelper>();

            InitializeComponent();
        }

        private void btnGo_Click(object sender, EventArgs e)
        {
            //FileValidationResult result = ValidateConfigFile(txtFilePath.Text);

            //if (!result.IsValid)
            {
                //MessageBox.Show(result.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //return;
            }

            //foreach (string storeConfig in result.FileContents)
            foreach (string storeConfig in fileContents)
            {
                Store store = new Store(storeConfig);
                ImageDownloadHelper downloadTask = null;

                if (store.DownloadingMethod == "RTSPLive")
                {
                    downloadTask = new RTSPLiveStream(store);
                    downloadTask.OnImageCaptured = OnImageCaptured;
                    downloadTask.OnImageSkipped = OnImageSkipped;
                    downloadTask.OnDownloadingCompleted = OnDownloadCompleted;
                }

                DownloadTasks.Add(downloadTask);
            }

            this.PopulateGridView(DownloadTasks.Select(x => x.Store));
        }

        public void OnDownloadCompleted(ImageDownloadHelper sender, Store store)
        {
            dataGridView1.Rows[store.Index].Cells[1].Value = "Completed";
            dataGridView1.Rows[store.Index].Cells[1].Style.ForeColor = Color.Green;
        }

        public void OnImageCaptured(ImageDownloadHelper sender, Camera camera)
        {
            dataGridView1.Rows[camera.Store.Index].Cells[camera.Index].Value = $"Saved: { camera.SavedCount } | Skipped: { camera.SkippedCount }";
        }

        public void OnImageSkipped(ImageDownloadHelper sender, Camera camera)
        {
            dataGridView1.Rows[camera.Store.Index].Cells[camera.Index].Value = $"Saved: { camera.SavedCount } | Skipped: { camera.SkippedCount }";
        }

        private FileValidationResult ValidateConfigFile(string fileName)
        {
            string errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(txtFilePath.Text))
            {
                return new FileValidationResult(false, "Please select a config [.csv] file first");
            }

            if (!File.Exists(txtFilePath.Text))
            {
                return new FileValidationResult(false, "Invalid file path is selected");
            }

            if (Path.GetExtension(txtFilePath.Text).ToLower() != ".csv")
            {
                return new FileValidationResult(false, "The utility could only supports .csv file for configuration");
            }

            List<string> fileContents = File.ReadAllLines(txtFilePath.Text).ToList();

            if (fileContents == null || fileContents.Count == 0)
            {
                return new FileValidationResult(false, "The selected config file is empty. Please select a valid config file and then try again.");
            }

            string invalidConfig = fileContents.FirstOrDefault(x => x.Split(',').Length < MINUMUM_CONFIG_COLUMNS_REQUIRED);

            if (!string.IsNullOrEmpty(invalidConfig))
            {
                return new FileValidationResult(false, string.Format("The config file contains invalid configuration at line # {0}", fileContents.IndexOf(invalidConfig) + 1));
            }

            return new FileValidationResult(true, "Success", fileContents);
        }

        private void PopulateGridView(IEnumerable<Store> stores)
        {
            dataGridView1.Columns.Add(new DataGridViewCheckBoxColumn(false));
            dataGridView1.Columns.Add("Status", "Status");
            dataGridView1.Columns.Add("ClientId", "Client Id");
            dataGridView1.Columns.Add("StoreId", "Store Id");
            dataGridView1.Columns.Add("DownloadingMethod", "Method");
            dataGridView1.Columns.Add("BusinessDate", "Business Date");
            dataGridView1.Columns.Add("StartTime", "Start Time");
            dataGridView1.Columns.Add("EndTime", "End Time");
            dataGridView1.Columns.Add("TotalImages", "Total Images");
            dataGridView1.Columns.Add("DownloadUrl", "Download Url");

            int maxNoOfCameras = DownloadTasks.Select(x => x.Store.Cameras.Count).Max();

            for (int i = 0; i < maxNoOfCameras; i++)
            {
                dataGridView1.Columns.Add("Camera1", string.Format("Camera {0}", i + 1));
            }

            foreach (Store store in stores)
            {
                List<object> rowValues = new List<object>() { false, string.Empty, store.ClientId, store.Id, store.DownloadingMethod, store.BusinessDate.ToShortDateString(), store.StartTime.ToShortTimeString(), store.CloseTime.ToShortTimeString(), store.TotalImages, store.BaseUrl };

                foreach (Camera camera in store.Cameras)
                {
                    rowValues.Add("Saved: 0 | Skipped: 0");

                    camera.Index = rowValues.Count - 1;
                }

                store.Index = dataGridView1.Rows.Add(rowValues.ToArray());
            }
        }

        private void btnPlayAll_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow dataGridViewRow in dataGridView1.Rows)
            {
                dataGridViewRow.Cells[0].Value = true;
                dataGridViewRow.Cells[1].Style.ForeColor = Color.Blue;
                dataGridViewRow.Cells[1].Value = "Processing";
            }

            _ = Task.WhenAll(DownloadTasks.Select(o => o.StartDownloading()));
        }

        private void btnPlaySelected_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                if ((bool)dataGridView1.Rows[i].Cells[0].Value)
                {
                    _ = DownloadTasks[i].StartDownloading();

                    dataGridView1.Rows[i].Cells[1].Style.ForeColor = Color.Blue;
                    dataGridView1.Rows[i].Cells[1].Value = "Processing";
                }
            }
        }

        private void btnStopAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                DownloadTasks[i].StopDownloading();

                dataGridView1.Rows[i].Cells[1].Style.ForeColor = Color.Red;
                dataGridView1.Rows[i].Cells[1].Value = "Cancelled";
            }
        }

        private void btnStopSelected_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                if ((bool)dataGridView1.Rows[i].Cells[0].Value)
                {
                    DownloadTasks[i].StopDownloading();

                    dataGridView1.Rows[i].Cells[1].Style.ForeColor = Color.Red;
                    dataGridView1.Rows[i].Cells[1].Value = "Cancelled";
                }
            }
        }
    }

    public class FileValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public List<string> FileContents { get; set; }

        public FileValidationResult(bool isValid, string message)
        {
            this.IsValid = IsValid;
            this.Message = Message;
        }

        public FileValidationResult(bool isValid, string message, List<string> fileContents)
        {
            this.IsValid = IsValid;
            this.Message = Message;
            this.FileContents = FileContents;
        }
    }
}
