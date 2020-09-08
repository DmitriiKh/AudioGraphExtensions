using System;
using Windows.Media.Audio;

namespace AudioGraphExtensions
{
    internal interface IAudioInput
    {
        IAudioInputNode Node { get; }
        
        double LengthInQuantum { get; }
        
        event EventHandler InputEnded;
    }
}