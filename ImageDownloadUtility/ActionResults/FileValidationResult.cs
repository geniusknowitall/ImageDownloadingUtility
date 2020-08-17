using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageDownloadUtility.ActionResults
{
    public class FileValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public List<string> FileContents { get; set; }

        public FileValidationResult(bool isValid, string message)
        {
            this.IsValid = isValid;
            this.Message = message;
        }

        public FileValidationResult(bool isValid, string message, List<string> fileContents)
        {
            this.IsValid = isValid;
            this.Message = message;
            this.FileContents = fileContents;
        }
    }
}
