﻿using Windows.Media.Audio;

namespace AudioGraphExtensions
{
    internal interface IAudioOutput
    {
        public IAudioNode Node { get; }
        
        bool Stop();
    }
}