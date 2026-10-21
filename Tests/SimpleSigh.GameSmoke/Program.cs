using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using SimpleSigh.Signs;
using Verse;

namespace SimpleSigh.GameSmoke;

/// <summary>Exit code 0 = all smoke checks passed. Not a substitute for running the game.</summary>
public static class Program
{
    private static int failures;

    public static int Main(string[] args)
    {
        string repo = args.Length > 0 ? args[0] : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        Check("SimpleSigh.dll types resolve against 1.7 (AssemblyIsUsable)", () =>
        {
            Assembly asm = typeof(CompSignText).Assembly;
            Type[] types = asm.GetTypes();
            Require(types.Any(t => t == typeof(CompProperties_SignText)), "CompProperties_SignText missing");
            Require(typeof(ThingComp).IsAssignableFrom(typeof(CompSignText)), "CompSignText is not a ThingComp");
            Require(typeof(GameComponent).IsAssignableFrom(typeof(Rendering.GameComponent_SignRenderCleanup)), "cleanup is not a GameComponent");
            Require(typeof(Rendering.GameComponent_SignRenderCleanup).GetConstructor(new[] { typeof(Game) }) != null,
                "GameComponent needs a (Game) constructor for Game.FillComponents");
        });

        foreach (string file in Directory.GetFiles(Path.Combine(repo, "Defs"), "*.xml", SearchOption.AllDirectories))
        {
            var doc = new XmlDocument();
            doc.Load(file);
            foreach (XmlNode li in doc.SelectNodes("//comps/li[@Class='SimpleSigh.Signs.CompProperties_SignText']"))
            {
                string defName = li.SelectSingleNode("../../defName")?.InnerText ?? "?";
                Check($"{Path.GetFileName(file)} {defName}: comp XML elements map to public fields of the right type", () =>
                {
                    // DirectXmlToObject maps each child element to the public field of the same name and
                    // parses it with ParseHelper. Neither can start outside the game (their static
                    // constructors need engine/Steam assemblies that are not in the reference ZIP), so
                    // values are parsed here following the decompiled 1.7 ParseHelper formats.
                    var props = new CompProperties_SignText();
                    Require(props.compClass == typeof(CompSignText), "compClass is not CompSignText");
                    foreach (XmlNode child in li.ChildNodes)
                    {
                        if (child.NodeType != XmlNodeType.Element)
                        {
                            continue;
                        }
                        FieldInfo field = typeof(CompProperties_SignText).GetField(child.Name, BindingFlags.Public | BindingFlags.Instance);
                        Require(field != null, $"no public field '{child.Name}'");
                        object value = ParseLike17(child.InnerText, field.FieldType);
                        field.SetValue(props, value);
                        Console.WriteLine($"      {child.Name} = {value}");
                    }
                    Require(props.panelRect.width > 0f && props.panelRect.height > 0f, $"panelRect not parsed: {props.panelRect}");
                });
            }
        }

        foreach (string file in Directory.GetFiles(Path.Combine(repo, "Defs"), "*.xml", SearchOption.AllDirectories))
        {
            var doc = new XmlDocument();
            doc.Load(file);
            foreach (XmlElement def in doc.DocumentElement.ChildNodes.OfType<XmlElement>().Where(e => e.Name == "ThingDef"))
            {
                string name = def["defName"]?.InnerText ?? def.GetAttribute("Name");
                Check($"{Path.GetFileName(file)} {name}: every XML field exists in the 1.7 Def classes", () =>
                {
                    RequireFields(def, typeof(ThingDef), name);
                    if (def["graphicData"] != null) RequireFields(def["graphicData"], typeof(GraphicData), name + ".graphicData");
                    if (def["building"] != null) RequireFields(def["building"], typeof(RimWorld.BuildingProperties), name + ".building");
                    foreach (XmlElement li in def["comps"]?.ChildNodes.OfType<XmlElement>() ?? Enumerable.Empty<XmlElement>())
                    {
                        string cls = li.GetAttribute("Class");
                        if (string.IsNullOrEmpty(cls) || cls.StartsWith("SimpleSigh."))
                        {
                            continue;
                        }
                        Type props = typeof(ThingDef).Assembly.GetType("RimWorld." + cls) ?? typeof(ThingDef).Assembly.GetType("Verse." + cls);
                        Require(props != null && typeof(CompProperties).IsAssignableFrom(props), $"comp {cls} not found in 1.7");
                        RequireFields(li, props, name + "." + cls);
                        XmlElement compClass = li["compClass"];
                        if (compClass != null)
                        {
                            Type comp = typeof(ThingDef).Assembly.GetType("RimWorld." + compClass.InnerText) ?? typeof(ThingDef).Assembly.GetType("Verse." + compClass.InnerText);
                            Require(comp != null && typeof(ThingComp).IsAssignableFrom(comp), $"compClass {compClass.InnerText} not found in 1.7");
                        }
                    }
                    foreach (XmlElement li in def["placeWorkers"]?.ChildNodes.OfType<XmlElement>() ?? Enumerable.Empty<XmlElement>())
                    {
                        Type worker = typeof(ThingDef).Assembly.GetType("RimWorld." + li.InnerText) ?? typeof(ThingDef).Assembly.GetType("Verse." + li.InnerText);
                        Require(worker != null && typeof(PlaceWorker).IsAssignableFrom(worker), $"placeWorker {li.InnerText} not found in 1.7");
                    }
                });
            }
        }

        Check("CompSignText.PostExposeData writes through the real 1.7 ScribeSaver", () =>
        {
            const string text = "研究室 C:\\new\n2行目  ";
            var comp = new CompSignText();
            comp.Initialize(new CompProperties_SignText { maxElements = 256, multiline = true });
            comp.Apply(text, new UnityEngine.Color(0.62f, 0.12f, 0.10f, 1f), 0.5f);
            string file = Path.Combine(Path.GetTempPath(), "simplesigh-smoke-save.xml");
            Scribe.saver.InitSaving(file, "sign");
            comp.PostExposeData();
            Scribe.saver.FinalizeSaving();
            var saved = new XmlDocument();
            saved.Load(file);
            string stored = saved.SelectSingleNode("/sign/simpleSighText")?.InnerText;
            Console.WriteLine($"      saved XML: {saved.DocumentElement.InnerXml}");
            Require(stored != null, "simpleSighText not written");
            Require(!stored.Contains("\\") && !stored.Contains("\n"), "stored text contains a backslash or newline");
            // Replay 1.7 ParseHelper.ParseString, then decode.
            Require(Text.SignTextCodec.Decode(stored.Replace("\\n", "\n")) == text, "round trip changed the text");
            Require(saved.SelectSingleNode("/sign/simpleSighColor") != null, "colour not written");
            Require(saved.SelectSingleNode("/sign/simpleSighScale")?.InnerText == "0.5", "scale not written");
            File.Delete(file);
        });

        Check("TmpGlyphSource adapts the 1.7 ITextMeshProBackend (fake backend)", () =>
        {
            var backend = new FakeTextBackend();
            Require(Rendering.TmpGlyphSource.TryCreateFor(backend, 2, out Rendering.TmpGlyphSource source), "no source");
            Require(source.LineHeight == 38f, $"line height {source.LineHeight} != ascent - descent");
            Require(source.TryGetGlyph('A', out Layout.GlyphMetrics a) && a.Found && a.AdvanceX == 18f && a.Y1 == 34f && a.V0 == 0.2f, "glyph not mapped");
            Require(!source.TryGetGlyph('☃', out _), "missing glyph reported as found");
            Require(Rendering.TmpGlyphSource.TryCreateFor(backend, 2, out Rendering.TmpGlyphSource again) && ReferenceEquals(source, again), "source not reused");
            Layout.SignTextLayout.Result layout = Layout.SignTextLayout.Build("研究室", new Layout.PanelPlacement(0f, 0.1f, 0.72f, 0.44f), 0.08f, false, 1f, source);
            Require(layout.Quads.Count == 3, $"expected 3 quads, got {layout.Quads.Count}");
        });

        Check("TmpGlyphSource re-reads font metrics when the atlas version changes (e.g. language switch)", () =>
        {
            var backend = new FakeTextBackend();
            Rendering.TmpGlyphSource.TryCreateFor(backend, 2, out Rendering.TmpGlyphSource before);
            backend.Ascent = 40f;
            backend.AtlasVersion++;
            Rendering.TmpGlyphSource.TryCreateFor(backend, 2, out Rendering.TmpGlyphSource after);
            Require(after.LineHeight == 48f, $"stale line height {after.LineHeight} after atlas change");
            Require(after.AtlasVersion == backend.AtlasVersion, "atlas version not tracked");
        });

        Console.WriteLine(failures == 0 ? "SMOKE PASS (assembly/XML only; the game was not run)" : $"SMOKE FAIL ({failures})");
        return failures == 0 ? 0 : 1;
    }

