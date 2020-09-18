
using System;
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
        private static StorageFolder storageFolder;
        private static float[] square;
        private static float[] saw;

        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            storageFolder = ApplicationData.Current.LocalFolder;
            square = GetSquare(44100, 4);
            saw = GetSaw(44100, 0.25f);
        }

        [TestMethod]
        public async Task UsingBuilder_AudioSystem_SquareToMono()
        {
            var outputFile = await storageFolder.CreateFileAsync(
                "square44100-mono.wav",
                CreationCollisionOption.ReplaceExisting);

            var builder = AudioSystem.Builder();
            builder.SampleRate(44100).Channels(1);
            builder.From(square).To(outputFile);

            var audioSystem = await builder.BuildAsync();
            var result = await audioSystem.RunAsync();

            Assert.AreEqual(true, result.Success, result.OutputFile.Path);
        }

        [TestMethod]
        public async Task UsingBuilder_AudioSystem_SawToMono()
        {
            var outputFile = await storageFolder.CreateFileAsync(
                "saw44100-mono.wav",
                CreationCollisionOption.ReplaceExisting);

            var builder = AudioSystem.Builder();
            builder.SampleRate(44100).Channels(1);
            builder.From(saw).To(outputFile);

            var audioSystem = await builder.BuildAsync();
            var result = await audioSystem.RunAsync();

            Assert.AreEqual(true, result.Success, result.OutputFile.Path);
        }

        [TestMethod]
        public async Task UsingBuilder_AudioSystem_SawToStereo()
        {
            var outputFile = await storageFolder.CreateFileAsync(
                "saw44100-stereo.wav",
                CreationCollisionOption.ReplaceExisting);

            var builder = AudioSystem.Builder();
            builder.SampleRate(44100).Channels(2);
            builder.From(saw, saw).To(outputFile);

            var audioSystem = await builder.BuildAsync();
            var result = await audioSystem.RunAsync();

            Assert.AreEqual(true, result.Success, result.OutputFile.Path);
        }

        [TestMethod]
        public async Task UsingBuilder_AudioSystem_MonoToArray()
        {
            var inputFile = await StorageFile.GetFileFromPathAsync(
                Path.Combine(storageFolder.Path, "saw44100-mono.wav"));

            var builder = AudioSystem.Builder();
            builder.From(inputFile);

            var audioSystem = await builder.BuildAsync();
            var result = await audioSystem.RunAsync();

            Assert.AreEqual(true, result.Success);
            Assert.AreEqual(44100, result.Left.Length);
        }

        [TestMethod]
        public async Task UsingBuilder_AudioSystem_SawToArray()
        {
            var builder = AudioSystem.Builder();
            builder.SampleRate(44100).Channels(1);
            builder.From(saw);

            var audioSystem = await builder.BuildAsync();
            var result = await audioSystem.RunAsync();

            Assert.AreEqual(true, result.Success);
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
