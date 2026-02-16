using UnityEngine;

[ExecuteAlways]
public class GateCellSetter : MonoBehaviour
{
    public Gate gate;
    public Transform referencePoint; 

    [ContextMenu("Set gateCell from Reference Point")]
    public void SetGateCellFromRef()
    {
        if (!gate) gate = GetComponent<Gate>();
        if (!gate || !gate.grid)
        {
            Debug.LogError("GateCellSetter: Gate or Gate.grid missing.");
            return;
        }

        Transform t = referencePoint ? referencePoint : gate.transform;
        Vector2Int cell = gate.grid.WorldToCell(t.position);

        gate.gateCell = cell;
        Debug.Log($"gateCell set to {cell} from {(referencePoint ? referencePoint.name : "Gate Transform")}");
    }
}
