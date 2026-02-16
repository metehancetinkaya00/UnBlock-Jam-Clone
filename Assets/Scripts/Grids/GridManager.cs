using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid")]
    public int columns = 6;
    public int rows = 6;
    public float cellSize = 1f;

    [Header("Origin (bottom-left of grid)")]
    public Transform boardOrigin;

    [Header("Board Shape Mask")]
    [Tooltip("True = cell exists, False = hole/outside. Length must be columns*rows.")]
    public bool[] validMask;

    private GridBlock[,] occupancy;

    void Awake()
    {
        if (!boardOrigin) boardOrigin = transform;
        occupancy = new GridBlock[columns, rows];
        EnsureMask();
    }

    void EnsureMask()
    {
        int need = columns * rows;
        if (validMask == null || validMask.Length != need)
        {
            validMask = new bool[need];
            for (int i = 0; i < need; i++) validMask[i] = true;
        }
    }

    public bool InBounds(int x, int y)
    {
        return x >= 0 && y >= 0 && x < columns && y < rows;
    }

    public bool IsValidCell(int x, int y)
    {
        if (!InBounds(x, y)) return false;
        EnsureMask();
        int idx = y * columns + x;
        return validMask[idx];
    }

    public Vector3 CellCenterWorld(int x, int y)
    {
        return boardOrigin.position + new Vector3((x + 0.5f) * cellSize, (y + 0.5f) * cellSize, 0f);
    }

    public Vector2Int WorldToCell(Vector3 world)
    {
        Vector3 local = world - boardOrigin.position;
        int x = Mathf.FloorToInt(local.x / cellSize);
        int y = Mathf.FloorToInt(local.y / cellSize);
        return new Vector2Int(x, y);
    }

    public void Clear(GridBlock block)
    {
        if (block == null) return;

        for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
                if (occupancy[x, y] == block)
                    occupancy[x, y] = null;
    }

    public bool CanPlace(GridBlock block, Vector2Int anchor)
    {
        if (block == null) return false;

        for (int dx = 0; dx < block.size.x; dx++)
        {
            for (int dy = 0; dy < block.size.y; dy++)
            {
                int x = anchor.x + dx;
                int y = anchor.y + dy;

                if (!IsValidCell(x, y)) return false;

                GridBlock occ = occupancy[x, y];
                if (occ != null && occ != block) return false;
            }
        }

        return true;
    }

    public void Place(GridBlock block, Vector2Int anchor)
    {
        if (block == null) return;

        anchor = ClampAnchor(block, anchor);
        if (!CanPlace(block, anchor)) return;

        Clear(block);

        for (int dx = 0; dx < block.size.x; dx++)
        {
            for (int dy = 0; dy < block.size.y; dy++)
            {
                int x = anchor.x + dx;
                int y = anchor.y + dy;
                occupancy[x, y] = block;
            }
        }

        block.anchorCell = anchor;

        Vector3 pos = CellCenterWorld(anchor.x, anchor.y) + block.CenterOffset(cellSize);
        block.transform.position = pos;
    }

    public Vector2Int ClampAnchor(GridBlock block, Vector2Int anchor)
    {
        int maxX = columns - block.size.x;
        int maxY = rows - block.size.y;
        anchor.x = Mathf.Clamp(anchor.x, 0, maxX);
        anchor.y = Mathf.Clamp(anchor.y, 0, maxY);
        return anchor;
    }

    public Vector2Int SnapWorldToAnchor(GridBlock block, Vector3 world)
    {
        Vector2Int a = WorldToCell(world);
        return ClampAnchor(block, a);
    }

    public bool TryFindReachablePlacement(
        GridBlock block,
        Vector2Int startAnchor,
        Vector2Int desiredAnchor,
        MoveAxis axisConstraint,
        out Vector2Int best)
    {
        best = startAnchor;
        if (block == null) return false;

        startAnchor = ClampAnchor(block, startAnchor);
        desiredAnchor = ClampAnchor(block, desiredAnchor);

        if (startAnchor == desiredAnchor)
        {
            best = startAnchor;
            return CanPlace(block, startAnchor);
        }

        if (axisConstraint == MoveAxis.Horizontal)
        {
            if (!ScanAxis(block, startAnchor, desiredAnchor, true, out best))
                best = startAnchor;
            return true;
        }

        if (axisConstraint == MoveAxis.Vertical)
        {
            if (!ScanAxis(block, startAnchor, desiredAnchor, false, out best))
                best = startAnchor;
            return true;
        }

        int adx = Mathf.Abs(desiredAnchor.x - startAnchor.x);
        int ady = Mathf.Abs(desiredAnchor.y - startAnchor.y);

        if (adx >= ady)
        {
            if (ScanAxis(block, startAnchor, desiredAnchor, true, out best)) return true;
            return ScanAxis(block, startAnchor, desiredAnchor, false, out best);
        }
        else
        {
            if (ScanAxis(block, startAnchor, desiredAnchor, false, out best)) return true;
            return ScanAxis(block, startAnchor, desiredAnchor, true, out best);
        }
    }

    private bool ScanAxis(
        GridBlock block,
        Vector2Int startAnchor,
        Vector2Int desiredAnchor,
        bool horizontal,
        out Vector2Int result)
    {
        result = startAnchor;

        int sx = startAnchor.x;
        int sy = startAnchor.y;

        int dx = desiredAnchor.x - sx;
        int dy = desiredAnchor.y - sy;

        int stepX = 0;
        int stepY = 0;

        if (horizontal)
        {
            if (dx == 0) return false;
            stepX = dx > 0 ? 1 : -1;
        }
        else
        {
            if (dy == 0) return false;
            stepY = dy > 0 ? 1 : -1;
        }

        Vector2Int lastGood = startAnchor;
        int steps = horizontal ? Mathf.Abs(dx) : Mathf.Abs(dy);

        for (int i = 1; i <= steps; i++)
        {
            Vector2Int a = new Vector2Int(sx + stepX * i, sy + stepY * i);
            a = ClampAnchor(block, a);

            if (a == lastGood) break;

            if (!CanPlace(block, a))
                break;

            lastGood = a;
        }

        result = lastGood;
        return true;
    }

    public void RebuildOccupancyFromScene()
    {
        if (occupancy == null || occupancy.GetLength(0) != columns || occupancy.GetLength(1) != rows)
            occupancy = new GridBlock[columns, rows];

        for (int x = 0; x < columns; x++)
            for (int y = 0; y < rows; y++)
                occupancy[x, y] = null;

        GridBlock[] blocks = FindObjectsOfType<GridBlock>();

        for (int i = 0; i < blocks.Length; i++)
        {
            GridBlock b = blocks[i];
            if (b == null) continue;

          
            Vector3 center = b.transform.position;

           
            Vector3 anchorWorld = center - b.CenterOffset(cellSize);
            Vector2Int a = WorldToCell(anchorWorld);
            a = ClampAnchor(b, a);

          
            b.anchorCell = a;

            for (int dx = 0; dx < b.size.x; dx++)
            {
                for (int dy = 0; dy < b.size.y; dy++)
                {
                    int cx = a.x + dx;
                    int cy = a.y + dy;

                    if (!IsValidCell(cx, cy)) continue;

              
                    if (occupancy[cx, cy] == null)
                        occupancy[cx, cy] = b;
                }
            }
        }
    }


}
