namespace AudioGraphExtensions
{
    using System;
    using Windows.Media.Audio;
    using Windows.Storage;

    public class AudioGraphOutputStream
    {
        private readonly IAudioNode _outputNode;
        private readonly AudioGraph _audioGraph;
        private readonly Progress<double> _progress;
        private readonly IProgress<string> _status;

        protected AudioGraphOutputStream(
            IAudioNode outputNode,
            AudioGraph audioGraph,
            Progress<double> progress,
            IProgress<string> status)
        {
            _outputNode = outputNode;
            _audioGraph = audioGraph;
            _progress = progress;
            _status = status;
        }

        public static AudioGraphOutputStream ToFile(
            StorageFile file,
            Progress<double> progress,
            IProgress<string> status)
        {
            return null;
        }
    }
}