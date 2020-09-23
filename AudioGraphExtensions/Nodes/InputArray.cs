using System;
using Windows.Media;
using Windows.Media.Audio;

namespace AudioGraphExtensions.Nodes
{
    internal sealed class InputArray : IAudioInput
    {
        private readonly AudioFrameInputNode _frameInputNode;
        private readonly float[] _leftChannel;
        private readonly float[] _rightChannel;
        private int _audioCurrentPosition;

        public InputArray(
            AudioGraph graph,
            uint sampleRate,
            uint channelCount,
            float[] left,
            float[] right = null)
        {
            _leftChannel = left;
            _rightChannel = right;

            LengthInSamples = (uint)left.Length;

            uint samplesPerQuantum = sampleRate / 100; // each quantum is 10ms
            LengthInQuantum = (uint)Math.Ceiling((double)left.Length / samplesPerQuantum);

            _frameInputNode = CreateAudioFrameInputNode(sampleRate, channelCount, graph);
            _frameInputNode.QuantumStarted += FrameInputNode_QuantumStarted;
        }

        public uint LengthInSamples { get; }

        public IAudioInputNode Node => _frameInputNode;

        public uint LengthInQuantum { get; }

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

            var frame = ArrayToFrame(numSamplesNeeded);

            sender.AddFrame(frame);
        }

        private unsafe AudioFrame ArrayToFrame(int requiredSamples)
        {
            var channelCount = _rightChannel is null ? 1u : 2u;

            var bufferSize = (uint) (requiredSamples * sizeof(float) * channelCount);

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

                for (uint index = 0; index < capacityInFloat; index += channelCount)
                {
                    if (_audioCurrentPosition >= _leftChannel.Length)
                    {
                        // fill the rest with zeros
                        for (var indexForZeros = index; indexForZeros < capacityInFloat; indexForZeros++)
                            dataInFloat[indexForZeros] = 0;

                        return frame;
                    }

                    dataInFloat[index] = _leftChannel[_audioCurrentPosition];

                    // if output is stereo
                    if (channelCount == 2 && _rightChannel != null)
                        dataInFloat[index + 1] = _rightChannel[_audioCurrentPosition];

                    _audioCurrentPosition++;
                }
            }

            return frame;
        }
    }
}