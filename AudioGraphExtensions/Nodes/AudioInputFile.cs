using System;
using System.Threading.Tasks;
using Windows.Media.Audio;
using Windows.Storage;

namespace AudioGraphExtensions
{
    public class AudioInputFile : IAudioInput
    {
        private readonly AudioFileInputNode _fileInputNode;

        private AudioInputFile(AudioFileInputNode fileInputNode)
        {
            _fileInputNode = fileInputNode;
                        
            LengthInSamples =
                (uint)Math.Ceiling(fileInputNode.EncodingProperties.SampleRate * fileInputNode.Duration.TotalSeconds);

            uint samplesPerQuantum = fileInputNode.EncodingProperties.SampleRate / 100; // each quantum is 10ms

            LengthInQuantum = (uint)Math.Ceiling((double)LengthInSamples / samplesPerQuantum);
        }

        public static async Task<AudioInputFile> CreateAsync(
            StorageFile file,
            AudioGraph graph)
        {
            var inputNode = await CreateAudioFileInputNode(file, graph);

            return new AudioInputFile(inputNode);
        }

        private static async Task<AudioFileInputNode> CreateAudioFileInputNode(
            StorageFile file,
            AudioGraph graph)
        {
            var result = await graph.CreateFileInputNodeAsync(file);

            if (result.Status != AudioFileNodeCreationStatus.Success)
            {
                throw result.ExtendedError;
            }

            return result.FileInputNode;
        }

        public IAudioInputNode Node => _fileInputNode;
        public uint LengthInQuantum { get; }
        public uint LengthInSamples { get; }
    }
}