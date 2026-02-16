using UnityEngine;

public class BoardMaskPresets : MonoBehaviour
{
    public GridManager grid;

    [ContextMenu("Apply I-Shape Mask")]
    public void ApplyIShape()
    {
        if (!grid) grid = GetComponent<GridManager>();
        if (!grid) { Debug.LogError("GridManager not found"); return; }

        int w = grid.columns;
        int h = grid.rows;

        grid.validMask = new bool[w * h];
        for (int i = 0; i < grid.validMask.Length; i++) grid.validMask[i] = false;

        // Top wide: last 3 rows
        for (int y = h - 1; y >= h - 3; y--)
            for (int x = 1; x <= w - 2; x++)
                grid.validMask[y * w + x] = true;

        // Middle narrow: 3 columns in the middle
        int midL = (w / 2) - 1;
        int midR = (w / 2) + 1;
        for (int y = h - 4; y >= 3; y--)
            for (int x = midL; x <= midR; x++)
                grid.validMask[y * w + x] = true;

        // Bottom wide: first 3 rows
        for (int y = 2; y >= 0; y--)
            for (int x = 1; x <= w - 2; x++)
                grid.validMask[y * w + x] = true;

        Debug.Log("I-Shape mask applied.");
    }
}
