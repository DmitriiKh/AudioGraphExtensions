using System;
using System.Threading.Tasks;
using Windows.Media.Audio;
using Windows.Media.Render;
using Windows.Storage;

namespace AudioGraphExtensions
{
    public class AudioSystem
    {
        private readonly uint _channelCount;
        private readonly uint _sampleRate;
        private readonly IProgress<double> _progress;
        private readonly IProgress<string> _status;
        private readonly TaskCompletionSource<RunResult> _writeFileSuccess;
        private AudioGraph _audioGraph;
        private IAudioInput _audioInput;
        private IAudioOutput _audioOutput;

        public AudioSystem(
            uint sampleRate,
            uint channelCount,
            IProgress<double> progress = null,
            IProgress<string> status = null)
        {
            _sampleRate = sampleRate;
            _channelCount = channelCount;
            _progress = progress;
            _status = status;

            // Prepare to return results asynchronously
            _writeFileSuccess = new TaskCompletionSource<RunResult>();
        }

        public int InputLength => _audioInput.LengthInSamples;
        
        public bool IsStereo => _channelCount == 2;

        public async Task InitAsync()
        {
            _audioGraph = await CreateAudioGraphAsync();
        }

        public async Task SetInput(StorageFile file)
        {
            _audioInput = await AudioInputFile.CreateAsync(file, _audioGraph);

            _audioInput.InputEnded += Stop;
        }

        public void SetInput(float[] left, float[] right = null)
        {
            _audioInput = new AudioInputArray(_audioGraph, _sampleRate, _channelCount, left, right);

            _audioInput.InputEnded += Stop;
        }

        public async Task SetOutputAsync(StorageFile file)
        {
            _audioOutput = await AudioOutputFile.CreateAsync(file, _sampleRate, _channelCount, _audioGraph);
        }

        public void SetOutput(float[] left, float[] right)
        {
            _audioOutput = new AudioOutputArray(_audioGraph, _sampleRate, _channelCount, left, right);
        }

        public async Task<RunResult> RunAsync()
        {
            _status?.Report("Saving audio file");

            _audioInput.Node.AddOutgoingConnection(_audioOutput.Node);

            _audioGraph.Start();

            return await _writeFileSuccess.Task;
        }

        public static AudioSystemBuilder Builder(
            uint sampleRate,
            uint channelCount,
            IProgress<double> progress = null,
            IProgress<string> status = null)
        {
            return new AudioSystemBuilder(sampleRate, channelCount, progress, status);
        }

        private void ReportProgress(AudioGraph sender, object args)
        {
            //to not report too many times
            if (_audioGraph.CompletedQuantumCount % 100 != 0) return;

            var dProgress =
                (double) 100 *
                _audioGraph.CompletedQuantumCount /
                _audioInput.LengthInQuantum;

            _progress?.Report(dProgress);
        }

        private void Stop(object sender, EventArgs e)
        {
            _audioGraph?.Stop();
            var finalizeResult = _audioOutput.Stop();

            _writeFileSuccess.SetResult(finalizeResult);

            // clean status and progress 
            _status?.Report("");
            _progress?.Report(0);
        }

        private async Task<AudioGraph> CreateAudioGraphAsync()
        {
            var mediaSettings = new AudioGraphSettings(AudioRenderCategory.Media);
            var resultGraph = await AudioGraph.CreateAsync(mediaSettings);

            if (resultGraph.Status != AudioGraphCreationStatus.Success) throw resultGraph.ExtendedError;

            resultGraph.Graph.QuantumProcessed += ReportProgress;

            return resultGraph.Graph;
        }
    }
}