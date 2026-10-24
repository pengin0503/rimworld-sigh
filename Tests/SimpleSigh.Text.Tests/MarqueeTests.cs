using System;
using System.Collections.Generic;
using SimpleSigh.Layout;
using Xunit;

namespace SimpleSigh.Text.Tests;

/// <summary>LED effects: scrolling text (marquee) and blinking.</summary>
public class MarqueeTests
{
    private readonly FakeGlyphs glyphs = new FakeGlyphs();

    private static List<GlyphQuad> Line(params float[] starts)
    {
        var quads = new List<GlyphQuad>();
        foreach (float x in starts)
        {
            quads.Add(new GlyphQuad { X0 = x, X1 = x + 1f, Y0 = -0.5f, Y1 = 0.5f, U0 = 0f, U1 = 1f, VBottom = 0f, VTop = 1f });
        }
        return quads;
    }

    [Fact]
    public void OffsetLoops()
    {
        // Text 3 wide, panel 2 wide: one cycle is 5 cells, at 1 cell per second.
        Assert.Equal(0f, Marquee.Offset(0f, 1f, 3f, 2f), 4);
        Assert.Equal(1.5f, Marquee.Offset(1.5f, 1f, 3f, 2f), 4);
        Assert.Equal(1.5f, Marquee.Offset(6.5f, 1f, 3f, 2f), 4);
        Assert.Equal(Marquee.Offset(2f, 1f, 3f, 2f), Marquee.Offset(2f + 5f * 1000f, 1f, 3f, 2f), 3);
        Assert.InRange(Marquee.Offset(4.999f, 1f, 3f, 2f), 4.9f, 5f);
        Assert.Equal(0f, Marquee.Offset(3f, 1f, 0f, 0f));
    }

    [Fact]
    public void TextEntersFromTheRightAndLeavesFullyOnTheLeft()
    {
        var output = new List<GlyphQuad>();
        List<GlyphQuad> line = Line(0f, 1f, 2f); // 3 cells of text
        Marquee.Clip(line, 0f, 1f, 1f, output);
        Assert.Empty(output); // starts just beyond the right edge
        Marquee.Clip(line, 0.5f, 1f, 1f, output);
        Assert.Single(output);
        Assert.Equal(0.5f, output[0].X0, 4);
        Assert.Equal(1f, output[0].X1, 4);
        Marquee.Clip(line, 5f, 1f, 1f, output); // offset = text + panel width: gone on the left
        Assert.Empty(output);
    }

    [Theory]
    [InlineData(0.3f)]
    [InlineData(1.25f)]
    [InlineData(2.6f)]
    [InlineData(4.7f)]
    public void ClipsGlyphsAtPanelEdges(float offset)
    {
        var output = new List<GlyphQuad>();
        Marquee.Clip(Line(0f, 1f, 2f), offset, 1f, 0.4f, output);
        foreach (GlyphQuad q in output)
        {
            Assert.InRange(q.X0, -1f, 1f);
            Assert.InRange(q.X1, -1f, 1f);
            Assert.InRange(q.Y0, -0.4f, 0.4f);
            Assert.InRange(q.Y1, -0.4f, 0.4f);
            Assert.True(q.X1 > q.X0);
            // UVs are trimmed in proportion: u matches the visible part of the glyph.
            float glyphX0 = (float)Math.Floor(q.X0 - (1f - offset) + 1e-4f) + (1f - offset);
            Assert.Equal(q.X0 - glyphX0, q.U0, 3);
            Assert.Equal(q.X1 - glyphX0, q.U1, 3);
            Assert.Equal(0.1f, q.VBottom, 3);
            Assert.Equal(0.9f, q.VTop, 3);
        }
    }

    [Fact]
    public void ClipDoesNotAllocateWithCapacity()
    {
        List<GlyphQuad> line = Line(0f, 1f, 2f, 3f, 4f, 5f);
        var output = new List<GlyphQuad>(line.Count);
        Marquee.Clip(line, 2f, 1.5f, 1f, output);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++)
        {
            Marquee.Clip(line, i * 0.1f, 1.5f, 1f, output);
        }
        Assert.Equal(before, GC.GetAllocatedBytesForCurrentThread());
    }

    [Fact]
    public void BlinkDutyCycle()
    {
        Assert.True(SignVisibility.BlinkOn(0f));
        Assert.True(SignVisibility.BlinkOn(0.59f));
        Assert.False(SignVisibility.BlinkOn(0.61f));
        Assert.False(SignVisibility.BlinkOn(0.99f));
        Assert.True(SignVisibility.BlinkOn(1.01f));
        Assert.True(SignVisibility.BlinkOn(1234.3f));
        Assert.False(SignVisibility.BlinkOn(1234.8f));
        int on = 0;
        for (int i = 0; i < 1000; i++)
        {
            on += SignVisibility.BlinkOn(i * 0.01f + 0.005f) ? 1 : 0;
        }
        Assert.Equal(600, on);
    }

    [Fact]
    public void ScrollLayoutIsOneRowAtUserScaleStartingAtZero()
    {
        var panel = new PanelPlacement(0.3f, -0.1f, 2f, 0.5f);
        var style = new SignTextStyle(0.5f, true, SignHAlign.Center, SignVAlign.Middle, false, SignEffect.Scroll);
        Marquee.Layout layout = Marquee.Build("AB\nCD", panel, 0.1f, style, glyphs);
        float innerHeight = 0.5f - 2f * 0.1f * 0.5f;
        Assert.Equal(innerHeight * 0.5f, layout.LineHeight, 4);
        Assert.Equal(4, layout.Quads.Count); // the line break became a space
        Assert.InRange(layout.Quads[0].X0, 0f, layout.LineHeight * 0.1f); // the pen starts at x = 0
        Assert.True(layout.TextWidth > layout.Quads[3].X1 - 1e-4f);
        Assert.Equal(1f, layout.HalfWidth, 4);
        Assert.Equal(0.25f, layout.HalfHeight, 4);
        Assert.Equal(0.3f, layout.CenterX, 4);
        Assert.Equal(-0.1f, layout.CenterZ, 4);
        foreach (GlyphQuad q in layout.Quads)
        {
            Assert.InRange(q.Y0, -layout.LineHeight / 2 - 1e-4f, layout.LineHeight / 2 + 1e-4f);
        }
    }

    [Fact]
    public void ScrollLayoutFollowsVerticalAlignment()
    {
        var panel = new PanelPlacement(0f, 0f, 2f, 1f);
        var top = new SignTextStyle(0.5f, true, SignHAlign.Center, SignVAlign.Top, false, SignEffect.Scroll);
        Marquee.Layout layout = Marquee.Build("A", panel, 0f, top, glyphs);
        Assert.True(layout.Quads[0].Y1 > 0.25f);
        Assert.Empty(Marquee.Build("", panel, 0f, top, glyphs).Quads);
    }

    [Fact]
    public void ScrollSpeedFollowsRowHeight()
    {
        Assert.Equal(2f * Marquee.SpeedFor(0.2f), Marquee.SpeedFor(0.4f), 5);
        Assert.True(Marquee.SpeedFor(0.2f) > 0f);
    }
}
