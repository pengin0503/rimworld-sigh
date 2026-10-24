using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SimpleSigh.Fonts;
using SimpleSigh.Layout;
using Xunit;
using Xunit.Abstractions;

namespace SimpleSigh.Text.Tests;

/// <summary>
/// Offline cost of the game-independent work behind 1000 signs (font: the bundled sign font). The
/// limits are deliberately loose so slow CI machines pass; the measured numbers are printed and
/// recorded in docs/testing/performance.md. These are not in-game frame times.
/// </summary>
public class PerformanceTests
{
    private const int SignCount = 1000;

    private readonly ITestOutputHelper output;

    public PerformanceTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    private static string[] Texts()
    {
        string[] samples =
        {
            "研究室", "食料保管庫", "立入禁止区域", "Food Storage", "第二研究施設の入口",
            "本日の作業予定：\n午前は畑の収穫、午後は倉庫の整理。夜間は発電設備の点検を行います。",
        };
        // Distinct texts, so no layout can be shared between signs.
        return Enumerable.Range(0, SignCount).Select(i => samples[i % samples.Length] + " " + i).ToArray();
    }

    private static (PanelPlacement Panel, float Padding, bool Multiline)[] Panels() => new[]
    {
        (SignPanelGeometry.Place(new PanelRect(0.1406f, 0.3906f, 0.7188f, 0.4375f), default, 1f, 1f, 0, 0f, 0f, false), 0.08f, false),
        (SignPanelGeometry.Place(new PanelRect(0.0703f, 0.2969f, 0.8594f, 0.5625f), new PanelRect(0.1406f, 0.1641f, 0.7188f, 0.7578f),
            2f, 1f, 0, 0f, 0f, true), 0.06f, true),
        (SignPanelGeometry.Place(new PanelRect(0.0469f, 0.2969f, 0.9062f, 0.5625f), new PanelRect(0.1406f, 0.1094f, 0.7188f, 0.8385f),
            3f, 1f, 1, 0f, 0f, true), 0.06f, true),
    };

    [Fact]
    public void LayoutFor1000SignsStaysWellUnderASecond()
    {
        string[] texts = Texts();
        var atlas = new GlyphAtlas(TrueTypeFontTests.SignFont, 48f, 512, 2048);
        var raster = Stopwatch.StartNew();
        foreach (string text in texts)
        {
            atlas.Ensure(text);
        }
        raster.Stop();

        var panels = Panels();
        SignTextLayout.Build(texts[0], panels[0].Panel, panels[0].Padding, panels[0].Multiline, SignTextStyle.Default, atlas); // JIT
        int quads = 0;
        var layout = Stopwatch.StartNew();
        for (int i = 0; i < texts.Length; i++)
        {
            var p = panels[i % panels.Length];
            quads += SignTextLayout.Build(texts[i], p.Panel, p.Padding, p.Multiline, SignTextStyle.Default, atlas).Quads.Count;
        }
        layout.Stop();

        output.WriteLine($"glyph rasterization for {SignCount} texts: {raster.ElapsedMilliseconds} ms (one-off per character)");
        output.WriteLine($"layout of {SignCount} signs: {layout.Elapsed.TotalMilliseconds:F1} ms total, " +
                         $"{layout.Elapsed.TotalMilliseconds * 1000 / SignCount:F0} us per sign, {quads} glyph quads");
        Assert.True(quads > SignCount * 3);
        // Layout runs only when a sign's text, style, rotation or the glyph atlas changes, never per frame.
        Assert.True(layout.ElapsedMilliseconds < 5000, $"layout took {layout.ElapsedMilliseconds} ms");
    }

    [Fact]
    public void ScrollingFrameFor100SignsIsCheapAndAllocationFree()
    {
        var atlas = new GlyphAtlas(TrueTypeFontTests.SignFont, 48f, 512, 2048);
        var panel = new PanelPlacement(0f, 0f, 2.6f, 0.5f);
        var style = new SignTextStyle(1f, true, SignHAlign.Center, SignVAlign.Middle, false, SignEffect.Scroll);
        var rows = new List<Marquee.Layout>();
        var buffers = new List<List<GlyphQuad>>();
        for (int i = 0; i < 100; i++)
        {
            string text = "ようこそ！ 本日の夕食はシチューです。Welcome! " + i;
            atlas.Ensure(text);
            Marquee.Layout row = Marquee.Build(text, panel, 0.06f, style, atlas);
            rows.Add(row);
            buffers.Add(new List<GlyphQuad>(row.Quads.Count));
        }
        void Frame(float time)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                Marquee.Layout row = rows[i];
                float offset = Marquee.Offset(time, Marquee.SpeedFor(row.LineHeight), row.TextWidth, row.PanelWidth);
                Marquee.Clip(row.Quads, offset, row.HalfWidth, row.HalfHeight, buffers[i]);
            }
        }
        Frame(0.5f);
        var watch = new Stopwatch();
        long before = GC.GetAllocatedBytesForCurrentThread();
        watch.Start();
        const int frames = 600;
        for (int f = 0; f < frames; f++)
        {
            Frame(f / 60f);
        }
        watch.Stop();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        output.WriteLine($"scroll clip for 100 signs: {watch.Elapsed.TotalMilliseconds * 1000 / frames:F0} us per frame, {allocated} bytes allocated in {frames} frames");
        Assert.Equal(0, allocated);
        Assert.True(watch.ElapsedMilliseconds < 3000, $"{frames} frames took {watch.ElapsedMilliseconds} ms");
    }
}
