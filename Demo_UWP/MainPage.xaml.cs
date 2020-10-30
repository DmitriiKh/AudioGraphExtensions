using AudioGraphExtensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Demo_UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private StorageFile _source;
        private StorageFile _target;

        public string SourcePath;
        public string TargetPath;
        public string AppState;

        public MainPage()
        {
            this.InitializeComponent();
            DataContext = this;
        }

        private async void SourceButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            _source = await PickInputFileAsync();
            Source.Text = _source?.Path;
        }

        private static async Task<StorageFile> PickInputFileAsync()
        {
            var filePicker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.MusicLibrary
            };

            filePicker.FileTypeFilter.Add(".mp3");
            filePicker.FileTypeFilter.Add(".wav");
            filePicker.FileTypeFilter.Add(".wma");
            filePicker.FileTypeFilter.Add(".m4a");
            filePicker.ViewMode = PickerViewMode.Thumbnail;

            return await filePicker.PickSingleFileAsync();
        }

        private async void TargetButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            _target = await PickOutputFileAsync();
            Target.Text = _target?.Path;
        }

        private async Task<StorageFile> PickOutputFileAsync()
        {
            var filePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.MusicLibrary,
                SuggestedFileName = _source.Name
            };

            filePicker.FileTypeChoices.Add("Audio file", new List<string> { ".mp3", ".wav", ".wma", ".m4a" });

            return await filePicker.PickSaveFileAsync();
        }

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            State.Text = "Starting...";
            var builder = AudioSystem.Builder();
            builder.From(_source).To(_target);

            try
            {
                State.Text = "Building...";
                var audioSystem = await builder.BuildAsync();
                State.Text = "Running...";
                var result = await audioSystem.RunAsync();
                var success = result.Success;
                audioSystem.Dispose();

                State.Text = success ? "Done." : "Something is wrong.";
            }
            catch (Exception exc)
            {
                State.Text = exc.Message;
            }
        }
    }
}
