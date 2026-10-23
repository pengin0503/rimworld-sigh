using System.Collections.Generic;
using System.Text;
using Xunit;

namespace SimpleSigh.Text.Tests;

/// <summary>
/// Guards issue #4: every character a Japanese player is likely to type on a sign must exist in the
/// bundled font, otherwise it is drawn as "?". JIS X 0208 is the standard everyday Japanese repertoire
/// (levels 1 and 2 hold 6,355 kanji); the characters are enumerated through the EUC-JP code page so the
/// test needs no hand-written list.
/// </summary>
public class BundledFontCoverageTests
{
    static BundledFontCoverageTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private static List<char> JisX0208(int firstRow, int lastRow)
    {
        Encoding eucJp = Encoding.GetEncoding("euc-jp", EncoderFallback.ExceptionFallback, DecoderFallback.ReplacementFallback);
        var chars = new List<char>();
        for (int row = firstRow; row <= lastRow; row++)
        {
            for (int cell = 1; cell <= 94; cell++)
            {
                string decoded = eucJp.GetString(new[] { (byte)(0xA0 + row), (byte)(0xA0 + cell) });
                if (decoded.Length == 1 && decoded[0] != '�' && decoded[0] != '?')
                {
                    chars.Add(decoded[0]);
                }
            }
        }
        return chars;
    }

    private static List<char> Missing(IEnumerable<char> chars)
    {
        var missing = new List<char>();
        foreach (char c in chars)
        {
            if (TrueTypeFontTests.SignFont.GetGlyphIndex(c) == 0)
            {
                missing.Add(c);
            }
        }
        return missing;
    }

    [Fact]
    public void CoverageCheckDetectsAMissingCharacter()
    {
        // Private-use code point: no font maps it, so the check below can fail.
        Assert.Equal(new List<char> { '' }, Missing(new[] { 'あ', '' }));
    }

    [Fact]
    public void CoversJisLevel1And2Kanji()
    {
        List<char> level1 = JisX0208(16, 47);
        List<char> level2 = JisX0208(48, 84);
        Assert.Equal(2965, level1.Count);
        Assert.Equal(3390, level2.Count);
        Assert.Empty(Missing(level1));
        Assert.Empty(Missing(level2));
    }

    [Fact]
    public void CoversJisSymbolsKanaAndFullWidthAlphanumerics()
    {
        // Rows 1–8: punctuation, symbols, full-width digits/letters, hiragana, katakana, Greek, Cyrillic, box drawing.
        List<char> nonKanji = JisX0208(1, 8);
        Assert.True(nonKanji.Count > 500, nonKanji.Count.ToString());
        Assert.Empty(Missing(nonKanji));
    }

    [Fact]
    public void CoversHalfWidthKatakanaAndAscii()
    {
        var chars = new List<char>();
        for (char c = ' '; c <= '~'; c++)
        {
            chars.Add(c);
        }
        for (char c = '｡'; c <= 'ﾟ'; c++)
        {
            chars.Add(c);
        }
        Assert.Empty(Missing(chars));
    }
}
