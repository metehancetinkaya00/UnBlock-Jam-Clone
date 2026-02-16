using UnityEngine;

public class GridBlock : MonoBehaviour
{
    [Header("Identity")]
    public BlockColor color = BlockColor.White;

    [Header("Grid Size (in cells)")]
public Vector2Int size = new Vector2Int(1, 1);


    [HideInInspector] public Vector2Int anchorCell; 

    public Vector3 CenterOffset(float cellSize)
    {
        float ox = (size.x - 1) * 0.5f * cellSize;
        float oy = (size.y - 1) * 0.5f * cellSize;
        return new Vector3(ox, oy, 0f);
    }
}
