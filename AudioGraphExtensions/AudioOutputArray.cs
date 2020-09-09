using Windows.Media.Audio;

namespace AudioGraphExtensions
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

            _frameOutputNode = graph.CreateFrameOutputNode();
        }

        public IAudioNode Node => _frameOutputNode;
        
        public bool Stop()
        {
            throw new System.NotImplementedException();
        }
    }
}