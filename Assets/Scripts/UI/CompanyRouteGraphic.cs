// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-06: Bounded outlined route arrows with per-step action-budget colors (spec 40B).
// See CHANGES.md and License.txt for GPL-3.0 terms.
using UnityEngine;
using UnityEngine.UI;

public sealed class CompanyRouteGraphic : MaskableGraphic
{
    internal static readonly Color Affordable = new Color32(249, 217, 118, 255);
    internal static readonly Color OverBudget = new Color32(255, 100, 92, 255);
    int incoming, outgoing;
    bool incomingRed, outgoingRed, head;
    internal void SetRoute(int from, int to, bool redFrom, bool redTo, bool arrowhead)
    {
        if (incoming == from && outgoing == to && incomingRed == redFrom && outgoingRed == redTo && head == arrowhead) return;
        incoming = from; outgoing = to; incomingRed = redFrom; outgoingRed = redTo; head = arrowhead;
        SetVerticesDirty();
    }
    internal static int Direction(int from, int to, int width)
    {
        if (to == from - 1) return 1;
        if (to == from + 1) return 2;
        return to == from - width ? 4 : 8;
    }
    static Vector2 Vector(int direction)
    {
        switch (direction)
        {
            case 1: return Vector2.left;
            case 2: return Vector2.right;
            case 4: return Vector2.up;
            default: return Vector2.down;
        }
    }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (incoming == 0 && outgoing == 0) return;
        var rect = GetPixelAdjustedRect();
        for (int layer = 0; layer < 2; layer++)
        {
            float half = layer == 0 ? 3.5f : 2;
            Color edge = new Color(0, 0, 0, .9f);
            if (incoming != 0) Segment(mesh, rect.center, Vector(incoming), rect.width / 2, half, layer == 0 ? edge : incomingRed ? OverBudget : Affordable);
            if (outgoing != 0) Segment(mesh, rect.center, Vector(outgoing), rect.width / 2, half, layer == 0 ? edge : outgoingRed ? OverBudget : Affordable);
            if (head && incoming != 0)
            {
                Vector2 forward = -Vector(incoming), side = new Vector2(-forward.y, forward.x);
                float size = layer == 0 ? 9 : 6.5f;
                Triangle(mesh, rect.center + forward * size, rect.center - forward * size + side * size,
                    rect.center - forward * size - side * size, layer == 0 ? edge : incomingRed ? OverBudget : Affordable);
            }
        }
    }
    static void Segment(VertexHelper mesh, Vector2 center, Vector2 direction, float length, float half, Color tint)
    {
        var side = new Vector2(-direction.y, direction.x) * half;
        Vector2 end = center + direction * length;
        Triangle(mesh, center - side, center + side, end + side, tint);
        Triangle(mesh, center - side, end + side, end - side, tint);
    }
    static void Triangle(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Color tint)
    {
        int start = mesh.currentVertCount;
        mesh.AddVert(a, tint, Vector2.zero); mesh.AddVert(b, tint, Vector2.zero); mesh.AddVert(c, tint, Vector2.zero);
        mesh.AddTriangle(start, start + 1, start + 2);
    }
}
