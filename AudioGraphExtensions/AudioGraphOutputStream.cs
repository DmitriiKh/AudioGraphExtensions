using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.MediaProperties;
using Windows.Media.Render;
using Windows.Media.Transcoding;
using Windows.Storage;

namespace AudioGraphExtensions
{
    public class AudioGraphOutputStream
    {
        private readonly AudioGraph _audioGraph;
        private readonly IAudioNode _outputNode;
        private readonly IProgress<double> _progress;
        private readonly IProgress<string> _status;
        private int _audioCurrentPosition;
        private TaskCompletionSource<bool> _writeFileSuccess;
        private float[] _leftChannel;
        private float[] _rightChannel;
        private readonly Func<AudioGraph, IAudioNode, bool> _finalizer;

        protected AudioGraphOutputStream(
            AudioFrameInputNode inputNode,
            IAudioNode outputNode,
            AudioGraph audioGraph,
            IProgress<double> progress,
            IProgress<string> status,
            Func<AudioGraph, IAudioNode, bool> finalizer)
        {
            _finalizer = finalizer;
            _outputNode = outputNode;
            _audioGraph = audioGraph;
            _progress = progress;
            _status = status;
            
            // Connect nodes
            inputNode.AddOutgoingConnection(outputNode);
            inputNode.QuantumStarted += FrameInputNode_QuantumStarted;
        }

        public async Task<bool> Transfer(float[] left, float[] rihgt = null)
        {
            _leftChannel = left;
            _rightChannel = rihgt;
            
            // Reset position
            _audioCurrentPosition = 0;

            // Prepare to return results asynchronously
            _writeFileSuccess = new TaskCompletionSource<bool>();

            // Start process which will write audio samples frame by frame
            // and will generated events QuantumStarted 
            _audioGraph.Start();            

            return await _writeFileSuccess.Task;
        }

        public static async Task<AudioGraphOutputStream> ToFile(
            StorageFile file,
            IProgress<double> progress,
            IProgress<string> status,
            uint sampleRate,
            uint channelCount)
        {
            var resultGraph = await CreateAudioGraphAsync();

            if (resultGraph.Status != AudioGraphCreationStatus.Success) return null;

            var resultOutputNode = await CreateAudioFileOutputNode(file, sampleRate, channelCount, resultGraph.Graph);

            if (resultOutputNode.Status != AudioFileNodeCreationStatus.Success) return null;
            
            var frameInputNode = CreateAudioFrameInputNode(sampleRate, channelCount, resultGraph.Graph);

            var stream = new AudioGraphOutputStream(
                frameInputNode,
                resultOutputNode.FileOutputNode,
                resultGraph.Graph,
                progress,
                status,
                FileFinalizer);

            return stream;
        }

        private static bool FileFinalizer(AudioGraph graph, IAudioNode outputNode)
        {
            graph?.Stop();
            outputNode.Stop();
            
            var result = ((AudioFileOutputNode) outputNode)
                .FinalizeAsync()
                .GetResults();

            return result == TranscodeFailureReason.None;
        }

        private static AudioFrameInputNode CreateAudioFrameInputNode(
            uint sampleRate,
            uint channelCount,
            AudioGraph graph)
        {
            var frameInputNodeProperties = graph.EncodingProperties;

            frameInputNodeProperties.SampleRate = sampleRate;
            frameInputNodeProperties.ChannelCount = channelCount;

            var frameInputNode = graph.CreateFrameInputNode(
                frameInputNodeProperties
            );

            return frameInputNode;
        }
        
        private void FrameInputNode_QuantumStarted(
            AudioFrameInputNode sender,
            FrameInputNodeQuantumStartedEventArgs args)
        {
            var numSamplesNeeded = args.RequiredSamples;

            if (numSamplesNeeded == 0 || _audioGraph is null)
                return;

            var (frame, finished) = ProcessOutputFrame(numSamplesNeeded);
            
            sender.AddFrame(frame);

            if (finished)
            {
                _writeFileSuccess.SetResult(_finalizer(_audioGraph, _outputNode));

                // clean status and progress 
                _status.Report("");
                _progress.Report(0);

                return;
            }

            // to not report too many times
            if (_audioGraph.CompletedQuantumCount % 100 == 0)
            {
                var dProgress =
                    (double) 100 *
                    _audioCurrentPosition /
                    _leftChannel.Length;

                _progress?.Report(dProgress);
            }
        }
        
        private unsafe (AudioFrame frame, bool finished)
            ProcessOutputFrame(int requiredSamples)
        {
            var channelCount = _rightChannel is null ? 1u : 2u;

            var bufferSize = (uint)(requiredSamples * sizeof(float) * channelCount);

            var frame = new AudioFrame(bufferSize);

            using (var buffer = frame.LockBuffer(AudioBufferAccessMode.Write))
            using (var reference = buffer.CreateReference())
            {
                // Get the buffer from the AudioFrame
                (reference as IMemoryBufferByteAccess).GetBuffer(
                    out var dataInBytes,
                    out var capacityInBytes);

                // Cast to float since the data we are generating is float
                var dataInFloat = (float*) dataInBytes;

                var capacityInFloat = capacityInBytes / sizeof(float);

                for (uint index = 0; index < capacityInFloat; index += channelCount)
                {
                    if (_audioCurrentPosition >= _leftChannel.Length)
                    {
                        // last frame can be not full
                        return (frame, true);
                    }

                    dataInFloat[index] = _leftChannel[_audioCurrentPosition];

                    // if output is stereo
                    if (channelCount == 2 && _rightChannel != null)
                    {
                        dataInFloat[index + 1] = _rightChannel[_audioCurrentPosition];
                    }

                    _audioCurrentPosition++;
                }
            }

            return (frame, false);
        }

        private static IAsyncOperation<CreateAudioGraphResult> CreateAudioGraphAsync()
        {
            return AudioGraph.CreateAsync(new AudioGraphSettings(AudioRenderCategory.Media));
        }

        private static async Task<CreateAudioFileOutputNodeResult> CreateAudioFileOutputNode(
            StorageFile file,
            uint sampleRate,
            uint channelCount,
            AudioGraph graph)
        {
            var mediaEncodingProfile = CreateMediaEncodingProfile(file);

            if (mediaEncodingProfile.Audio != null)
            {
                mediaEncodingProfile.Audio.SampleRate = sampleRate;
                mediaEncodingProfile.Audio.ChannelCount = channelCount;
            }

            var result = await graph.CreateFileOutputNodeAsync(
                file,
                mediaEncodingProfile);

            return result;
        }

        private static MediaEncodingProfile CreateMediaEncodingProfile(StorageFile file)
        {
            return file.FileType.ToLowerInvariant() switch
            {
                ".wma" => MediaEncodingProfile.CreateWma(AudioEncodingQuality.High),
                ".mp3" => MediaEncodingProfile.CreateMp3(AudioEncodingQuality.High),
                ".wav" => MediaEncodingProfile.CreateWav(AudioEncodingQuality.High),
                _ => throw new ArgumentException("Can't create MediaEncodingProfile for this file extension")
            };
        }
    }
}