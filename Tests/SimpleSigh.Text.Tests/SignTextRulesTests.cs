using SimpleSigh.Text;
using Xunit;

namespace SimpleSigh.Tests;
public sealed class SignTextRulesTests
{
    [Fact] public void EmptyIsAllowed() => Assert.True(SignTextRules.TryNormalize("", 64, false, out var value, out _) && value == "");
    [Fact] public void SixtyFourJapaneseCharactersAllowed() => Assert.True(SignTextRules.TryNormalize(new string('倉',64),64,false,out _,out _));
    [Fact] public void SixtyFiveJapaneseCharactersRejected() => Assert.False(SignTextRules.TryNormalize(new string('倉',65),64,false,out _,out _));
    [Fact] public void SurrogatePairCountsAsOne() => Assert.True(SignTextRules.TryNormalize(new string('木',63)+"😀",64,false,out _,out _));
    [Fact] public void NewlineRejectedForSmallSign() => Assert.False(SignTextRules.TryNormalize("A\r\nB",64,false,out _,out _));
    [Fact] public void NewlineNormalizedForBoard() { Assert.True(SignTextRules.TryNormalize("A\r\nB\rC",256,true,out var result,out _)); Assert.Equal("A\nB\nC", result); }
    [Fact] public void LeadingAndTrailingWhitespacePreserved() { Assert.True(SignTextRules.TryNormalize(" A ",64,false,out var result,out _)); Assert.Equal(" A ",result); }
    [Fact] public void NullRejected() => Assert.False(SignTextRules.TryNormalize(null,64,false,out _,out _));
    [Fact] public void ControlCharacterRejected() => Assert.False(SignTextRules.TryNormalize("a\0b",64,false,out _,out _));
}
