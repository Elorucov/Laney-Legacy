using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Elorucov.Laney.Services.Media {
    public class AudioRecorder2 {
        private static MediaCapture _mediaCapture;
        private static InMemoryRandomAccessStream _memoryBuffer;
        private static StorageFile _file;

        public static bool IsRecording { get; private set; }

        private static async Task InitializeAsync() {
            if (_memoryBuffer != null) {
                _memoryBuffer.Dispose();
            }

            _memoryBuffer = new InMemoryRandomAccessStream();

            if (_mediaCapture != null) {
                _mediaCapture.Dispose();
            }

            var devices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(DeviceClass.AudioCapture);
            if (devices.Count < 1) throw new NotSupportedException("Audio input device not available");
        }

        public static async Task StartAsync(StorageFile file) {
            if (IsRecording) {
                throw new InvalidOperationException("Recording already in progress!");
            }

            await InitializeAsync();
            _file = file;

            MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings {
                StreamingCaptureMode = StreamingCaptureMode.Audio,
                MediaCategory = MediaCategory.Speech
            };

            _mediaCapture = new MediaCapture();
            await _mediaCapture.InitializeAsync(settings);
            await _mediaCapture.StartRecordToStreamAsync(MediaEncodingProfile.CreateWav(AudioEncodingQuality.Low), _memoryBuffer);

            IsRecording = true;
        }

        public static async Task StopAsync() {
            await _mediaCapture.StopRecordAsync();

            await SaveAudioToFileAsync();
            _mediaCapture?.Dispose();
            _memoryBuffer?.Dispose();
            IsRecording = false;
        }

        private static async Task SaveAudioToFileAsync() {
            IRandomAccessStream audioStream = _memoryBuffer.CloneStream();

            using (IRandomAccessStream fileStream = await _file.OpenAsync(FileAccessMode.ReadWrite)) {
                await RandomAccessStream.CopyAndCloseAsync(audioStream.GetInputStreamAt(0), fileStream.GetOutputStreamAt(0));
                await audioStream.FlushAsync();
                audioStream.Dispose();
            }
        }
    }
}
