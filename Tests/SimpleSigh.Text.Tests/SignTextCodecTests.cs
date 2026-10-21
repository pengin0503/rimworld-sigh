using SimpleSigh.Text;
using Xunit;

namespace SimpleSigh.Text.Tests;

/// <summary>
/// RimWorld 1.7 loads saved strings through ParseHelper.ParseString, which turns the two
/// characters "\n" into a newline, and through an XmlDocument that drops whitespace-only
/// text. The codec must make every valid sign text survive that path unchanged.
/// </summary>
public class SignTextCodecTests
{
    // Mirrors Verse.ParseHelper.ParseString in the 1.7 reference assembly.
    private static string RimWorldParseString(string str) => str.Replace("\\n", "\n");

    [Theory]
    [InlineData("")]
    [InlineData("研究室")]
    [InlineData("C:\\new\\folder")]
    [InlineData("   ")]
    [InlineData("  Wood A-12  ")]
    [InlineData("倉庫\n備品")]
    [InlineData("100% \\n literal")]
    [InlineData("|pipes|")]
    [InlineData("%0A%25")]
    [InlineData("𠮷野家 \U0001F600")]
    public void RoundTripsThroughRimWorldLoadPath(string text)
    {
        string encoded = SignTextCodec.Encode(text);
        Assert.Equal(text, SignTextCodec.Decode(RimWorldParseString(encoded)));
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("a\nb")]
    [InlineData("C:\\new")]
    public void EncodedFormHasNoWhitespaceEdgesNewlinesOrBackslashes(string text)
    {
        string encoded = SignTextCodec.Encode(text);
        Assert.DoesNotContain("\n", encoded);
        Assert.DoesNotContain("\\", encoded);
        Assert.False(char.IsWhiteSpace(encoded[0]));
        Assert.False(char.IsWhiteSpace(encoded[encoded.Length - 1]));
    }

    [Fact]
    public void JapaneseStaysReadableInSaveFile()
    {
        Assert.Contains("研究室", SignTextCodec.Encode("研究室"));
    }

    [Fact]
    public void NullDecodesToEmpty()
    {
        Assert.Equal("", SignTextCodec.Decode(null));
    }

    [Fact]
    public void UnwrappedValueIsReturnedAsIs()
    {
        // Hand-edited saves without the wrapper still load.
        Assert.Equal("plain", SignTextCodec.Decode("plain"));
    }
}
