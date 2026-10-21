using SimpleSigh.Layout;
using Xunit;

namespace SimpleSigh.Text.Tests;

public class SignRenderKeyTests
{
    private static SignRenderKey Key(string text = "研究室", uint color = 0x221C17FF, float scale = 1f, int rotation = 0,
        int atlas = 1, int font = 2, int layout = 0)
    {
        return new SignRenderKey(text, color, scale, rotation, atlas, font, layout);
    }

    [Fact]
    public void SameInputsAreEqual()
    {
        Assert.Equal(Key(), Key());
        Assert.Equal(Key().GetHashCode(), Key().GetHashCode());
    }

    [Theory]
    [InlineData("text")]
    [InlineData("color")]
    [InlineData("scale")]
    [InlineData("rotation")]
    [InlineData("atlas")]
    [InlineData("font")]
    [InlineData("layout")]
    public void EveryVisibleInputChangesTheKey(string field)
    {
        SignRenderKey changed = field switch
        {
            "text" => Key(text: "倉庫"),
            "color" => Key(color: 0xFF0000FF),
            "scale" => Key(scale: 0.5f),
            "rotation" => Key(rotation: 1),
            "atlas" => Key(atlas: 2),
            "font" => Key(font: 1),
            _ => Key(layout: 7),
        };
        Assert.NotEqual(Key(), changed);
    }
}
