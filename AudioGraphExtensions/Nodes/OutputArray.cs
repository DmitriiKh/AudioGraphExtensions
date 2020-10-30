using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Audio;

namespace AudioGraphExtensions.Nodes
{
    public class OutputArray : IAudioOutput
    {
        private readonly AudioFrameOutputNode _frameOutputNode;
        private readonly float[] _leftChannel;
        private readonly float[] _rightChannel;
        private int _audioCurrentPosition;
        
        public OutputArray(
            AudioGraph graph,
            uint sampleRate,
            uint channelCount,
            float[] left,
            float[] right)
        {
            _leftChannel = left;
            _rightChannel = right;

            var properties = graph.EncodingProperties;
            properties.SampleRate = sampleRate;
            properties.ChannelCount = channelCount;

            _frameOutputNode = graph.CreateFrameOutputNode(properties);
            graph.QuantumStarted += GraphOnQuantumStarted;
        }

        private void GraphOnQuantumStarted(AudioGraph sender, object args)
        {
            var frame = _frameOutputNode.GetFrame();
            FrameToArray(frame);
        }
        
        private unsafe void FrameToArray(AudioFrame frame)
        {
            using (var buffer = frame.LockBuffer(AudioBufferAccessMode.Read))
            using (var reference = buffer.CreateReference())
            {
                // Get data from current buffer
                (reference as IMemoryBufferByteAccess).GetBuffer(
                    out var dataInBytes,
                    out var capacityInBytes
                );

                var dataInFloat = (float*) dataInBytes;

                var capacityInFloat = capacityInBytes / sizeof(float);

                var channelCount = _rightChannel is null ? 1u : 2u;

                // Transfer audio samples from buffer to audio arrays
                for (uint index = 0; index < capacityInFloat; index += channelCount)
                {
                    if (_audioCurrentPosition >= _leftChannel.Length)
                    {
                        break;
                    }

                    _leftChannel[_audioCurrentPosition] = dataInFloat[index];

                    // if stereo
                    if (channelCount == 2)
                    {
                        _rightChannel[_audioCurrentPosition] = dataInFloat[index + 1];
                    }

                    _audioCurrentPosition++;
                }
            }
        }

        public IAudioNode Node => _frameOutputNode;
        
        public Task<RunResult> Finalize()
        {
            _frameOutputNode.Stop();

            var sampleRate = _frameOutputNode.EncodingProperties.SampleRate;

            return Task.FromResult(new RunResult(true, sampleRate, _leftChannel, _rightChannel));
        }
    }
}