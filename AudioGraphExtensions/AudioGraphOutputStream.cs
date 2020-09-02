using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Audio;
using Windows.Media.MediaProperties;
using Windows.Media.Render;
using Windows.Storage;

namespace AudioGraphExtensions
{
    public class AudioGraphOutputStream
    {
        private readonly AudioGraph _audioGraph;
        private readonly IAudioInputNode _inputNode;
        private readonly IAudioNode _outputNode;
        private readonly Progress<double> _progress;
        private readonly IProgress<string> _status;

        protected AudioGraphOutputStream(
            IAudioInputNode inputNode,
            IAudioNode outputNode,
            AudioGraph audioGraph,
            Progress<double> progress,
            IProgress<string> status)
        {
            _inputNode = inputNode;
            _outputNode = outputNode;
            _audioGraph = audioGraph;
            _progress = progress;
            _status = status;
        }

        public static async Task<AudioGraphOutputStream> ToFile(
            StorageFile file,
            Progress<double> progress,
            IProgress<string> status,
            uint sampleRate,
            uint channelCount)
        {
            var resultGraph = await CreateAudioGraphAsync();

            if (resultGraph.Status != AudioGraphCreationStatus.Success) return null;

            var resultNode = await CreateAudioFileOutputNode(file, sampleRate, channelCount, resultGraph.Graph);

            if (resultNode.Status != AudioFileNodeCreationStatus.Success) return null;
            
            var frameInputNode = CreateAudioFrameInputNode(sampleRate, channelCount, resultGraph.Graph);

            var stream = new AudioGraphOutputStream(
                frameInputNode,
                resultNode.FileOutputNode,
                resultGraph.Graph,
                progress,
                status);

            return stream;
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