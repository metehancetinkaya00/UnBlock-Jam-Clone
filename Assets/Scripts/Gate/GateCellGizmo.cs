using UnityEngine;

[ExecuteAlways]
public class GateCellGizmo : MonoBehaviour
{
    public Gate gate;
    public Color color = new Color(0f, 1f, 0f, 0.35f);

    void OnDrawGizmos()
    {
        if (!gate) gate = GetComponent<Gate>();
        if (!gate || !gate.grid) return;

        var g = gate.grid;
        Vector2Int c = gate.gateCell;

        Vector3 center = g.CellCenterWorld(c.x, c.y);
        Gizmos.color = color;
        Gizmos.DrawCube(center, new Vector3(g.cellSize * 0.9f, g.cellSize * 0.9f, 0.1f));
    }
}
