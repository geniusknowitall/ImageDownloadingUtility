using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageDownloadingUtility.ActionResults
{
    public class DownloadResult
    {
        public bool IsSuccess { get; set; }
        public string FileName { get; set; }
        public DateTime ImageCapturedFor { get; set; }
    }
}