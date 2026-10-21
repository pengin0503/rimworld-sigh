using System;
using System.Collections.Generic;

namespace SimpleSigh.Layout;

/// <summary>Counters for render resources; exposed in dev mode to verify there is no per-frame rebuild.</summary>
public sealed class RenderCacheStats
{
    public int Builds { get; private set; }

    public int Releases { get; private set; }

    /// <summary>Resources currently alive (built and not yet released).</summary>
    public int Live { get; private set; }

    internal void OnBuilt(bool hasValue)
    {
        Builds++;
        if (hasValue)
        {
            Live++;
        }
    }

    internal void OnReleased()
    {
        Releases++;
        Live--;
    }
}

/// <summary>
/// Holds the single render resource of one sign. The resource is rebuilt only when the key
/// changes; the previous one is released first. "Nothing to draw" (null) is cached as well,
/// so an empty or unrenderable sign costs nothing per frame.
/// </summary>
public sealed class SignRenderSlot<TKey, TValue> where TValue : class
{
    private readonly Action<TValue> release;
    private readonly RenderCacheStats stats;
    private bool hasKey;
    private TKey key;
    private TValue value;

    public SignRenderSlot(Action<TValue> release, RenderCacheStats stats)
    {
        this.release = release;
        this.stats = stats;
    }

    public bool HasValue => value != null;

    public TValue Get(TKey newKey, Func<TKey, TValue> build)
    {
        if (hasKey && EqualityComparer<TKey>.Default.Equals(key, newKey))
        {
            return value;
        }
        Release();
        value = build(newKey);
        key = newKey;
        hasKey = true;
        stats.OnBuilt(value != null);
        return value;
    }

    public void Release()
    {
        if (value != null)
        {
            release(value);
            stats.OnReleased();
            value = null;
        }
        hasKey = false;
        key = default;
    }
}
