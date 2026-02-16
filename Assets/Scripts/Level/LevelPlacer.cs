using UnityEngine;

public class LevelPlacer : MonoBehaviour
{
    public GridManager grid;

    [System.Serializable]
    public class Entry
    {
        public GridBlock block;
        public Vector2Int anchorCell;
        public bool autoFindIfFailed = true;
    }

    public Entry[] entries;

    void Start()
    {
        if (!grid) { Debug.LogError("LevelPlacer: Grid not assigned."); return; }

        for (int i = 0; i < entries.Length; i++)
        {
            var e = entries[i];
            if (!e.block) continue;

            Vector2Int a = grid.ClampAnchor(e.block, e.anchorCell);

            if (grid.CanPlace(e.block, a))
            {
                grid.Place(e.block, a);
                continue;
            }

            if (e.autoFindIfFailed)
            {
                if (TryFindAnyValidPlacement(e.block, out Vector2Int found))
                {
                    grid.Place(e.block, found);
                   
                    // Debug.LogWarning($"Block placement auto-fixed: {e.block.name} -> {found}");
                }
                else
                {
                    Debug.LogWarning($"Block placement failed (no valid spot): {e.block.name}");
                }
            }
            else
            {
                Debug.LogWarning($"Block placement failed: {e.block.name} (anchor={a})");
            }
        }
    }

    bool TryFindAnyValidPlacement(GridBlock block, out Vector2Int result)
    {
        for (int y = 0; y < grid.rows; y++)
        {
            for (int x = 0; x < grid.columns; x++)
            {
                Vector2Int a = grid.ClampAnchor(block, new Vector2Int(x, y));
                if (grid.CanPlace(block, a))
                {
                    result = a;
                    return true;
                }
            }
        }

        result = default;
        return false;
    }
}
