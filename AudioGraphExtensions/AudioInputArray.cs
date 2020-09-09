using System;
using Windows.Media;
using Windows.Media.Audio;

namespace AudioGraphExtensions
{
    internal sealed class AudioInputArray : IAudioInput
    {
        private readonly AudioFrameInputNode _frameInputNode;
        private readonly uint _channelCount;
        private readonly float[] _leftChannel;
        private readonly float[] _rightChannel;
        private int _audioCurrentPosition;

        public AudioInputArray(
            AudioGraph graph,
            uint sampleRate,
            uint channelCount,
            float[] left,
            float[] right = null)
        {
            _channelCount = channelCount;
            _leftChannel = left;
            _rightChannel = right;

            LengthInQuantum = (double) _leftChannel.Length / graph.SamplesPerQuantum;

            _frameInputNode = CreateAudioFrameInputNode(sampleRate, channelCount, graph);
            _frameInputNode.QuantumStarted += FrameInputNode_QuantumStarted;
        }

        public event EventHandler InputEnded;

        public IAudioInputNode Node => _frameInputNode;

        public double LengthInQuantum { get; }

        private static AudioFrameInputNode CreateAudioFrameInputNode(
            uint sampleRate,
            uint channelCount,
            AudioGraph graph)
        {
            var frameInputNodeProperties = graph.EncodingProperties;

            frameInputNodeProperties.SampleRate = sampleRate;
            frameInputNodeProperties.ChannelCount = channelCount;

            var frameInputNode = graph.CreateFrameInputNode(frameInputNodeProperties);

            return frameInputNode;
        }

        private void FrameInputNode_QuantumStarted(
            AudioFrameInputNode sender,
            FrameInputNodeQuantumStartedEventArgs args)
        {
            var numSamplesNeeded = args.RequiredSamples;

            if (numSamplesNeeded == 0)
                return;

            var (frame, finished) = GenerateFrame(numSamplesNeeded);

            sender.AddFrame(frame);

            if (finished) OnInputEnd();
        }

        private void OnInputEnd()
        {
            InputEnded?.Invoke(this, EventArgs.Empty);
        }

        private unsafe (AudioFrame frame, bool finished)
            GenerateFrame(int requiredSamples)
        {
            var bufferSize = (uint) (requiredSamples * sizeof(float) * _channelCount);

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

                for (uint index = 0; index < capacityInFloat; index += _channelCount)
                {
                    if (_audioCurrentPosition >= _leftChannel.Length)
                        // last frame can be not full
                        return (frame, true);

                    dataInFloat[index] = _leftChannel[_audioCurrentPosition];

                    // if output is stereo
                    if (_channelCount == 2 && _rightChannel != null)
                        dataInFloat[index + 1] = _rightChannel[_audioCurrentPosition];

                    _audioCurrentPosition++;
                }
            }

            return (frame, false);
        }
    }
}