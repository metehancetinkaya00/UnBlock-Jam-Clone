using UnityEngine;

[ExecuteAlways]
public class BoardMeshBuilder : MonoBehaviour
{
    [Header("Refs")]
    public GridManager grid;

    [Header("Tile Prefab")]
    public GameObject tilePrefab;

    [Header("Visual")]
    public float tileThickness = 0.01f;
    public Vector3 tileOffset = Vector3.zero;

    [Header("Build Options")]
    public bool rebuildOnStart = true;
    public bool clearChildrenOnBuild = true;

    [Header("Generated Parent")]
    public Transform tilesParent;

    void Start()
    {
        if (Application.isPlaying && rebuildOnStart)
            Build();
    }

    [ContextMenu("Build Board Tiles")]
    public void Build()
    {
        if (!grid) grid = GetComponent<GridManager>();
        if (!grid) { Debug.LogError("No GridManager."); return; }
        if (!tilePrefab) { Debug.LogError("No tilePrefab."); return; }

        if (tilesParent == null)
        {
            var go = new GameObject("Tiles");
            go.transform.SetParent(transform, false);
            tilesParent = go.transform;
        }

        if (clearChildrenOnBuild)
        {
            for (int i = tilesParent.childCount - 1; i >= 0; i--)
            {
                var c = tilesParent.GetChild(i);
                if (Application.isPlaying) Destroy(c.gameObject);
                else DestroyImmediate(c.gameObject);
            }
        }

        int w = grid.columns;
        int h = grid.rows;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (!grid.IsValidCell(x, y)) continue;

                Vector3 center = grid.CellCenterWorld(x, y) + tileOffset;

                GameObject tile = Instantiate(tilePrefab, tilesParent);
                tile.name = $"Tile_{x}_{y}";
                tile.transform.position = center;

            
                float s = grid.cellSize;
                Vector3 localScale = tile.transform.localScale;
                localScale.x *= s;
                localScale.y *= s;
                localScale.z = Mathf.Max(localScale.z, tileThickness);
                tile.transform.localScale = localScale;
            }
        }
    }
}
