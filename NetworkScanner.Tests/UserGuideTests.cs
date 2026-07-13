using System.Linq;
using Xunit;

namespace NetworkScanner.Tests
{
    public class UserGuideTests
    {
        [Fact]
        public void Languages_ContainsAllFour_EnglishFirstAsDefault()
        {
            var codes = UserGuide.Languages.Select(l => l.Code).ToArray();
            Assert.Equal(new[] { "en", "ko", "ja", "zh" }, codes);
        }

        [Fact]
        public void GetSections_AllLanguages_HaveSameStructureAndNoEmptyText()
        {
            int expected = UserGuide.GetSections("en").Count;
            Assert.True(expected > 0);

            foreach (var (code, _) in UserGuide.Languages)
            {
                var sections = UserGuide.GetSections(code);
                Assert.Equal(expected, sections.Count); // 언어별로 섹션 수가 같아야 번역 누락이 없다
                Assert.All(sections, s =>
                {
                    Assert.False(string.IsNullOrWhiteSpace(s.Heading));
                    Assert.False(string.IsNullOrWhiteSpace(s.Body));
                });
            }
        }

        [Fact]
        public void GetSections_UnknownLanguage_FallsBackToEnglish()
        {
            Assert.Equal(UserGuide.GetSections("en"), UserGuide.GetSections("fr"));
        }
    }
}
