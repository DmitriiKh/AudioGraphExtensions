using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace AudioGraphExtensions
{
    public class AudioSystemBuilder
    {
        private readonly AudioSystem _audioSystem;
        private readonly Task _initialization;

        internal AudioSystemBuilder(
            uint sampleRate,
            uint channelCount,
            IProgress<double> progress = null,
            IProgress<string> status = null)
        {
            _audioSystem = new AudioSystem(sampleRate, channelCount, progress, status);
            _initialization = _audioSystem.InitAsync();
        }

        public async Task<AudioSystemBuilder> From(float[] left, float[] right = null)
        {
            await _initialization;
            _audioSystem.SetInput(left, right);
            return this;
        }

        public async Task<AudioSystemBuilder> From(StorageFile file)
        {
            await _initialization;
            await _audioSystem.SetInput(file);
            return this;
        }

        public async Task<AudioSystemBuilder> To(StorageFile file)
        {
            await _initialization;
            await _audioSystem.SetOutputAsync(file);
            return this;
        }
        
        public async Task<AudioSystemBuilder> To(float[] left, float[] right = null)
        {
            await _initialization;
            _audioSystem.SetOutput(left, right);
            return this;
        }
        
        public async Task<AudioSystemBuilder> ToArray()
        {
            var left = new float[_audioSystem.InputLength];
            var right = _audioSystem.IsStereo ? new float[_audioSystem.InputLength] : null;
            await To(left, right);
            return this;
        }

        public AudioSystem Build()
        {
            return _audioSystem;
        }
    }

    public static class AudioSystemBuilderExtension
    {
        public static async Task<AudioSystemBuilder> From(this Task<AudioSystemBuilder> antecedent, float[] left,
            float[] right = null)
        {
            var builder = await antecedent;
            await builder.From(left, right);
            return builder;
        }
        
        public static async Task<AudioSystemBuilder> From(this Task<AudioSystemBuilder> antecedent, StorageFile file)
        {
            var builder = await antecedent;
            await builder.From(file);
            return builder;
        }

        public static async Task<AudioSystemBuilder> To(this Task<AudioSystemBuilder> antecedent, StorageFile file)
        {
            var builder = await antecedent;
            await builder.To(file);
            return builder;
        }
        
        public static async Task<AudioSystemBuilder> To(this Task<AudioSystemBuilder> antecedent, float[] left,
            float[] right = null)
        {
            var builder = await antecedent;
            await builder.To(left, right);
            return builder;
        }
        
        public static async Task<AudioSystemBuilder>  ToArray(this Task<AudioSystemBuilder> antecedent)
        {
            var builder = await antecedent;
            await builder.ToArray();
            return builder;
        }

        public static async Task<AudioSystem> BuildAsync(this Task<AudioSystemBuilder> antecedent)
        {
            var builder = await antecedent;
            return builder.Build();
        }
    }
}