using System.Collections.Generic;
using SimpleSigh.Text;
using Xunit;

namespace SimpleSigh.Text.Tests;

public class FavoriteColorListTests
{
    [Fact]
    public void AddsNewColourAtTheFront()
    {
        var list = new List<uint> { 0x112233FF };
        FavoriteColorList.Add(list, 0xAABBCCFF);
        Assert.Equal(new List<uint> { 0xAABBCCFF, 0x112233FF }, list);
    }

    [Fact]
    public void ExistingColourMovesToTheFrontWithoutDuplicating()
    {
        var list = new List<uint> { 0x112233FF, 0xAABBCCFF };
        FavoriteColorList.Add(list, 0xAABBCCFF);
        Assert.Equal(new List<uint> { 0xAABBCCFF, 0x112233FF }, list);
    }

    [Fact]
    public void OldestColourIsDroppedBeyondTheLimit()
    {
        var list = new List<uint>();
        for (uint i = 1; i <= FavoriteColorList.MaxCount + 2; i++)
        {
            FavoriteColorList.Add(list, i);
        }
        Assert.Equal(FavoriteColorList.MaxCount, list.Count);
        Assert.Equal((uint)FavoriteColorList.MaxCount + 2, list[0]);
        Assert.DoesNotContain(1u, list);
        Assert.DoesNotContain(2u, list);
    }

    [Fact]
    public void RemoveDeletesOnlyThatColour()
    {
        var list = new List<uint> { 1, 2, 3 };
        FavoriteColorList.Remove(list, 2);
        Assert.Equal(new List<uint> { 1, 3 }, list);
    }
}
