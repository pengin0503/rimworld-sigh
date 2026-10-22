using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using SimpleSigh.Fonts;
using SimpleSigh.Layout;

namespace SimpleSigh.Preview;

/// <summary>
/// Lays out sample texts with the mod's own code for every def and rotation: SignPanelGeometry and
/// SignTextLayout, with glyphs from the bundled sign font rasterized by the mod's GlyphAtlas. Writes the
/// quads with their atlas UVs as JSON and the atlas coverage as raw bytes, so the preview draws exactly
/// the texture regions the in-game mesh would sample.
/// Usage: SignPreview REPO FONT.ttf CASES.json OUT.json OUT-ATLAS.bin
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        string repo = args[0];
        var atlas = new GlyphAtlas(new TrueTypeFont(File.ReadAllBytes(args[1])), 48f, 512, 2048);
        JsonElement cases = JsonDocument.Parse(File.ReadAllText(args[2])).RootElement;
        var defs = LoadDefs(repo);
        // Rasterize every character first so all quads refer to the final atlas layout.
        foreach (JsonElement c in cases.EnumerateArray())
        {
            atlas.Ensure(c.GetProperty("text").GetString());
        }
        var output = new List<object>();
        foreach (JsonElement c in cases.EnumerateArray())
        {
            string defName = c.GetProperty("def").GetString();
            int rotation = c.GetProperty("rotation").GetInt32();
            string text = c.GetProperty("text").GetString();
            float userScale = c.TryGetProperty("scale", out JsonElement s) ? s.GetSingle() : 1f;
            bool autoWrap = !c.TryGetProperty("autoWrap", out JsonElement w) || w.GetBoolean();
            XElement def = defs[defName];
            XElement graphic = Field(def, defs, "graphicData");
            XElement comp = Field(def, defs, "comps").Elements("li").First(li => (string)li.Attribute("Class") == "SimpleSigh.Signs.CompProperties_SignText");
            bool multi = (string)graphic.Element("graphicClass") == "Graphic_Multi";
            bool drawRotated = (string)graphic.Element("drawRotated") != "false";
            float[] drawSize = Floats((string)graphic.Element("drawSize") ?? "(1,1)");
            string[] rotNames = { "North", "East", "South", "West" };
            float[] offset = Floats((string)graphic.Element("drawOffset" + rotNames[rotation]) ?? "(0,0,0)");
            PanelRect north = Rect((string)comp.Element("panelRect"));
            PanelRect horizontal = comp.Element("panelRectHorizontal") != null ? Rect((string)comp.Element("panelRectHorizontal")) : default;
            float padding = comp.Element("panelPadding") != null ? float.Parse((string)comp.Element("panelPadding"), CultureInfo.InvariantCulture) : 0.08f;
            bool multiline = (string)comp.Element("multiline") == "true";
            // Same inputs the in-game renderer derives from the Graphic: Graphic_Multi with a distinct east texture
            // and Graphic_Single with drawRotated=false are not drawn rotated; Graphic_Multi west mirrors east.
            bool rotatedSize = multi || !drawRotated;
            bool mirror = multi && rotation == 3;
            PanelPlacement panel = SignPanelGeometry.Place(north, horizontal, drawSize[0], drawSize[1], rotation, offset[0], offset[2], rotatedSize, mirror);
            SignTextLayout.Result layout = SignTextLayout.Build(text, panel, padding, multiline, userScale, atlas, autoWrap);
            output.Add(new
            {
                def = defName,
                rotation,
                text,
                panel = new { x = panel.CenterX, z = panel.CenterZ, w = panel.Width, h = panel.Height },
                fits = layout.Fits,
                lines = layout.LineCount,
                lineHeight = layout.LineHeight,
                missing = GlyphQuadBuilder.HasMissingGlyphs(text, atlas),
                quads = layout.Quads.Select(q => new
                {
                    x0 = q.X0 + layout.CenterX, x1 = q.X1 + layout.CenterX, z0 = q.Y0 + layout.CenterZ, z1 = q.Y1 + layout.CenterZ,
                    u0 = q.U0, u1 = q.U1, vTop = q.VTop, vBottom = q.VBottom,
                }),
            });
        }
        File.WriteAllText(args[3], JsonSerializer.Serialize(new { atlasWidth = atlas.Width, atlasHeight = atlas.Height, cases = output }));
        File.WriteAllBytes(args[4], atlas.Alpha);
        return 0;
    }

    private static Dictionary<string, XElement> LoadDefs(string repo)
    {
        var all = Directory.GetFiles(Path.Combine(repo, "Defs"), "*.xml", SearchOption.AllDirectories)
            .SelectMany(f => XDocument.Load(f).Root.Elements("ThingDef")).ToList();
        var result = new Dictionary<string, XElement>();
        foreach (XElement d in all)
        {
            string key = (string)d.Element("defName") ?? "@" + (string)d.Attribute("Name");
            result[key] = d;
        }
        return result;
    }

    private static XElement Field(XElement def, Dictionary<string, XElement> defs, string name)
    {
        for (XElement d = def; d != null; d = defs.GetValueOrDefault("@" + (string)d.Attribute("ParentName")))
        {
            if (d.Element(name) != null)
            {
                return d.Element(name);
            }
        }
        throw new InvalidOperationException($"{name} not found");
    }

    private static float[] Floats(string text) =>
        text.Trim('(', ')', ' ').Split(',').Select(v => float.Parse(v, CultureInfo.InvariantCulture)).ToArray();

    private static PanelRect Rect(string text)
    {
        float[] v = Floats(text);
        return new PanelRect(v[0], v[1], v[2], v[3]);
    }
}
