using Windows.Storage;

namespace AudioGraphExtensions
{
    public class RunResult
    {
        public bool Success { get; }
        public IStorageFile OutputFile { get; }
        public float[] Left { get; }
        public float[] Right { get; }

        public RunResult(bool success)
        {
            Success = success;
        }
        
        public RunResult(bool success, float[] left, float[] right) : this(success)
        {
            Left = left;
            Right = right;
        }
        
        public RunResult(bool success, IStorageFile outputFile) : this(success)
        {
            OutputFile = outputFile;
        }
    }
}