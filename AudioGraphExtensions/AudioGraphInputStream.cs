namespace AudioGraphExtensions
{
    using System;
    using Windows.Media.Audio;
    using Windows.Storage;
    
    public class AudioGraphInputStream
    {
        private readonly IAudioInputNode _inputNode;
        private readonly AudioGraph _audioGraph;
        private readonly Progress<double> _progress;
        private readonly IProgress<string> _status;

        protected AudioGraphInputStream(
            IAudioInputNode inputNode,
            AudioGraph audioGraph,
            Progress<double> progress,
            IProgress<string> status)
        {
            _inputNode = inputNode;
            _audioGraph = audioGraph;
            _progress = progress;
            _status = status;
        }

        public static AudioGraphInputStream FromFile(
            StorageFile file,
            Progress<double> progress,
            IProgress<string> status)
        {
            return null;
        }
    }
}