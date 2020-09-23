using System;
using System.Threading.Tasks;
using Windows.Media.Audio;
using Windows.Storage;

namespace AudioGraphExtensions
{
    public class AudioInputFile : IAudioInput
    {
        private readonly AudioFileInputNode _fileInputNode;
        private uint _sampleRate;
        private uint _channelCount;

        private AudioInputFile(AudioFileInputNode fileInputNode, int samplesPerQuantum)
        {
            _fileInputNode = fileInputNode;
            
            _sampleRate = fileInputNode.EncodingProperties.SampleRate;
            _channelCount = fileInputNode.EncodingProperties.ChannelCount;
            
            LengthInSamples =
                (uint)Math.Ceiling(fileInputNode.EncodingProperties.SampleRate * fileInputNode.Duration.TotalSeconds);
            LengthInQuantum = (uint)Math.Ceiling((double)LengthInSamples / samplesPerQuantum);
            
            _fileInputNode.FileCompleted += OnFileCompleted;
        }

        private void OnFileCompleted(AudioFileInputNode sender, object args)
        {
            InputEnded?.Invoke(this, EventArgs.Empty);
        }

        public static async Task<AudioInputFile> CreateAsync(
            StorageFile file,
            AudioGraph graph)
        {
            var inputNode = await CreateAudioFileInputNode(file, graph);

            return new AudioInputFile(inputNode, graph.SamplesPerQuantum);
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

        public event EventHandler InputEnded;
    }
}