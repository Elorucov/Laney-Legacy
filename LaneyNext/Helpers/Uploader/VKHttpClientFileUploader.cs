using Elorucov.Laney.Core;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace Elorucov.Laney.Helpers.Uploader
{
    public class VKHttpClientFileUploader : IFileUploader
    {
        CancellationTokenSource cts;
        string _type;
        Uri _uploadUri;
        StorageFile _file;
        Action<HttpProgress> _progressCallback;

        public event ProgressChangedDelegate ProgressChanged;
        public event UploadFailedDelegate UploadFailed;

        public VKHttpClientFileUploader(string type, Uri uploadUri, StorageFile file)
        {
            _type = type;
            _uploadUri = uploadUri;
            _file = file;
            _progressCallback = ProgressCallback;
            cts = new CancellationTokenSource();
        }

        private void ProgressCallback(HttpProgress obj)
        {
            double a = (double)obj.TotalBytesToSend;
            double b = obj.BytesSent;
            double p = 100 / a * b;
            string c = $"Upload method 2 (HttpClient)\nProgress: {Math.Round(p, 1)}; {b}b of {a}b...";
            Log.General.Verbose(c);
            ProgressChanged?.Invoke(a, b, p, c);
        }

        public async Task<string> UploadAsync()
        {
            try
            {
                if (_type == null && _uploadUri == null && _file == null && _progressCallback == null)
                    throw new Exception("One of the important parameters is null");
                Stream data = await _file.OpenStreamForReadAsync();

                var filter = new HttpBaseProtocolFilter();
                filter.CacheControl.ReadBehavior = HttpCacheReadBehavior.NoCache;
                filter.CacheControl.WriteBehavior = HttpCacheWriteBehavior.NoCache;

                using (var httpClient = new HttpClient(filter))
                {
                    string disposition = new string(Encoding.UTF8.GetBytes($"form-data; name=\"{_type}\"; filename=\"{_file.Name}\"")
                        .Select(b => (char)b).ToArray());

                    HttpStreamContent filecontent = new HttpStreamContent(data.AsInputStream());
                    filecontent.Headers.Add("Content-Type", "application/octet-stream");
                    filecontent.Headers.Add("Content-Disposition", disposition);

                    string boundary = $"----------------------------{Guid.NewGuid()}";
                    HttpMultipartFormDataContent mfdc = new HttpMultipartFormDataContent(boundary);
                    mfdc.Add(filecontent);

                    HttpRequestMessage hrm = new HttpRequestMessage();
                    hrm.Method = HttpMethod.Post;
                    hrm.RequestUri = _uploadUri;
                    hrm.Content = mfdc;

                    Log.General.Info("Starting upload...", new ValueSet { { "url", _uploadUri.ToString() }, { "boundary", boundary } });
                    HttpResponseMessage response = await httpClient.SendRequestAsync(hrm, HttpCompletionOption.ResponseContentRead).AsTask(cts.Token, new Progress<HttpProgress>(_progressCallback));
                    return await response.Content.ReadAsStringAsync();
                }
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
