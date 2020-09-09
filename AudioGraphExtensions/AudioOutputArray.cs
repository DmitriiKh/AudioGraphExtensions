using Windows.Media.Audio;

namespace AudioGraphExtensions
{
    public class AudioOutputArray : IAudioOutput
    {
        private readonly AudioFrameOutputNode _frameOutputNode;
        private readonly uint _channelCount;
        private readonly float[] _leftChannel;
        private readonly float[] _rightChannel;
        private int _audioCurrentPosition;
        
        public AudioOutputArray(
            AudioGraph graph,
            uint sampleRate,
            uint channelCount,
            int lengthSamples)
        {
            _channelCount = channelCount;
            _leftChannel = new float[lengthSamples];
            if (channelCount > 1)
            {
                _rightChannel = new float[lengthSamples];
            }

            _frameOutputNode = CreateFrameOutputNode(sampleRate, channelCount, graph);
        }

        private AudioFrameOutputNode CreateFrameOutputNode(uint sampleRate, uint channelCount, AudioGraph graph)
        {
            throw new System.NotImplementedException();
        }

        public IAudioNode Node => _frameOutputNode;
        
        public bool Stop()
        {
            throw new System.NotImplementedException();
        }
    }
}