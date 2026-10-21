using SimpleSigh.Text;
using Xunit;

namespace SimpleSigh.Text.Tests;

public class SignEditSessionTests
{
    [Fact]
    public void DraftStartsAsOriginal()
    {
        var session = new SignEditSession("研究室", 64, false);
        Assert.Equal("研究室", session.Draft);
        Assert.Equal("研究室", session.Original);
    }

    [Fact]
    public void NullOriginalOpensAsEmptyDraft()
    {
        var session = new SignEditSession(null, 64, false);
        Assert.Equal("", session.Draft);
        Assert.Equal("", session.Original);
    }

    [Fact]
    public void EditingDraftDoesNotChangeOriginal()
    {
        var session = new SignEditSession("研究室", 64, false);
        session.Draft = "倉庫";
        Assert.Equal("研究室", session.Original);
    }

    [Fact]
    public void ValidConfirmReturnsNormalizedText()
    {
        var session = new SignEditSession("", 256, true);
        session.Draft = "倉庫\r\n備品";
        Assert.True(session.TryConfirm(out string normalized, out string errorKey));
        Assert.Equal("倉庫\n備品", normalized);
        Assert.Null(errorKey);
    }

    [Fact]
    public void CancelRestoresOriginalDraft()
    {
        var session = new SignEditSession("Wood A-12", 64, false);
        session.Draft = "something else";
        session.Cancel();
        Assert.Equal("Wood A-12", session.Draft);
    }

    [Fact]
    public void OverlongDraftRefusesConfirmAndKeepsOriginal()
    {
        var session = new SignEditSession("研究室", 64, false);
        session.Draft = new string('あ', 65);
        Assert.False(session.TryConfirm(out string normalized, out string errorKey));
        Assert.Null(normalized);
        Assert.Equal(SignTextRules.TooLongKey, errorKey);
        Assert.Equal("研究室", session.Original);
        Assert.Equal(new string('あ', 65), session.Draft);
    }

    [Fact]
    public void NewlineOnSingleLineSignRefusesConfirm()
    {
        var session = new SignEditSession("", 64, false);
        session.Draft = "倉庫\n備品";
        Assert.False(session.TryConfirm(out _, out string errorKey));
        Assert.Equal(SignTextRules.NewlineNotAllowedKey, errorKey);
    }

    [Fact]
    public void NonBmpMixedInputRemainsIntact()
    {
        string text = "𠮷野家 Ａ-1 \U0001F600";
        var session = new SignEditSession("", 64, false) { Draft = text };
        Assert.True(session.TryConfirm(out string normalized, out _));
        Assert.Equal(text, normalized);
    }

    [Fact]
    public void ElementCountTracksDraftInGraphemes()
    {
        var session = new SignEditSession("", 64, false) { Draft = "がA\U0001F600" };
        Assert.Equal(3, session.ElementCount);
        Assert.Equal(64, session.MaxElements);
    }

    [Fact]
    public void NullDraftIsTreatedAsEmpty()
    {
        var session = new SignEditSession("x", 64, false) { Draft = null };
        Assert.Equal("", session.Draft);
        Assert.True(session.TryConfirm(out string normalized, out _));
        Assert.Equal("", normalized);
    }
}
