using UnityEngine;

public class FillBoardAlternating : MonoBehaviour
{
    [Header("Refs")]
    public GridManager grid;

    [Header("Prefabs")]
    public GameObject blockPrefab;
    public GameObject whitePrefab;
    public GameObject blackPrefab;

    [Header("Parent")]
    public Transform blocksParent;

    [Header("Options")]
    public bool clearBeforeFill = true;
    public bool spawnOnStart = false;

    public bool startWithWhite = true;

    void Start()
    {
        if (spawnOnStart)
            Fill();
    }

    [ContextMenu("Fill Board (Alternating)")]
    public void Fill()
    {
        if (!grid) grid = FindObjectOfType<GridManager>();
        if (!grid) { Debug.LogError("FillBoardAlternating: GridManager not found."); return; }

        if (!blocksParent)
        {
            var go = new GameObject("AutoBlocks");
            go.transform.SetParent(transform, false);
            blocksParent = go.transform;
        }

        if (clearBeforeFill)
        {
            for (int i = blocksParent.childCount - 1; i >= 0; i--)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(blocksParent.GetChild(i).gameObject);
                else Destroy(blocksParent.GetChild(i).gameObject);
#else
                Destroy(blocksParent.GetChild(i).gameObject);
#endif
            }
        }

        int spawned = 0;

        for (int y = 0; y < grid.rows; y++)
        {
            for (int x = 0; x < grid.columns; x++)
            {
                if (!grid.IsValidCell(x, y))
                    continue;

                // ? startWithWhite = true => (0,0) white
                // ? startWithWhite = false => (0,0) black
                bool isWhite = (((x + y) % 2 == 0) == startWithWhite);

                GameObject prefab =
                    isWhite ? (whitePrefab ? whitePrefab : blockPrefab)
                            : (blackPrefab ? blackPrefab : blockPrefab);

                if (!prefab)
                {
                    Debug.LogError("FillBoardAlternating: Prefab missing. Assign blockPrefab or white/black prefab.");
                    return;
                }

                var go = Instantiate(prefab, blocksParent);
                go.name = (isWhite ? "White_" : "Black_") + x + "_" + y;

                var gb = go.GetComponent<GridBlock>();
                if (!gb) gb = go.AddComponent<GridBlock>();

                gb.size = new Vector2Int(1, 1);
                gb.color = isWhite ? BlockColor.White : BlockColor.Black;
                gb.anchorCell = new Vector2Int(x, y);

                var rule = go.GetComponent<BlockMoveRule>();
                if (!rule) rule = go.AddComponent<BlockMoveRule>();
                rule.autoAxisFromSize = false;
                rule.axis = MoveAxis.Free;

                if (grid.CanPlace(gb, gb.anchorCell))
                {
                    grid.Place(gb, gb.anchorCell);
                    spawned++;
                }
                else
                {
                    Destroy(go);
                }
            }
        }

        Debug.Log($"FillBoardAlternating: spawned {spawned} blocks.");
    }
}
