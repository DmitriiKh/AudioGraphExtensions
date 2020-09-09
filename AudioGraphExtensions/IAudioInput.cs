using System;
using Windows.Media.Audio;

namespace AudioGraphExtensions
{
    internal interface IAudioInput
    {
        IAudioInputNode Node { get; }
        
        int LengthInQuantum { get; }
        
        int LengthInSamples { get; }

        event EventHandler InputEnded;
    }
}