using System.Collections.Generic;
using SimpleSigh.Layout;
using Xunit;

namespace SimpleSigh.Text.Tests;

public class SignRenderSlotTests
{
    private sealed class Resource
    {
        public Resource(string name) => Name = name;

        public string Name { get; }

        public int Released { get; set; }
    }

    private readonly RenderCacheStats stats = new RenderCacheStats();
    private readonly List<Resource> built = new List<Resource>();
    private SignRenderSlot<string, Resource> slot;

    public SignRenderSlotTests()
    {
        slot = new SignRenderSlot<string, Resource>(r => r.Released++, stats);
    }

    private Resource Build(string key)
    {
        var resource = new Resource(key);
        built.Add(resource);
        return resource;
    }

    [Fact]
    public void UnchangedKeyBuildsOnceAcrossManyFrames()
    {
        for (int frame = 0; frame < 10_000; frame++)
        {
            Assert.Equal("研究室", slot.Get("研究室", Build).Name);
        }
        Assert.Single(built);
        Assert.Equal(1, stats.Builds);
        Assert.Equal(1, stats.Live);
    }

    [Fact]
    public void KeyChangeRebuildsExactlyOnceAndReleasesOld()
    {
        slot.Get("a", Build);
        slot.Get("b", Build);
        slot.Get("b", Build);
        Assert.Equal(2, built.Count);
        Assert.Equal(1, built[0].Released);
        Assert.Equal(0, built[1].Released);
        Assert.Equal(2, stats.Builds);
        Assert.Equal(1, stats.Releases);
        Assert.Equal(1, stats.Live);
    }

    [Fact]
    public void ReleaseFreesCurrentValueAndNextGetRebuilds()
    {
        slot.Get("a", Build);
        slot.Release();
        Assert.Equal(1, built[0].Released);
        Assert.Equal(0, stats.Live);
        slot.Get("a", Build);
        Assert.Equal(2, built.Count);
    }

    [Fact]
    public void ReleaseIsIdempotent()
    {
        slot.Release();
        slot.Get("a", Build);
        slot.Release();
        slot.Release();
        Assert.Equal(1, built[0].Released);
        Assert.Equal(1, stats.Releases);
        Assert.Equal(0, stats.Live);
    }

    [Fact]
    public void NullResultIsCachedToo()
    {
        int calls = 0;
        for (int frame = 0; frame < 100; frame++)
        {
            Assert.Null(slot.Get("empty", _ => { calls++; return null; }));
        }
        Assert.Equal(1, calls);
        Assert.Equal(0, stats.Live);
    }

    [Fact]
    public void HasValueReflectsState()
    {
        Assert.False(slot.HasValue);
        slot.Get("a", Build);
        Assert.True(slot.HasValue);
        slot.Release();
        Assert.False(slot.HasValue);
    }
}
