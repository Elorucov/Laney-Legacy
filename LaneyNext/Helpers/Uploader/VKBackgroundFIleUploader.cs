using Elorucov.Laney.Core;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Elorucov.Laney.Helpers.Uploader
{
    public class VKBackgroundFileUploader : IFileUploader
    {
        private CancellationTokenSource cts;
        private BackgroundUploader bu;
        private List<BackgroundTransferContentPart> list;
        private Progress<UploadOperation> progClbck;

        string _type;
        Uri _uploadUri;
        StorageFile _file;

        public event ProgressChangedDelegate ProgressChanged;
        public event UploadFailedDelegate UploadFailed;

        public VKBackgroundFileUploader(string type, Uri uploadUri, StorageFile file)
        {
            _type = type;
            _uploadUri = uploadUri;
            _file = file;
            progClbck = new Progress<UploadOperation>();
            progClbck.ProgressChanged += (x, y) =>
            {
                double a = y.Progress.TotalBytesToSend;
                double b = y.Progress.BytesSent;
                double p = 100 / a * b;
                string c = $"Upload method 1 (BackgroundUploader)\nProgress: {Math.Round(p, 1)}; {b}b of {a}b...";
                Log.General.Verbose(c);
                ProgressChanged?.Invoke(a, b, p, c);
            };
        }

        public async Task<string> UploadAsync()
        {
            try
            {
                string result = null;

                if (_uploadUri == null && _file == null)
                    throw new Exception("One of the important parameters is null");

                cts = new CancellationTokenSource();
                bu = new BackgroundUploader();
                list = new List<BackgroundTransferContentPart>();

                bu.Method = "POST";
                bu.CostPolicy = BackgroundTransferCostPolicy.Always;

                BackgroundTransferContentPart ctprt = new BackgroundTransferContentPart(_type, WebUtility.UrlEncode(_file.Name));
                ctprt.SetHeader("Content-Type", "application/octet-stream");
                ctprt.SetFile(_file);
                list.Add(ctprt);

                string boundary = string.Format("----------------------------" + DateTime.Now.Ticks.ToString("x"), new object[0]);
                UploadOperation upop = await bu.CreateUploadAsync(_uploadUri, list, "form-data", boundary);
                Log.General.Info("Starting upload...", new ValueSet { { "url", _uploadUri.ToString() }, { "boundary", boundary } });
                var op = await upop.StartAsync().AsTask(cts.Token, progClbck);

                var b = op.GetResultStreamAt(0);
                var b1 = await b.ReadAsync(new Windows.Storage.Streams.Buffer((uint)op.Progress.BytesReceived), (uint)op.Progress.BytesReceived, InputStreamOptions.None);
                using (DataReader lo = DataReader.FromBuffer(b1))
                {
                    result = lo.ReadString(lo.UnconsumedBufferLength);
                    lo.Dispose();
                }

                b.Dispose();
                cts.Dispose();
                cts = null;
                list.Clear();
                return result;
            }
            catch (Exception ex)
            {
                UploadFailed?.Invoke(ex);
                return null;
            }
        }

        public void CancelUpload()
        {
            Log.General.Info(String.Empty, new ValueSet { { "url", _uploadUri.ToString() }, { "file", _file.Path } });
            cts.Cancel();
            cts.Dispose();
        }
    }
}