    /// <summary>
    /// The 1.7 XML loader binds child elements to instance fields, public or not
    /// (DirectXmlSaveLoadUtility.FieldGetFlags); unknown names are load errors in game.
    /// </summary>
    private static void RequireFields(XmlElement node, Type type, string context)
    {
        foreach (XmlElement child in node.ChildNodes.OfType<XmlElement>())
        {
            FieldInfo field = null;
            for (Type t = type; t != null && field == null; t = t.BaseType)
            {
                field = t.GetField(child.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            }
            Require(field != null, $"{context}: {type.Name} has no field '{child.Name}'");
        }
    }

    /// <summary>Subset of 1.7 ParseHelper used by our comp: int, float, bool, and FromStringRect "(x,y,w,h)".</summary>
    private static object ParseLike17(string text, Type type)
    {
        var culture = System.Globalization.CultureInfo.InvariantCulture;
        if (type == typeof(int)) return int.Parse(text, culture);
        if (type == typeof(float)) return float.Parse(text, culture);
        if (type == typeof(bool)) return bool.Parse(text);
        if (type == typeof(UnityEngine.Rect))
        {
            string[] parts = text.TrimStart('(').TrimEnd(')').Split(',');
            Require(parts.Length == 4, $"Rect needs 4 values: {text}");
            return new UnityEngine.Rect(Convert.ToSingle(parts[0], culture), Convert.ToSingle(parts[1], culture),
                Convert.ToSingle(parts[2], culture), Convert.ToSingle(parts[3], culture));
        }
        if (type == typeof(UnityEngine.Color))
        {
            // 1.7 ParseColor: "(r,g,b[,a])", floats 0..1 (byte values > 1 are also accepted by the game).
            string[] parts = text.TrimStart('(', 'R', 'G', 'B', 'A').TrimEnd(')').Split(',');
            Require(parts.Length == 3 || parts.Length == 4, $"Color needs 3 or 4 values: {text}");
            float[] v = parts.Select(p => Convert.ToSingle(p, culture)).ToArray();
            Require(v.All(x => x >= 0f && x <= 1f), $"use 0..1 floats for colours: {text}");
            return new UnityEngine.Color(v[0], v[1], v[2], v.Length == 4 ? v[3] : 1f);
        }
        throw new InvalidOperationException($"smoke parser does not cover {type.Name}");
    }

    private static void Check(string name, Action action)
    {
        try
        {
            action();
            Console.WriteLine($"  ok   {name}");
        }
        catch (Exception ex)
        {
            failures++;
            Console.WriteLine($"  FAIL {name}\n       {ex.GetType().Name}: {ex.Message}");
            for (Exception inner = ex.InnerException; inner != null; inner = inner.InnerException)
            {
                Console.WriteLine($"       inner {inner.GetType().Name}: {inner.Message}");
            }
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
