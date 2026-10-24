using System.Collections.Generic;
using SimpleSigh.Text;
using Xunit;

namespace SimpleSigh.Text.Tests;

public class SignEditRulesTests
{
    [Fact]
    public void SingleTargetKeepsOwnRules()
    {
        SignEditRules rules = SignEditRules.Combine(new[] { new SignEditRules(256, true) });
        Assert.Equal(256, rules.MaxElements);
        Assert.True(rules.Multiline);
    }

    [Fact]
    public void UsesStrictestRules()
    {
        // A notice board (256, multi-line) edited together with a basic sign (64, single-line):
        // the shared text must be valid for both.
        SignEditRules rules = SignEditRules.Combine(new[] { new SignEditRules(256, true), new SignEditRules(64, false), new SignEditRules(384, true) });
        Assert.Equal(64, rules.MaxElements);
        Assert.False(rules.Multiline);
    }

    [Fact]
    public void TextValidForCombinedRulesIsValidForEveryTarget()
    {
        var targets = new[] { new SignEditRules(256, true), new SignEditRules(64, false) };
        SignEditRules rules = SignEditRules.Combine(targets);
        string text = new string('あ', 64);
        Assert.True(SignTextRules.TryNormalize(text, rules.MaxElements, rules.Multiline, out string normalized, out _));
        foreach (SignEditRules target in targets)
        {
            Assert.True(SignTextRules.TryNormalize(normalized, target.MaxElements, target.Multiline, out _, out _));
        }
    }

    [Fact]
    public void EmptyTargetListIsRejected()
    {
        Assert.Throws<System.ArgumentException>(() => SignEditRules.Combine(new List<SignEditRules>()));
    }

    [Theory]
    [InlineData(false, "", true)]
    [InlineData(false, null, true)]
    [InlineData(false, "けんきゅう", false)]
    [InlineData(true, "", false)]
    [InlineData(true, "けんきゅう", false)]
    public void EnterConfirmsOnlySingleLineEditorsOutsideImeComposition(bool multiline, string composition, bool expected)
    {
        Assert.Equal(expected, SignEditRules.EnterConfirms(multiline, composition));
    }
}
