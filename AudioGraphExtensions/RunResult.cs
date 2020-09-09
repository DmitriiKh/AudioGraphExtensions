using Windows.Storage;

namespace AudioGraphExtensions
{
    public class RunResult
    {
        public bool Success { get; }
        public IStorageFile OutputFile { get; }
        public float[] Left { get; }
        public float[] Right { get; }
        public uint SampleRate { get; }

        public RunResult(bool success, uint sampleRate)
        {
            Success = success;
            SampleRate = sampleRate;
        }
        
        public RunResult(bool success, uint sampleRate, float[] left, float[] right) : this(success, sampleRate)
        {
            Left = left;
            Right = right;
        }
        
        public RunResult(bool success, uint sampleRate, IStorageFile outputFile) : this(success, sampleRate)
        {
            OutputFile = outputFile;
        }
    }
}