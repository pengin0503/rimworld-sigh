using SimpleSigh.Text;
using Xunit;

namespace SimpleSigh.Text.Tests;

public class SignSearchTests
{
    [Theory]
    [InlineData("研究室", "")]
    [InlineData("研究室", "   ")]
    [InlineData("食料保管庫", "保管")]
    [InlineData("Food Storage", "storage")]
    [InlineData("ＡＢＣ倉庫", "abc")]            // full-width letters
    [InlineData("ABC倉庫", "ＡＢＣ")]
    [InlineData("ｿｳｺ", "ソウコ")]                // half-width katakana
    [InlineData("そうこ", "ソウコ")]              // hiragana vs katakana
    [InlineData("ソウコ", "そうこ")]
    [InlineData("倉庫\n備品", "倉庫 備品")]        // line break counts as a space
    [InlineData("Wood A-12", "a-12")]
    public void Matches(string text, string query)
    {
        Assert.True(SignSearch.Matches(text, query));
    }

    [Theory]
    [InlineData("研究室", "倉庫")]
    [InlineData("Food", "storage")]
    [InlineData("", "a")]
    [InlineData(null, "a")]
    public void DoesNotMatch(string text, string query)
    {
        Assert.False(SignSearch.Matches(text, query));
    }

    [Fact]
    public void FirstLineIsAShortSummary()
    {
        Assert.Equal("倉庫", SignSearch.FirstLine("倉庫\n備品"));
        Assert.Equal("", SignSearch.FirstLine(null));
        Assert.Equal(new string('あ', 40) + "…", SignSearch.FirstLine(new string('あ', 50), 40));
    }
}
