using System;
using System.Threading.Tasks;

namespace Elorucov.Laney.Helpers.Uploader
{
    public delegate void ProgressChangedDelegate(double totalBytes, double bytesSent, double percent, string debugInfo);
    public delegate void UploadFailedDelegate(Exception e);

    public interface IFileUploader
    {
        event ProgressChangedDelegate ProgressChanged;
        event UploadFailedDelegate UploadFailed;

        Task<string> UploadAsync();

        void CancelUpload();
    }
}
