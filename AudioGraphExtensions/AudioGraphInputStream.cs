namespace AudioGraphExtensions
{
    using Windows.Media.Audio;
    using Windows.Storage;
    
    public class AudioGraphInputStream
    {
        private readonly IAudioInputNode _inputNode;
        private readonly AudioGraph _audioGraph;

        protected AudioGraphInputStream(IAudioInputNode inputNode, AudioGraph audioGraph)
        {
            _inputNode = inputNode;
            _audioGraph = audioGraph;
        }

        public static AudioGraphInputStream FromFile(StorageFile file)
        {
            return null;
        }
    }
}