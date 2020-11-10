using System;
using System.Threading.Tasks;
using Windows.Media.Audio;
using Windows.Storage;

namespace AudioGraphExtensions.Nodes
{
    public class InputFile : IAudioInput
    {
        private readonly AudioFileInputNode _fileInputNode;

        private InputFile(AudioFileInputNode fileInputNode)
        {
            _fileInputNode = fileInputNode;
                        
            LengthInSamples =
                (uint)Math.Ceiling(fileInputNode.EncodingProperties.SampleRate * fileInputNode.Duration.TotalSeconds);

            var samplesPerQuantum = fileInputNode.EncodingProperties.SampleRate / 100; // each quantum is 10ms

            LengthInQuantum = (uint)Math.Ceiling((double)LengthInSamples / samplesPerQuantum);
        }

        public static async Task<InputFile> CreateAsync(
            IStorageFile file,
            AudioGraph graph)
        {
            var result = await graph.CreateFileInputNodeAsync(file);

            if (result.Status != AudioFileNodeCreationStatus.Success)
            {
                throw result.ExtendedError;
            }

            return new InputFile(result.FileInputNode);
        }

        public IAudioInputNode Node => _fileInputNode;
        public uint LengthInQuantum { get; }
        public uint LengthInSamples { get; }
    }
}