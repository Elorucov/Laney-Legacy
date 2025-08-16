using Elorucov.Laney.Services.Media;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Elorucov.Laney.PreviewDebug.Pages {
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class AudioRecorderTest : Page {
        DispatcherTimer tmr = new DispatcherTimer();
        TimeSpan startTime;

        public AudioRecorderTest() {
            this.InitializeComponent();
            Loaded += (a, b) => {
                tmr.Interval = TimeSpan.FromMilliseconds(10);
                tmr.Tick += (c, d) => {
                    TimeSpan diff = DateTime.Now.TimeOfDay - startTime;
                    res.Text = $"Recorded: {diff.ToString(@"m\:ss")}.{diff.Milliseconds}\n";
                };
                audioRec.DoneButtonClicked += async (c, d) => {
                    await (new MessageDialog(d != null ? d.Path : "null", "File")).ShowAsync();
                };
            };
        }

        StorageFile file;

        private void OnStart(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                FileSavePicker fsp = new FileSavePicker();
                fsp.DefaultFileExtension = ".wav";
                fsp.SuggestedFileName = $"laneyrecording_{DateTimeOffset.Now.ToUnixTimeSeconds()}";
                fsp.FileTypeChoices.Add("Audio file", new List<string> { ".wav" });
                fsp.SuggestedStartLocation = PickerLocationId.ComputerFolder;
                file = await fsp.PickSaveFileAsync();

                if (file == null) file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync($"laneyrecording_{DateTimeOffset.Now.ToUnixTimeSeconds()}.wav", CreationCollisionOption.GenerateUniqueName);

                if (file != null) {
                    res.Text += "Starting...\n";
                    await Task.Delay(10);
                    await AudioRecorder2.StartAsync(file);
                    try {
                        res.Text += "Success.\n";
                        startTime = DateTime.Now.TimeOfDay;
                        tmr.Start();
                    } catch (Exception ex) {
                        res.Text += $"Exception: 0x{ex.HResult.ToString("x8")}\n{ex.Message}\nFile: {file.Path}\n";
                    }
                } else {
                    res.Text += "File not selected\n";
                }
            })();
        }

        private void OnStop(object sender, RoutedEventArgs e) {
            new System.Action(async () => {
                tmr.Stop();
                await AudioRecorder2.StopAsync();
                res.Text += $"Stopped! File: {file.Path}\n";
                file = null;
            })();
        }

        private void ShowUI(object sender, RoutedEventArgs e) {
            audioRec.Show();
        }
    }
}
