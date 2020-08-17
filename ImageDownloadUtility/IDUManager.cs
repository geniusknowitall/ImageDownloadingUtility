using System;
using ImageDownloadingUtility.Helpers.DownloadHelpers;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ImageDownloadingUtility.Entities;
using ImageDownloadUtility.ActionResults;
using ImageDownloadingUtility.Repositories;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Helpers;
using System.Configuration;
using System.Text;

namespace ImageDownloadUtility
{
    public partial class IDUManager : Form
    {
        private const int MINUMUM_CONFIG_COLUMNS_REQUIRED = 9;
        private const int DEFAULT_SCHEMA_COLUMN_COUNT = 10;
        private const int TICK_INTERVAL_IN_MILISECONDS = 300000; // 5 Minutes
        private readonly TimeSpan fiveMinutes;

        private Timer timer;
        private SqlDbHelper sqlDbHelper;
        private List<ImageDownloader> downloadTasks;
        private StorageRespository storageRespository;

        private Action<ImageDownloader, Camera> onChangeEvent;
        private Action<ImageDownloader, Store> onDownloadingCompleted;

        private bool isApplicationSetToAutomatic;
        private readonly TimeSpan applicationRestartTime;

        public IDUManager()
        {
            this.sqlDbHelper = new SqlDbHelper();
            this.downloadTasks = new List<ImageDownloader>();
            this.storageRespository = new BlobStorageRepository("imagescontainer");
            this.isApplicationSetToAutomatic = false;

            this.onChangeEvent = (ImageDownloader imageDownloader, Camera camera) =>
            {
                this.dataGridView1.Rows[camera.Store.UIIndex].Cells[camera.UIIndex].Value = $"Saved: {camera.SavedImagesCount} | Skipped: {camera.SkippedImagesCount}";
            };
            this.onDownloadingCompleted = (ImageDownloader imageDownloader, Store store) =>
            {
                this.dataGridView1.Rows[store.UIIndex].Cells[1].Value = "Completed";
                this.dataGridView1.Rows[store.UIIndex].Cells[1].Style.ForeColor = Color.Green;
            };

            var setAutomatic = ConfigurationManager.AppSettings["SetAutomatic"];
            var restartTime = ConfigurationManager.AppSettings["RestartTime"];

            if (setAutomatic != null)
            {
                this.isApplicationSetToAutomatic = Convert.ToBoolean(setAutomatic);
            }

            if (restartTime != null)
            {
                this.applicationRestartTime = TimeSpan.Parse(restartTime);
            }

            this.fiveMinutes = new TimeSpan(0, 0, 0, 0, TICK_INTERVAL_IN_MILISECONDS);

            InitializeComponent();
        }

        private void IDUManager_Load(object sender, EventArgs e)
        {
            if (this.isApplicationSetToAutomatic)
            {
                this.Text = "Image Downloading Utility [Automatic]";

                this.btnGo.Enabled = false;
                this.btnBrowse.Enabled = false;
                this.txtFilePath.Enabled = false;

                this.timer = new Timer();
                this.timer.Tick += Timer_Tick;
                this.timer.Interval = TICK_INTERVAL_IN_MILISECONDS;
                this.timer.Start();
                this.Timer_Tick(this.timer, null);
            }
            else
            {
                this.Text = "Image Downloading Utility [Manual]";
            }

            this.BuildDataGridSchema();
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            string csvStoreIds = string.Join(",", this.downloadTasks.Select(x => x.Store.Id));

            StringBuilder queryBuilder = new StringBuilder($"SELECT * FROM Store WHERE OpenTime <= '{DateTime.Now.TimeOfDay}' AND CloseTime > '{DateTime.Now.TimeOfDay}'");

            if (!string.IsNullOrEmpty(csvStoreIds))
            {
                queryBuilder.Append($" AND Id NOT IN ({csvStoreIds})");
            }

            DataTable dt = await this.sqlDbHelper.ExecuteDataTableAsync(queryBuilder.ToString());

            if (dt.Rows.Count > 0)
            {
                List<Store> stores = new List<Store>();

                foreach (DataRow dr in dt.Rows)
                {
                    Store store = new Store(dr);

                    if (store.DownloadingMethod == "RTSPLive")
                    {
                        this.downloadTasks.Add(new RTSPLiveStream(store, this.storageRespository, this.onChangeEvent, this.onChangeEvent, this.onDownloadingCompleted));
                    }

                    stores.Add(store);
                }

                this.PopulateGridView(stores, true);
            }

            if (DateTime.Now.TimeOfDay >= applicationRestartTime && DateTime.Now.TimeOfDay <= applicationRestartTime.Add(this.fiveMinutes))
            {
                this.btnStopAll_Click(null, null);

                //TODO: Create daily report and push it in the blob storage account
                
                this.dataGridView1.Rows.Clear();
                this.downloadTasks.Clear();
            }
        }

        private void btnGo_Click(object sender, EventArgs e)
        {
            var (isValid, message, fileContents) = this.ValidateConfigFile(txtFilePath.Text);

            if (!isValid)
            {
                MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            foreach (string storeConfig in fileContents)
            {
                Store store = new Store(storeConfig);

                if (store.DownloadingMethod == "RTSPLive")
                {
                    this.downloadTasks.Add(new RTSPLiveStream(store, this.storageRespository, this.onChangeEvent, this.onChangeEvent, this.onDownloadingCompleted));
                }
            }

            this.btnGo.Enabled = false;
            this.btnBrowse.Enabled = false;
            this.txtFilePath.Enabled = false;

            this.PopulateGridView(downloadTasks.Select(x => x.Store), false);
        }

        private void btnPlayAll_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow dataGridViewRow in this.dataGridView1.Rows)
            {
                dataGridViewRow.Cells[0].Value = true;
                dataGridViewRow.Cells[1].Style.ForeColor = Color.Blue;
                dataGridViewRow.Cells[1].Value = "Processing";
            }

