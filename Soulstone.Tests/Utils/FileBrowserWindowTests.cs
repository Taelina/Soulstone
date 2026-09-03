using System;
using FluentAssertions;
using Soulstone.Utils;
using Xunit;

namespace Soulstone.Tests.Utils
{
    [Collection("NonParallel")]
    public class FileBrowserWindowTests
    {
        public FileBrowserWindowTests()
        {
            TestHelper.EnsureMockServices();
        }

        [Fact]
        public void SetAllowedExtensions_WithImageFilters_CorrectlyParsesExtensions()
        {
            var browser = new ImGuiFileBrowserWindow("Test Browser");

            browser.SetAllowedExtensions(".png;.jpg;.jpeg;.bmp;.webp;.gif");

            browser.IsImageFilter.Should().BeTrue();
        }

        [Fact]
        public void SetAllowedExtensions_WithJsonFilter_SetsNotImageFilter()
        {
            var browser = new ImGuiFileBrowserWindow("Test Browser");

            browser.SetAllowedExtensions(".json");

            browser.IsImageFilter.Should().BeFalse();
        }

        [Fact]
        public void SetAllowedExtensions_WithWildcardOrEmpty_AllowsAll()
        {
            var browser = new ImGuiFileBrowserWindow("Test Browser");

            browser.SetAllowedExtensions("*.*");
            browser.IsImageFilter.Should().BeFalse();

            browser.SetAllowedExtensions(null);
            browser.IsImageFilter.Should().BeFalse();
        }

        [Fact]
        public void ConfirmUrl_SetsSelectedPathAndInvokesCallback()
        {
            var browser = new ImGuiFileBrowserWindow("Test Browser");
            string? receivedUrl = null;
            browser.OnFileSelected = (url) => receivedUrl = url;

            browser.ConfirmUrl("https://example.com/avatar.png");

            browser.Confirmed.Should().BeTrue();
            browser.SelectedPath.Should().Be("https://example.com/avatar.png");
            receivedUrl.Should().Be("https://example.com/avatar.png");
        }
    }
}
