using System.Threading.Tasks;
using Windows.Media.Audio;

namespace AudioGraphExtensions
{
    internal interface IAudioOutput
    {
        public IAudioNode Node { get; }
        
        public Task<RunResult> Finalize();
    }
}