            _ = Task.WhenAll(downloadTasks.Select(o => o.StartDownloading()));
        }

        private void btnPlaySelected_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < this.dataGridView1.Rows.Count; i++)
            {
                if ((bool)this.dataGridView1.Rows[i].Cells[0].Value)
                {
                    this.dataGridView1.Rows[i].Cells[1].Style.ForeColor = Color.Blue;
                    this.dataGridView1.Rows[i].Cells[1].Value = "Processing";

                    _ = this.downloadTasks[i].StartDownloading();
                }
            }
        }

        private void btnStopAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < this.dataGridView1.Rows.Count; i++)
            {
                this.dataGridView1.Rows[i].Cells[1].Style.ForeColor = Color.Red;
                this.dataGridView1.Rows[i].Cells[1].Value = "Cancelled";

                this.downloadTasks[i].StopDownloading();
            }
        }

        private void btnStopSelected_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < this.dataGridView1.Rows.Count; i++)
            {
                if ((bool)this.dataGridView1.Rows[i].Cells[0].Value)
                {
                    this.dataGridView1.Rows[i].Cells[1].Style.ForeColor = Color.Red;
                    this.dataGridView1.Rows[i].Cells[1].Value = "Cancelled";

                    this.downloadTasks[i].StopDownloading();
                }
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.RestoreDirectory = true;
            openFileDialog.Title = "Browse Config File [.csv]";
            openFileDialog.DefaultExt = ".csv";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                txtFilePath.Text = openFileDialog.FileName;
            }
        }

        private (bool, string, List<string>) ValidateConfigFile(string fileName)
        {
            string errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(txtFilePath.Text))
            {
                return (false, "Please select a config [.csv] file first", null);
            }

            if (!File.Exists(txtFilePath.Text))
            {
                return (false, "Invalid file path is selected", null);
            }

            if (Path.GetExtension(txtFilePath.Text).ToLower() != ".csv")
            {
                return (false, "The utility only supports .csv file for configuration", null);
            }

            List<string> fileContents = File.ReadAllLines(txtFilePath.Text).ToList();

            if (fileContents == null || fileContents.Count == 0)
            {
                return (false, "The selected config file is empty. Please select a valid config file and then try again.", null);
            }

            string invalidConfig = fileContents.FirstOrDefault(x => x.Split(',').Length < MINUMUM_CONFIG_COLUMNS_REQUIRED);

            if (!string.IsNullOrEmpty(invalidConfig))
            {
                return (false, $"The config file contains invalid configuration at line # {fileContents.IndexOf(invalidConfig) + 1}", null);
            }

            return (true, "Success", fileContents);
        }

        private void BuildDataGridSchema()
        {
            this.dataGridView1.Columns.Add(new DataGridViewCheckBoxColumn(false));
            this.dataGridView1.Columns.Add("Status", "Status");
            this.dataGridView1.Columns.Add("ClientId", "Client Id");
            this.dataGridView1.Columns.Add("StoreId", "Store Id");
            this.dataGridView1.Columns.Add("DownloadingMethod", "Method");
            this.dataGridView1.Columns.Add("BusinessDate", "Business Date");
            this.dataGridView1.Columns.Add("OpenTime", "Open Time");
            this.dataGridView1.Columns.Add("CloseTime", "Close Time");
            this.dataGridView1.Columns.Add("TotalImages", "Total Images");
            this.dataGridView1.Columns.Add("DownloadUrl", "Download Url");
        }

        private void PopulateGridView(IEnumerable<Store> stores, bool isChecked)
        {
            int currentCameraColumnsCount = this.dataGridView1.Columns.Count - DEFAULT_SCHEMA_COLUMN_COUNT;
            int newColumnsToCreateCount = stores.Select(x => x.Cameras.Count).Max() - currentCameraColumnsCount;

            for (int i = currentCameraColumnsCount; i < currentCameraColumnsCount + newColumnsToCreateCount; i++)
            {
                this.dataGridView1.Columns.Add($"Camera{i + 1}", $"Camera {i + 1}");
            }

            foreach (Store store in stores)
            {
                List<object> rowValues = new List<object>() 
                { 
                    isChecked, 
                    string.Empty, 
                    store.ClientId, 
                    store.Id, 
                    store.DownloadingMethod,
                    "RTSPLive".Equals(store.DownloadingMethod) ? $"[TODAY] {DateTime.Now.ToShortDateString()}" : store.BusinessDate.ToShortDateString(),
                    store.OpenTime.ToString(), 
                    store.CloseTime.ToString(), 
                    store.TotalImages, 
                    store.BaseUrl 
                };

                foreach (Camera camera in store.Cameras)
                {
                    rowValues.Add("Saved: 0 | Skipped: 0");

                    camera.UIIndex = rowValues.Count - 1;
                }

                store.UIIndex = dataGridView1.Rows.Add(rowValues.ToArray());
            }

            if (isChecked)
            {
                this.btnPlaySelected_Click(this.btnPlaySelected, null);
            }
        }
    }
}