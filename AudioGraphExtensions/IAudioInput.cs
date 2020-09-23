using System;
using Windows.Media.Audio;

namespace AudioGraphExtensions
{
    internal interface IAudioInput
    {
        IAudioInputNode Node { get; }
        
        uint LengthInQuantum { get; }
        
        uint LengthInSamples { get; }
    }
}