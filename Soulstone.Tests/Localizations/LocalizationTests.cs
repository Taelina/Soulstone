using System.Collections.Generic;
using FluentAssertions;
using Xunit;
using Soulstone.Localizations;

namespace Soulstone.Tests.Localizations
{
    public class LocalizationTests
    {
        [Theory]
        [InlineData(Language.Français, 0)]
        [InlineData(Language.English, 1)]
        public void LanguageEnum_MatchesExpectedIntegerValues(Language language, int expectedValue)
        {
            ((int)language).Should().Be(expectedValue);
        }

        [Fact]
        public void Localization_Properties_SetAndGetCorrectly()
        {
            // Arrange
            var localization = new Localization();
            var dict = new Dictionary<string, string>
            {
                { "TestKey", "TestValue" }
            };

            // Act
            localization.Language = Language.English;
            localization.LocalizedStrings = dict;

            // Assert
            localization.Language.Should().Be(Language.English);
            localization.LocalizedStrings.Should().BeSameAs(dict);
            localization.LocalizedStrings.Should().ContainKey("TestKey").WhoseValue.Should().Be("TestValue");
        }
    }
}
