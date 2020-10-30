
using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using AudioGraphExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Storage;

namespace UnitTestProjectMsTest
{
    [TestClass]
    public class IntegrationTests
    {
        private static uint _sampleRate;
        private static StorageFolder _storageFolder;
        private static float[] _square;
        private static float[] _saw;

        [AssemblyInitialize]
        public static async Task AssemblyInit(TestContext context)
        {
            const int quantumPerSecond = 100;
            var defaultQuantumSize = await AudioSystem.GetDefaultQuantumSizeAsync();
            _sampleRate = quantumPerSecond * (uint)defaultQuantumSize;

            _storageFolder = ApplicationData.Current.LocalFolder;

            var oneSecondLong = (int)_sampleRate;
            _square = GetSquare(oneSecondLong, 4);
            _saw = GetSaw(oneSecondLong, 0.25f);
        }

        [TestMethod]
        public async Task UsingBuilder_AudioSystem_SquareArrayToMono()
        {
            var outputFile = await _storageFolder.CreateFileAsync(
                "square-array-to-mono.wav",
                CreationCollisionOption.ReplaceExisting);

            var builder = AudioSystem.Builder();
            builder.SampleRate(_sampleRate);
            builder.From(_square).To(outputFile);

            var audioSystem = await builder.BuildAsync();
            var result = await audioSystem.RunAsync();

            Assert.AreEqual(true, result.Success, result.OutputFile.Path);
        }

        [TestMethod]
        public async Task UsingBuilder_AudioSystem_SawArrayToMono()
        {
            var outputFile = await _storageFolder.CreateFileAsync(
                "saw-array-to-mono.wav",
                CreationCollisionOption.ReplaceExisting);

            var builder = AudioSystem.Builder();
            builder.SampleRate(_sampleRate);
            builder.From(_saw).To(outputFile);

            var audioSystem = await builder.BuildAsync();
            var result = await audioSystem.RunAsync();

            Assert.AreEqual(true, result.Success, result.OutputFile.Path);
        }

        [TestMethod]
        public async Task UsingBuilder_AudioSystem_SawArrayToStereo()
        {
            var outputFile = await _storageFolder.CreateFileAsync(
                "saw-array-to-stereo.wav",
                CreationCollisionOption.ReplaceExisting);

            var builder = AudioSystem.Builder();
            builder.SampleRate(_sampleRate);
            builder.From(_saw, _saw).To(outputFile);

            var audioSystem = await builder.BuildAsync();
            var result = await audioSystem.RunAsync();

            Assert.AreEqual(true, result.Success, result.OutputFile.Path);
        }

        [TestMethod]
        public async Task UsingBuilder_AudioSystem_SawMonoToArray()
        {
            var inputFile = await StorageFile.GetFileFromPathAsync(
                Path.Combine(_storageFolder.Path, "saw-array-to-mono.wav"));

            var builder = AudioSystem.Builder();
            builder.From(inputFile);

            var audioSystem = await builder.BuildAsync();
            var result = await audioSystem.RunAsync();

            Assert.AreEqual(true, result.Success);
            Assert.AreEqual(_saw.Length, result.Left.Length);
        }

        [TestMethod]
        public async Task UsingBuilder_AudioSystem_SawArrayToArray()
        {
            var builder = AudioSystem.Builder();
            builder.SampleRate(_sampleRate);
            builder.From(_saw);

            var audioSystem = await builder.BuildAsync();
            var result = await audioSystem.RunAsync();

            Assert.AreEqual(true, result.Success);
        }

        [TestMethod]
        public async Task UsingBuilder_AudioSystem_MonoToMono()
        {
            var inputFile = await StorageFile.GetFileFromPathAsync(
                Path.Combine(_storageFolder.Path, "saw-array-to-mono.wav"));

            var outputFile = await _storageFolder.CreateFileAsync(
                "saw-array-to-mono-to-mono.wav",
                CreationCollisionOption.ReplaceExisting);

            var builder = AudioSystem.Builder();
            builder.From(inputFile).To(outputFile);

            var audioSystem = await builder.BuildAsync();
            var result = await audioSystem.RunAsync();

            Assert.AreEqual(true, result.Success);
        }

        [TestMethod]
        public async Task UsingBuilder_AudioSystem_Stress()
        {
            const int lengthSeconds = 10;
            var longSaw = GetSaw(lengthSeconds * (int)_sampleRate, 0.25f);

            const int executeTimes = 10;
            for (var count = 0; count < executeTimes; count++)
            {
                // Write
                var outputFile = await _storageFolder.CreateFileAsync(
                    "stress.wav",
                    CreationCollisionOption.ReplaceExisting);

                var builderWrite = AudioSystem.Builder();
                builderWrite.SampleRate(_sampleRate);
                builderWrite.From(longSaw).To(outputFile);

                var audioSystemWrite = await builderWrite.BuildAsync();
                var resultWrite = await audioSystemWrite.RunAsync();
                audioSystemWrite.Dispose();

                Assert.AreEqual(true, resultWrite.Success, "Error while writing");

                // Read
                var inputFile = await StorageFile.GetFileFromPathAsync(
                Path.Combine(_storageFolder.Path, "stress.wav"));

                var builderRead = AudioSystem.Builder();
                builderRead.From(inputFile);

                var audioSystemRead = await builderRead.BuildAsync();
                var resultRead = await audioSystemRead.RunAsync();
                audioSystemRead.Dispose();

                Assert.AreEqual(true, resultRead.Success, "Error while reading");
                Assert.AreEqual(longSaw.Length, resultRead.Left.Length, "Successful runs: " + count);
                CollectionAssert.AreEqual(longSaw, resultRead.Left, new SampleComparer(0.0001f), "Successful runs: " + count);
            }
        }


        private class SampleComparer : IComparer
        {
            private readonly float _tolerance;

            public SampleComparer(float tolerance)
            {
                _tolerance = tolerance;
            }

            public int Compare(object x, object y)
            {
                return Math.Abs((float)x - (float)y) <= _tolerance ? 0 : 1;
            }
        }

        private static float[] GetSquare(int arrayLength, int halfPeriod)
        {
            var squareSamples = new float[arrayLength];
            var high = true;
            var currentWidth = 0;
            for (var index = 0; index < squareSamples.Length; index++)
            {
                squareSamples[index] = high ? 1f : -1f;

                if (++currentWidth != halfPeriod) continue;
                
                currentWidth = 0;
                high = !high;
            }

            return squareSamples;
        }

        private static float[] GetSaw(int arrayLength, float step)
        {
            var sawSamples = new float[arrayLength];
            var current = 0f;
            for (var index = 0; index < sawSamples.Length; index++)
            {
                sawSamples[index] = current;
                current += step;

                if (current >= 1 || current <= -1) step = -step;
            }

            return sawSamples;
        }
    }
}
