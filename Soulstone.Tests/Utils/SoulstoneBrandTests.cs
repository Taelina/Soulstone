using FluentAssertions;
using Soulstone.Utils;
using Xunit;

namespace Soulstone.Tests.Utils;

public class SoulstoneBrandTests
{
    [Fact]
    public void IconIsBundledInPluginAssembly_AsAValidPng()
    {
        using var stream = typeof(Plugin).Assembly.GetManifestResourceStream(SoulstoneBrand.IconResourceName);

        stream.Should().NotBeNull("the installed plugin must carry its icon without an external Assets folder");
        stream!.Length.Should().BeGreaterThan(8);
        var signature = new byte[8];
        stream.ReadExactly(signature);
        signature.Should().Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
    }
}
