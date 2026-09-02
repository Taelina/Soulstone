using System;
using System.IO;
using FluentAssertions;
using Soulstone.Utils;
using Xunit;

namespace Soulstone.Tests.Utils
{
    [Collection("NonParallel")]
    public class ImageHelperTests : IDisposable
    {
        private readonly string tempDirectory;

        public ImageHelperTests()
        {
            TestHelper.EnsureMockServices();
            tempDirectory = Path.Combine(Path.GetTempPath(), "SoulstoneImgTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);
            Plugin.dataLocation = tempDirectory;
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, true);
                }
            }
            catch { }
        }

        [Fact]
        public void GetImagesDirectory_CreatesAndReturnsDirectory()
        {
            var dir = ImageHelper.GetImagesDirectory();

            Directory.Exists(dir).Should().BeTrue();
            dir.Should().StartWith(tempDirectory);
        }

        [Fact]
        public void GetCacheDirectory_CreatesAndReturnsCacheDirectory()
        {
            var dir = ImageHelper.GetCacheDirectory();

            Directory.Exists(dir).Should().BeTrue();
            dir.Should().Contain("cache");
        }

        [Fact]
        public void CopyImageToLocalFolder_CopiesFileToTargetDirectory()
        {
            // Create a fake image file
            var sourceFile = Path.Combine(tempDirectory, "test_item.png");
            File.WriteAllBytes(sourceFile, new byte[] { 1, 2, 3, 4, 5 });

            var copiedPath = ImageHelper.CopyImageToLocalFolder(sourceFile, "items");

            copiedPath.Should().NotBeNullOrWhiteSpace();
            File.Exists(copiedPath).Should().BeTrue();
            copiedPath.Should().Contain("items");
            copiedPath.Should().Contain("test_item");
            File.ReadAllBytes(copiedPath).Should().Equal(new byte[] { 1, 2, 3, 4, 5 });
        }

        [Fact]
        public void CopyImageToLocalFolder_WithNonExistentSource_ReturnsSource()
        {
            var nonExistent = Path.Combine(tempDirectory, "non_existent.png");
            var result = ImageHelper.CopyImageToLocalFolder(nonExistent, "items");

            result.Should().Be(nonExistent);
        }

        [Fact]
        public void ResolveImagePath_WithLocalExistingFile_ReturnsPath()
        {
            var localFile = Path.Combine(tempDirectory, "portrait.png");
            File.WriteAllText(localFile, "data");

            var resolved = ImageHelper.ResolveImagePath(localFile);
            resolved.Should().Be(localFile);
        }

        [Fact]
        public void ResolveImagePath_WithNullOrEmpty_ReturnsNull()
        {
            ImageHelper.ResolveImagePath(null).Should().BeNull();
            ImageHelper.ResolveImagePath("").Should().BeNull();
            ImageHelper.ResolveImagePath("   ").Should().BeNull();
        }
    }
}
