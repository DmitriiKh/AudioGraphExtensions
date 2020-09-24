
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Threading.Tasks;
using AudioGraphExtensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Storage;

namespace UnitTestProjectMsTest
{
    [TestClass]
    public class IntegrationTests
    {
        private static uint sampleRate;
        private static StorageFolder storageFolder;
        private static float[] square;
        private static float[] saw;

        [AssemblyInitialize]
        public static async Task AssemblyInit(TestContext context)
        {
            const int quantumsPerSecond = 100;
            var defaultQuantumSize = await AudioSystem.GetDefaultQuantumSizeAsync();
            sampleRate = quantumsPerSecond * (uint)defaultQuantumSize;

            storageFolder = ApplicationData.Current.LocalFolder;

            var oneSecondLong = (int)sampleRate;
            square = GetSquare(oneSecondLong, 4);
            saw = GetSaw(oneSecondLong, 0.25f);
        }

        [TestMethod]
        public async Task UsingBuilder_AudioSystem_SquareArrayToMono()
        {
            var outputFile = await storageFolder.CreateFileAsync(
                "square-array-to-mono.wav",
                CreationCollisionOption.ReplaceExisting);

            var builder = AudioSystem.Builder();
            builder.SampleRate(sampleRate);
            builder.From(square).To(outputFile);

            var audioSystem = await builder.BuildAsync();
            var result = await audioSystem.RunAsync();

            Assert.AreEqual(true, result.Success, result.OutputFile.Path);
        }

        [TestMethod]
        public async Task UsingBuilder_AudioSystem_SawArrayToMono()
        {
            var outputFile = await storageFolder.CreateFileAsync(
                "saw-array-to-mono.wav",
                CreationCollisionOption.ReplaceExisting);

            var builder = AudioSystem.Builder();
            builder.SampleRate(sampleRate);
            builder.From(saw).To(outputFile);

            var audioSystem = await builder.BuildAsync();
            var result = await audioSystem.RunAsync();

            Assert.AreEqual(true, result.Success, result.OutputFile.Path);
        }

        [TestMethod]
        public async Task UsingBuilder_AudioSystem_SawArrayToStereo()
        {
            var outputFile = await storageFolder.CreateFileAsync(
                "saw-array-to-stereo.wav",
                CreationCollisionOption.ReplaceExisting);

            var builder = AudioSystem.Builder();
            builder.SampleRate(sampleRate);
            builder.From(saw, saw).To(outputFile);

            var audioSystem = await builder.BuildAsync();
            var result = await audioSystem.RunAsync();

            Assert.AreEqual(true, result.Success, result.OutputFile.Path);
        }

        [TestMethod]
        public async Task UsingBuilder_AudioSystem_SawMonoToArray()
        {
            var inputFile = await StorageFile.GetFileFromPathAsync(
                Path.Combine(storageFolder.Path, "saw-array-to-mono.wav"));

            var builder = AudioSystem.Builder();
            builder.From(inputFile);

            var audioSystem = await builder.BuildAsync();
            var result = await audioSystem.RunAsync();

            Assert.AreEqual(true, result.Success);
            Assert.AreEqual(saw.Length, result.Left.Length);
        }

        [TestMethod]
        public async Task UsingBuilder_AudioSystem_SawArrayToArray()
        {
            var builder = AudioSystem.Builder();
            builder.SampleRate(sampleRate);
            builder.From(saw);

            var audioSystem = await builder.BuildAsync();
            var result = await audioSystem.RunAsync();

            Assert.AreEqual(true, result.Success);
        }

        [TestMethod]
        public async Task UsingBuilder_AudioSystem_MonoToMono()
        {
            var inputFile = await StorageFile.GetFileFromPathAsync(
                Path.Combine(storageFolder.Path, "saw-array-to-mono.wav"));

            var outputFile = await storageFolder.CreateFileAsync(
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
            var lengthSeconds = 10;
            var longSaw = GetSaw(lengthSeconds * (int)sampleRate, 0.25f);

            const int executeTimes = 10;
            for (var count = 0; count < executeTimes; count++)
            {
                // Write
                var outputFile = await storageFolder.CreateFileAsync(
                    "stress.wav",
                    CreationCollisionOption.ReplaceExisting);

                var builderWrite = AudioSystem.Builder();
                builderWrite.SampleRate(sampleRate);
                builderWrite.From(longSaw).To(outputFile);

                var audioSystemWrite = await builderWrite.BuildAsync();
                var resultWrite = await audioSystemWrite.RunAsync();
                audioSystemWrite.Dispose();

                Assert.AreEqual(true, resultWrite.Success, "Error while writing");

                // Read
                var inputFile = await StorageFile.GetFileFromPathAsync(
                Path.Combine(storageFolder.Path, "stress.wav"));

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
        

        class SampleComparer : IComparer
        {
            private readonly float tolerance;

            public SampleComparer(float tolerance)
            {
                this.tolerance = tolerance;
            }

            public int Compare(object x, object y)
            {
                if (Math.Abs((float)x - (float)y) <= tolerance)
                    return 0;
                else 
                    return 1;
            }
        }

        private static float[] GetSquare(int arrayLength, int halfPeriod)
        {
            var square = new float[arrayLength];
            var high = true;
            var currentWidth = 0;
            for (var index = 0; index < square.Length; index++)
            {
                square[index] = high ? 1f : -1f;
                if (++currentWidth == halfPeriod)
                {
                    currentWidth = 0;
                    high = !high;
                }
            }

            return square;
        }

        private static float[] GetSaw(int arrayLength, float step)
        {
            var saw = new float[arrayLength];
            var current = 0f;
            for (var index = 0; index < saw.Length; index++)
            {
                saw[index] = current;
                current += step;

                if (current >= 1 || current <= -1) step = -step;
            }

            return saw;
        }
    }
}
