using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace Elorucov.Laney.Services {
    public delegate void ProgressChangedDelegate(double totalBytes, double bytesSent, double percent, string debugInfo);
    public delegate void UploadFailedDelegate(Exception e);

    public interface IFileUploader {
        event ProgressChangedDelegate ProgressChanged;
        event UploadFailedDelegate UploadFailed;

        Task<string> UploadAsync();

        void CancelUpload();
    }

    public class VKFileUploader : IFileUploader {
        private CancellationTokenSource cts;
        private BackgroundUploader bu;
        private List<BackgroundTransferContentPart> list;
        private Progress<UploadOperation> progClbck;

        string _type;
        Uri _uploadUri;
        StorageFile _file;

        public event ProgressChangedDelegate ProgressChanged;
        public event UploadFailedDelegate UploadFailed;

        public VKFileUploader(string type, Uri uploadUri, StorageFile file) {
            _type = type;
            _uploadUri = uploadUri;
            _file = file;
            progClbck = new Progress<UploadOperation>();
            progClbck.ProgressChanged += (x, y) => {
                double a = y.Progress.TotalBytesToSend;
                double b = y.Progress.BytesSent;
                double p = 100 / a * b;
                string c = $"Upload method 1 (BackgroundUploader)\nProgress: {Math.Round(p, 1)}; {b}b of {a}b...";
                ProgressChanged?.Invoke(a, b, p, c);
            };
        }

        public async Task<string> UploadAsync() {
            try {
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

                string boundary = $"----------------------------{Guid.NewGuid()}";
                UploadOperation upop = await bu.CreateUploadAsync(_uploadUri, list, "form-data", boundary);
                var op = await upop.StartAsync().AsTask(cts.Token, progClbck);

                var b = op.GetResultStreamAt(0);
                var b1 = await b.ReadAsync(new Windows.Storage.Streams.Buffer((uint)op.Progress.BytesReceived), (uint)op.Progress.BytesReceived, InputStreamOptions.None);
                using (DataReader lo = DataReader.FromBuffer(b1)) {
                    result = lo.ReadString(lo.UnconsumedBufferLength);
                    lo.Dispose();
                }

                b.Dispose();
                cts.Dispose();
                cts = null;
                list.Clear();
                return result;
            } catch (Exception ex) {
                UploadFailed?.Invoke(ex);
                return null;
            }
        }

        public void CancelUpload() {
            cts.Cancel();
            cts.Dispose();
        }
    }

    public class VKFileUploaderViaHttpClient : IFileUploader {
        CancellationTokenSource cts;
        string _type;
        Uri _uploadUri;
        StorageFile _file;
        Action<HttpProgress> _progressCallback;

        public event ProgressChangedDelegate ProgressChanged;
        public event UploadFailedDelegate UploadFailed;

        public VKFileUploaderViaHttpClient(string type, Uri uploadUri, StorageFile file) {
            _type = type;
            _uploadUri = uploadUri;
            _file = file;
            _progressCallback = ProgressCallback;
            cts = new CancellationTokenSource();
        }

        private void ProgressCallback(HttpProgress obj) {
            double a = (double)obj.TotalBytesToSend;
            double b = obj.BytesSent;
            double p = 100 / a * b;
            string c = $"Upload method 2 (HttpClient)\nProgress: {Math.Round(p, 1)}; {b}b of {a}b...";
            ProgressChanged?.Invoke(a, b, p, c);
        }

        public async Task<string> UploadAsync() {
            try {
                if (_type == null && _uploadUri == null && _file == null && _progressCallback == null)
                    throw new Exception("One of the important parameters is null");
                Stream data = await _file.OpenStreamForReadAsync();

                var filter = new HttpBaseProtocolFilter();
                filter.CacheControl.ReadBehavior = HttpCacheReadBehavior.NoCache;
                filter.CacheControl.WriteBehavior = HttpCacheWriteBehavior.NoCache;

                using (var httpClient = new HttpClient(filter)) {
                    string disposition = new string(Encoding.UTF8.GetBytes($"form-data; name=\"{_type}\"; filename=\"{_file.Name}\"").
                        Select(b => (char)b).ToArray());

                    HttpStreamContent filecontent = new HttpStreamContent(data.AsInputStream());
                    filecontent.Headers.Add("Content-Type", "application/octet-stream");
                    filecontent.Headers.Add("Content-Disposition", disposition);

                    HttpMultipartFormDataContent mfdc = new HttpMultipartFormDataContent($"----------------------------{Guid.NewGuid()}");
                    mfdc.Add(filecontent);

                    HttpRequestMessage hrm = new HttpRequestMessage();
                    hrm.Method = HttpMethod.Post;
                    hrm.RequestUri = _uploadUri;
                    hrm.Content = mfdc;

                    HttpResponseMessage response = await httpClient.SendRequestAsync(hrm, HttpCompletionOption.ResponseContentRead).AsTask(cts.Token, new Progress<HttpProgress>(_progressCallback));
                    response.EnsureSuccessStatusCode();
                    await Task.Delay(64);
                    data.Dispose();
                    return await response.Content.ReadAsStringAsync();
                }
            } catch (Exception ex) {
                UploadFailed?.Invoke(ex);
                return null;
            }
        }

        public void CancelUpload() {
            cts.Cancel();
            cts.Dispose();
        }
    }
}