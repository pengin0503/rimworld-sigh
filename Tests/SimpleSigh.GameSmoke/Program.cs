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

        Console.WriteLine(failures == 0 ? "SMOKE PASS (assembly/XML only; the game was not run)" : $"SMOKE FAIL ({failures})");
        return failures == 0 ? 0 : 1;
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
