using Windows.Media;
using Windows.Media.Audio;

namespace AudioGraphExtensions.Nodes
{
    public class AudioOutputArray : IAudioOutput
    {
        private readonly AudioFrameOutputNode _frameOutputNode;
        private readonly uint _sampleRate;
        private readonly uint _channelCount;
        private readonly float[] _leftChannel;
        private readonly float[] _rightChannel;
        private int _audioCurrentPosition;
        
        public AudioOutputArray(
            AudioGraph graph,
            uint sampleRate,
            uint channelCount,
            float[] left,
            float[] right)
        {
            _sampleRate = sampleRate;
            _channelCount = channelCount;
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

                // Transfer audio samples from buffer to audio arrays
                for (uint index = 0; index < capacityInFloat; index += _channelCount)
                {
                    if (_audioCurrentPosition >= _leftChannel.Length)
                    {
                        break;
                    }

                    _leftChannel[_audioCurrentPosition] = dataInFloat[index];

                    // if stereo
                    if (_channelCount == 2)
                    {
                        _rightChannel[_audioCurrentPosition] = dataInFloat[index + 1];
                    }

                    _audioCurrentPosition++;
                }
            }
        }

        public IAudioNode Node => _frameOutputNode;
        
        public RunResult Stop()
        {
            return new RunResult(true, _sampleRate, _leftChannel, _rightChannel);
        }
    }
}