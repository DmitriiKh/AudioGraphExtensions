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
        private uint _sampleRate;
        private readonly IProgress<double> _progress;
        private readonly IProgress<string> _status;
        private readonly TaskCompletionSource<RunResult> _runAsyncCompletion;
        private AudioGraph _audioGraph;
        private IAudioInput _audioInput;
        private IAudioOutput _audioOutput;
        private bool _lastQuantum;
        private bool _finalizing;

        internal AudioSystem(
            uint sampleRate,
            IProgress<double> progress = null,
            IProgress<string> status = null)
        {
            _sampleRate = sampleRate;
            _progress = progress;
            _status = status;

            // Prepare to return results asynchronously
            _runAsyncCompletion = new TaskCompletionSource<RunResult>();
        }

        internal uint InputLength => _audioInput.LengthInSamples;

        public bool IsStereo => _audioInput.Node.EncodingProperties.ChannelCount == 2;

        internal async Task InitAsync()
        {
            _audioGraph = await CreateAudioGraphAsync();
            _audioGraph.QuantumProcessed += AudioSystem_QuantumProcessed;
            _audioGraph.QuantumStarted += AudioSystem_QuantumStarted;
        }

        private void AudioSystem_QuantumStarted(AudioGraph sender, object args)
        {
            if (_audioGraph.CompletedQuantumCount == _audioInput.LengthInQuantum)
            {
                _lastQuantum = true;
            }    
        }

        internal async Task SetInputAsync(StorageFile file)
        {
            _audioInput = await InputFile.CreateAsync(file, _audioGraph);

            InheritInputSetting();
        }

        private void InheritInputSetting()
        {
            _sampleRate = _audioInput.Node.EncodingProperties.SampleRate;
        }

        internal void SetInput(float[] left, float[] right = null)
        {
            var channelCount = right is null ? 1u : 2u;

            _audioInput = new InputArray(_audioGraph, _sampleRate, channelCount, left, right);
        }

        internal async Task SetOutputAsync(StorageFile file)
        {
            var channelCount = _audioInput.Node.EncodingProperties.ChannelCount;

            _audioOutput = await OutputFile.CreateAsync(file, _sampleRate, channelCount, _audioGraph);
        }

        internal void SetOutput(float[] left, float[] right = null)
        {
            var channelCount = right is null ? 1u : 2u;

            _audioOutput = new OutputArray(_audioGraph, _sampleRate, channelCount, left, right);
        }

        public async Task<RunResult> RunAsync()
        {
            _lastQuantum = false;
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

        private static async Task<AudioGraph> CreateAudioGraphAsync()
        {
            var mediaSettings = new AudioGraphSettings(AudioRenderCategory.Media);

            var resultGraph = await AudioGraph.CreateAsync(mediaSettings);

            if (resultGraph.Status != AudioGraphCreationStatus.Success) throw resultGraph.ExtendedError;

            return resultGraph.Graph;
        }

        private void AudioSystem_QuantumProcessed(AudioGraph sender, object args)
        {
            if (_lastQuantum)
            {
                if (_finalizing)
                {
                    return;
                }

                _finalizing = true;

                _audioGraph.Stop();
                FinalizeAsync();
            }
            else
            {
                ReportProgress();
            }
        }

        private async void FinalizeAsync()
        {
            var result = await _audioOutput.Finalize();

            _runAsyncCompletion.SetResult(result);

            // clean status and progress 
            _status?.Report("");
            _progress?.Report(0);
        }
    }
}