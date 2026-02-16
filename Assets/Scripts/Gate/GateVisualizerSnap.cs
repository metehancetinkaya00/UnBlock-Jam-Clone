using UnityEngine;

public class GateVisualizerSnap : MonoBehaviour
{
    public Gate gate;

    void Start()
    {
        if (!gate || !gate.grid) return;
        transform.position = gate.grid.CellCenterWorld(gate.gateCell.x, gate.gateCell.y);
    }
}
