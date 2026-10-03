using FluentAssertions;
using Soulstone.Utils;
using Xunit;

namespace Soulstone.Tests.Windows;

public class EquipmentPickerLayoutTests
{
    [Theory]
    [InlineData(1.0f)]
    [InlineData(1.5f)]
    [InlineData(2.0f)]
    public void Footer_LeavesRoomForCloseButtonAndSpacing(float scale)
    {
        var buttonHeight = 20.0f * scale + 8.0f * scale;

        var height = EquipmentPickerLayout.GetFooterHeight(20.0f * scale, 3.0f * scale, 4.0f * scale, scale);

        height.Should().BeGreaterThanOrEqualTo(buttonHeight + 8.0f * scale);
    }

    [Fact]
    public void Footer_AccommodatesLargeFontsAndCustomFramePadding()
    {
        var height = EquipmentPickerLayout.GetFooterHeight(36.0f, 12.0f, 8.0f, 1.0f);

        height.Should().BeGreaterThanOrEqualTo(60.0f + 16.0f);
    }
}
