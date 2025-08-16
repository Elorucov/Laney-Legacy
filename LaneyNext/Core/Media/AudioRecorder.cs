using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Media.Render;
using Windows.Storage;

namespace Elorucov.Laney.Core.Media
{
    public class AudioRecorderService
    {
        static AudioGraph graph;
        static AudioFileOutputNode outputNode;

        public static bool IsRecording { get; private set; }

        #region Public functions

        //public static async Task<AudioDeviceNodeCreationStatus> IsMicrophoneAvailable() {
        //    AudioDeviceNodeCreationStatus res = AudioDeviceNodeCreationStatus.UnknownFailure;
        //    var result = await AudioGraph.CreateAsync(new AudioGraphSettings(AudioRenderCategory.Speech));
        //    if (result.Status == AudioGraphCreationStatus.Success) {
        //        graph = result.Graph;

        //        var microphone = await DeviceInformation.CreateFromIdAsync(MediaDevice.GetDefaultAudioCaptureId(AudioDeviceRole.Default));

        //        var inProfile = MediaEncodingProfile.CreateWav(AudioEncodingQuality.Medium);
        //        var inputResult = await graph.CreateDeviceInputNodeAsync(MediaCategory.Speech, inProfile.Audio, microphone);
        //        res = inputResult.Status;
        //        if (res == AudioDeviceNodeCreationStatus.Success) {
        //            inputResult.DeviceInputNode.Dispose();
        //        }
        //        result.Graph.Dispose();
        //    }
        //    return res;
        //}

        public static async Task<AudioDeviceNodeCreationStatus> StartAsync(StorageFile file)
        {
            AudioDeviceNodeCreationStatus status = AudioDeviceNodeCreationStatus.UnknownFailure;
            if (file == null) throw new ArgumentNullException("file");
            var result = await AudioGraph.CreateAsync(new AudioGraphSettings(AudioRenderCategory.Speech));
            Log.General.Info($"AudioGraph result: {result.Status}");
            if (result.Status != AudioGraphCreationStatus.Success) throw new Exception($"AudioGraph creation failed! {result.Status}");

            graph = result.Graph;
            var microphone = await DeviceInformation.CreateFromIdAsync(MediaDevice.GetDefaultAudioCaptureId(AudioDeviceRole.Default));
            var outProfile = MediaEncodingProfile.CreateM4a(AudioEncodingQuality.Medium);
            outProfile.Audio.ChannelCount = 1;

            var outputResult = await graph.CreateFileOutputNodeAsync(file, outProfile);
            Log.General.Info($"CreateFileOutputNode result: {outputResult.Status}");
            if (outputResult.Status != AudioFileNodeCreationStatus.Success) throw new Exception($"FileOutputNode creation failed! {outputResult.Status}");

            outputNode = outputResult.FileOutputNode;
            var inProfile = MediaEncodingProfile.CreateWav(AudioEncodingQuality.High);
            var inputResult = await graph.CreateDeviceInputNodeAsync(MediaCategory.Speech, inProfile.Audio, microphone);
            Log.General.Info(null, $"CreateDeviceInputNode result: {inputResult.Status}");
            status = inputResult.Status;
            if (inputResult.Status == AudioDeviceNodeCreationStatus.Success)
            {
                inputResult.DeviceInputNode.AddOutgoingConnection(outputNode);
                graph.Start();
                IsRecording = true;
                Log.General.Info(null, $"Start recording!");
            }
            return status;
        }

        public static async Task<bool> StopAsync()
        {
            Log.General.Info(String.Empty);
            IsRecording = false;
            if (graph != null)
            {
                graph?.Stop();

                await outputNode.FinalizeAsync();
                graph?.Dispose();
                graph = null;
                return true;
            }
            return false;
        }

        #endregion
    }
}