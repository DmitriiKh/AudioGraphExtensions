namespace AudioGraphExtensions
{
    using Windows.Media.Audio;
    using Windows.Storage;

    public class AudioGraphOutputStream
    {
        private readonly IAudioNode _outputNode;
        private readonly AudioGraph _audioGraph;

        protected AudioGraphOutputStream(IAudioNode outputNode, AudioGraph audioGraph)
        {
            _outputNode = outputNode;
            _audioGraph = audioGraph;
        }

        public static AudioGraphOutputStream ToFile(StorageFile file)
        {
            return null;
        }
    }
}