using SimpleSigh.Text;
using Xunit;

namespace SimpleSigh.Text.Tests;

public class SignTextRulesTests
{
    private const int BasicLimit = 64;

    private static bool Normalize(string input, bool multiline, out string normalized, out string errorKey)
    {
        return SignTextRules.TryNormalize(input, BasicLimit, multiline, out normalized, out errorKey);
    }

    [Fact]
    public void EmptyTextIsAccepted()
    {
        Assert.True(Normalize("", false, out string normalized, out string errorKey));
        Assert.Equal("", normalized);
        Assert.Null(errorKey);
    }

    [Fact]
    public void AsciiAtLimitIsAccepted()
    {
        string text = new string('A', 64);
        Assert.True(Normalize(text, false, out string normalized, out _));
        Assert.Equal(text, normalized);
    }

    [Fact]
    public void AsciiOverLimitIsRejectedNotTruncated()
    {
        string text = new string('A', 65);
        Assert.False(Normalize(text, false, out string normalized, out string errorKey));
        Assert.Null(normalized);
        Assert.Equal(SignTextRules.TooLongKey, errorKey);
    }

    [Fact]
    public void JapaneseCountsOneElementPerCharacter()
    {
        string text = string.Concat(System.Linq.Enumerable.Repeat("研究室倉庫", 13)).Substring(0, 64);
        Assert.True(Normalize(text, false, out string normalized, out _));
        Assert.Equal(text, normalized);
        Assert.False(Normalize(text + "庫", false, out _, out _));
    }

    [Fact]
    public void SurrogatePairCountsAsOneElement()
    {
        // U+1F600 is two UTF-16 code units but one text element.
        string text = new string('A', 63) + "\U0001F600";
        Assert.Equal(65, text.Length);
        Assert.Equal(64, SignTextRules.CountElements(text));
        Assert.True(Normalize(text, false, out string normalized, out _));
        Assert.Equal(text, normalized);
    }

    [Fact]
    public void CombiningSequenceCountsAsOneElement()
    {
        // "か" + combining dakuten (U+3099) renders as one grapheme.
        Assert.Equal(1, SignTextRules.CountElements("が"));
    }

    [Fact]
    public void NewlineRejectedWhenSingleLine()
    {
        Assert.False(Normalize("倉庫\r\n備品", false, out string normalized, out string errorKey));
        Assert.Null(normalized);
        Assert.Equal(SignTextRules.NewlineNotAllowedKey, errorKey);
    }

    [Fact]
    public void LoneCarriageReturnAlsoRejectedWhenSingleLine()
    {
        Assert.False(Normalize("a\rb", false, out _, out string errorKey));
        Assert.Equal(SignTextRules.NewlineNotAllowedKey, errorKey);
    }

    [Fact]
    public void NewlinesNormalizedToLfWhenMultiline()
    {
        Assert.True(Normalize("倉庫\r\n備品\rA\nB", true, out string normalized, out _));
        Assert.Equal("倉庫\n備品\nA\nB", normalized);
    }

    [Fact]
    public void CrLfCountsAsOneElementAfterNormalization()
    {
        string text = new string('A', 31) + "\r\n" + new string('B', 32);
        Assert.True(Normalize(text, true, out string normalized, out _));
        Assert.Equal(64, SignTextRules.CountElements(normalized));
    }

    [Fact]
    public void LeadingAndTrailingWhitespaceIsPreserved()
    {
        Assert.True(Normalize("  Wood A-12  ", false, out string normalized, out _));
        Assert.Equal("  Wood A-12  ", normalized);
    }

    [Fact]
    public void NullIsRejectedWithStableKey()
    {
        Assert.False(Normalize(null, false, out string normalized, out string errorKey));
        Assert.Null(normalized);
        Assert.Equal("SimpleSigh_InvalidText", errorKey);
        Assert.Equal(SignTextRules.InvalidTextKey, errorKey);
    }

    [Fact]
    public void SymbolsAndFullWidthPunctuationAreAccepted()
    {
        string text = "発電設備 24h ※立入禁止！（Ａ－１２）#&@";
        Assert.True(Normalize(text, false, out string normalized, out _));
        Assert.Equal(text, normalized);
    }

    [Theory]
    [InlineData("a\u0000b")]
    [InlineData("a\u0007b")]
    [InlineData("a\tb")]
    [InlineData("a\uFFFEb")]
    public void XmlUnsafeCharactersAreRejected(string text)
    {
        // Saves are XML; these characters would corrupt the save file.
        Assert.False(Normalize(text, true, out string normalized, out string errorKey));
        Assert.Null(normalized);
        Assert.Equal(SignTextRules.InvalidTextKey, errorKey);
    }

    [Fact]
    public void LoneSurrogateIsRejected()
    {
        Assert.False(Normalize("a\uD800b", false, out _, out string errorKey));
        Assert.Equal(SignTextRules.InvalidTextKey, errorKey);
    }
}
