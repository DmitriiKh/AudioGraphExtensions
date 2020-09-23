using System;
using System.Threading.Tasks;
using Windows.Media.Audio;
using Windows.Media.Render;
using Windows.Storage;
using AudioGraphExtensions.Nodes;

namespace AudioGraphExtensions
{
    public class AudioSystem : IDisposable
    {
        private uint _channelCount;
        private uint _sampleRate;
        private readonly IProgress<double> _progress;
        private readonly IProgress<string> _status;
        private readonly TaskCompletionSource<RunResult> _runAsyncCompletion;
        private AudioGraph _audioGraph;
        private IAudioInput _audioInput;
        private IAudioOutput _audioOutput;
        private bool _lastFrame;
        private bool _finalizing;

        internal AudioSystem(
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
            _runAsyncCompletion = new TaskCompletionSource<RunResult>();
        }

        internal int InputLength => _audioInput.LengthInSamples;
        
        internal bool IsStereo => _channelCount == 2;

        internal async Task InitAsync()
        {
            _audioGraph = await CreateAudioGraphAsync();
            _audioGraph.QuantumProcessed += AudioSystem_QuantumProcessed;
        }

        internal async Task SetInputAsync(StorageFile file)
        {
            _audioInput = await AudioInputFile.CreateAsync(file, _audioGraph);

            InheritInputSetting();

            _audioInput.InputEnded += OnLastFrame;
        }

        private void InheritInputSetting()
        {
            _channelCount = _audioInput.Node.EncodingProperties.ChannelCount;
            _sampleRate = _audioInput.Node.EncodingProperties.SampleRate;
        }

        internal void SetInput(float[] left, float[] right = null)
        {
            _audioInput = new AudioInputArray(_audioGraph, _sampleRate, _channelCount, left, right);

            _audioInput.InputEnded += OnLastFrame;
        }

        internal async Task SetOutputAsync(StorageFile file)
        {
            _audioOutput = await AudioOutputFile.CreateAsync(file, _sampleRate, _channelCount, _audioGraph);
        }

        internal void SetOutput(float[] left, float[] right)
        {
            _audioOutput = new AudioOutputArray(_audioGraph, _sampleRate, _channelCount, left, right);
        }

        public async Task<RunResult> RunAsync()
        {
            _lastFrame = false;
            _finalizing = false;

            _status?.Report("Working...");

            _audioInput.Node.AddOutgoingConnection(_audioOutput.Node);

            _audioGraph.Start();

            return await _runAsyncCompletion.Task;
        }

        public void Dispose()
        {
            _audioGraph.Dispose();
        }

        public static AudioSystemBuilder Builder()
        {
            return new AudioSystemBuilder();
        }

        public static async Task<int> GetDefaultQuantumSizeAsync()
        {
            var graph = await CreateAudioGraphAsync();
            return graph.SamplesPerQuantum;
        }

        private void ReportProgress()
        {
            //to not report too many times
            if (_audioGraph.CompletedQuantumCount % 10 != 0) return;

            var dProgress =
                (double) 100 *
                _audioGraph.CompletedQuantumCount /
                _audioInput.LengthInQuantum;

            _progress?.Report(dProgress);
        }

        private void OnLastFrame(object sender, EventArgs e)
        {
            _lastFrame = true;
        }

        private static async Task<AudioGraph> CreateAudioGraphAsync()
        {
            var mediaSettings = new AudioGraphSettings(AudioRenderCategory.Media);

            var resultGraph = await AudioGraph.CreateAsync(mediaSettings);

            if (resultGraph.Status != AudioGraphCreationStatus.Success) throw resultGraph.ExtendedError;

            return resultGraph.Graph;
        }

        private void AudioSystem_QuantumProcessed(AudioGraph sender, object args)
        {
            if (_lastFrame)
            {
                if (_finalizing)
                {
                    return;
                }

                _finalizing = true;

                //_audioOutput.Node.Stop();
                _audioGraph.Stop();

                var result = _audioOutput.Stop();

                _runAsyncCompletion.SetResult(result);

                // clean status and progress 
                _status?.Report("");
                _progress?.Report(0);
            }
            else
            {
                ReportProgress();
            }
        }
    }
}