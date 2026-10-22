using System.Linq;
using SimpleSigh.Layout;

namespace SimpleSigh.Text.Tests;

/// <summary>Sign panels exactly as configured in Defs/ThingDefs (panelRect, panelRectHorizontal, drawSize, padding).</summary>
internal static class SignPanels
{
    internal sealed class Panel
    {
        public Panel(string name, PanelPlacement placement, float padding, bool multiline)
        {
            Name = name;
            Placement = placement;
            Padding = padding;
            Multiline = multiline;
        }

        public string Name { get; }

        public PanelPlacement Placement { get; }

        public float Padding { get; }

        public bool Multiline { get; }
    }

    private static readonly PanelRect BasicRect = new PanelRect(0.1406f, 0.3906f, 0.7188f, 0.4375f);
    private static readonly PanelRect WallRect = new PanelRect(0.125f, 0.25f, 0.75f, 0.4688f);
    private static readonly PanelRect BoardRect = new PanelRect(0.0703f, 0.2969f, 0.8594f, 0.5625f);
    private static readonly PanelRect BoardRectHorizontal = new PanelRect(0.1406f, 0.1641f, 0.7188f, 0.7578f);

    private static PanelPlacement Place(PanelRect rect, PanelRect horizontal, float sizeX, float sizeZ, int rotation, bool rotatedSize, bool mirror = false) =>
        SignPanelGeometry.Place(rect, horizontal, sizeX, sizeZ, rotation, 0f, 0f, rotatedSize, mirror);

    public static readonly Panel[] SingleLine =
    {
        new Panel("BasicSign", Place(BasicRect, default, 1f, 1f, 0, false), 0.08f, false),
        new Panel("WallSign", Place(WallRect, default, 1f, 1f, 0, false), 0.08f, false),
    };

    // Notice board and electronic sign share the panel geometry; west mirrors the east art.
    public static readonly Panel[] All = SingleLine.Concat(new[]
    {
        new Panel("Board-North", Place(BoardRect, BoardRectHorizontal, 2f, 1f, 0, true), 0.06f, true),
        new Panel("Board-East", Place(BoardRect, BoardRectHorizontal, 2f, 1f, 1, true), 0.06f, true),
        new Panel("Board-South", Place(BoardRect, BoardRectHorizontal, 2f, 1f, 2, true), 0.06f, true),
        new Panel("Board-West", Place(BoardRect, BoardRectHorizontal, 2f, 1f, 3, true, mirror: true), 0.06f, true),
    }).ToArray();

    public static Panel Get(string name) => All.First(p => p.Name == name);
}
