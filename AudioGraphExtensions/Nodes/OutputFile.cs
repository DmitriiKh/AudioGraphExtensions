using System;
using System.Threading.Tasks;
using Windows.Media.Audio;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Storage;

namespace AudioGraphExtensions.Nodes
{
    internal sealed class OutputFile : IAudioOutput
    {
        private readonly AudioFileOutputNode _fileOutputNode;

        private OutputFile(AudioFileOutputNode fileOutputNode)
        {
            _fileOutputNode = fileOutputNode;
        }

        public IAudioNode Node => _fileOutputNode;

        public async Task<RunResult> FinalizeAsync()
        {
            _fileOutputNode.Stop();

            var sampleRate = _fileOutputNode.EncodingProperties.SampleRate;
            var file = _fileOutputNode.File;

            var finalizeResult = await _fileOutputNode.FinalizeAsync();

            var success = finalizeResult == TranscodeFailureReason.None;

            return new RunResult(success, sampleRate, file);
        }

        public static async Task<OutputFile> CreateAsync(
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

            var result = await graph.CreateFileOutputNodeAsync(file, mediaEncodingProfile);

            if (result.Status != AudioFileNodeCreationStatus.Success) throw result.ExtendedError;

            return new OutputFile(result.FileOutputNode);
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