using UnityEngine;

public enum MoveAxis
{
    Free,
    Horizontal,
    Vertical
}

public class BlockMoveRule : MonoBehaviour
{
    [Header("Movement")]
    public MoveAxis axis = MoveAxis.Free;

    [Header("Optional Auto Rule")]
    [Tooltip("If enabled, automatically selects axis based on size (2x1 -> Horizontal, 1xN -> Vertical etc.)")]
    public bool autoAxisFromSize = false;

    public MoveAxis ResolveAxis(Vector2Int size)
    {
        if (!autoAxisFromSize) return axis;

        if (size.x > 1 && size.y == 1) return MoveAxis.Horizontal;
        if (size.y > 1 && size.x == 1) return MoveAxis.Vertical;

        // square / large area -> free
        return MoveAxis.Free;
    }
}
