using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace AudioGraphExtensions
{
    public class AudioSystemBuilder
    {
        private AudioSystem _audioSystem;
        private uint _sampleRate;
        private uint _channelCount;
        private IProgress<double> _progress;
        private IProgress<string> _status;
        private IData _inputData;
        private IData _outputData;

        internal AudioSystemBuilder()
        {
        }

        public AudioSystemBuilder SampleRate(uint sampleRate)
        {
            _sampleRate = sampleRate;
            return this;
        }

        public AudioSystemBuilder Channels(uint channelCount)
        {
            _channelCount = channelCount;
            return this;
        }
        
        public AudioSystemBuilder Report(IProgress<string> status, IProgress<double> progress)
        {
            _status = status;
            _progress = progress;
            return this;
        }

        public AudioSystemBuilder From(StorageFile file)
        {
            _inputData = new FileData(file);
            return this;
        }

        public AudioSystemBuilder From(float[] left, float[] right = null)
        {
            _inputData = new ArrayData(left, right);
            return this;
        }

        public AudioSystemBuilder To(StorageFile file)
        {
            _outputData = new FileData(file);
            return this;
        }

        public AudioSystemBuilder To(float[] left, float[] right = null)
        {
            _outputData = new ArrayData(left, right);
            return this;
        }

        public async Task<AudioSystem> BuildAsync()
        {
            await Init();

            await _inputData.ConnectAsInputAsync(_audioSystem);

            if (_outputData is null)
            {
                var left = new float[_audioSystem.InputLength];
                var right = _audioSystem.IsStereo ? new float[_audioSystem.InputLength] : null;
                _audioSystem.SetOutput(left, right);
            }
            else
            {
                await _outputData.ConnectAsOutputAsync(_audioSystem);
            }
            
            return _audioSystem;
        }

        private async Task Init()
        {
            _audioSystem = new AudioSystem(_sampleRate, _channelCount, _progress, _status);
            await _audioSystem.InitAsync();
        }

        private interface IData
        {
            Task ConnectAsInputAsync(AudioSystem system);
            Task ConnectAsOutputAsync(AudioSystem system);
        }

        private sealed class ArrayData : IData
        {
            private readonly float[] _left;
            private readonly float[] _right;

            public ArrayData(float[] left, float[] right)
            {
                _left = left;
                _right = right;
            }

            public async Task ConnectAsInputAsync(AudioSystem system) => 
                await Task.Run(() => system.SetInput(_left, _right));

            public async Task ConnectAsOutputAsync(AudioSystem system) => 
                await Task.Run(() => system.SetOutput(_left, _right));
        }
        
        private sealed class FileData : IData
        {
            private readonly StorageFile _file;

            public FileData(StorageFile file) => 
                _file = file;

            public async Task ConnectAsInputAsync(AudioSystem system) => 
                await system.SetInputAsync(_file);

            public async Task ConnectAsOutputAsync(AudioSystem system) => 
                await system.SetOutputAsync(_file);
        }
    }
}