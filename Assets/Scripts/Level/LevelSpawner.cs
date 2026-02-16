using UnityEngine;

public class LevelSpawner : MonoBehaviour
{
    public GridManager grid;
    public LevelDefinition level;

    [Header("Parent")]
    public Transform blocksParent;

    [Header("Options")]
    public bool spawnOnStart = true;
    public bool clearBeforeSpawn = true;

    void Start()
    {
        if (spawnOnStart) Spawn();
    }

    [ContextMenu("Spawn")]
    public void Spawn()
    {
        if (!grid) grid = FindObjectOfType<GridManager>();
        if (!grid) { Debug.LogError("LevelSpawner: GridManager missing."); return; }

        if (!level) { Debug.LogError("LevelSpawner: LevelDefinition missing."); return; }

        if (!blocksParent)
        {
            var go = new GameObject("Blocks");
            go.transform.SetParent(transform, false);
            blocksParent = go.transform;
        }

        if (clearBeforeSpawn)
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

        foreach (var b in level.blocks)
        {
            if (!b.prefab)
            {
                Debug.LogWarning($"LevelSpawner: prefab missing for {b.id}");
                continue;
            }

            var go = Instantiate(b.prefab, blocksParent);
            go.name = b.id;

            // GridBlock
            var gb = go.GetComponent<GridBlock>();
            if (!gb) gb = go.AddComponent<GridBlock>();
            gb.size = b.size;
            gb.color = b.color;
            gb.anchorCell = b.anchor;

            // Move rule
            var rule = go.GetComponent<BlockMoveRule>();
            if (!rule) rule = go.AddComponent<BlockMoveRule>();
            rule.autoAxisFromSize = false;
            rule.axis = b.moveAxis;

            // Place
            if (grid.CanPlace(gb, b.anchor))
                grid.Place(gb, b.anchor);
            else
            {
                Debug.LogWarning($"LevelSpawner: Cannot place {b.id} at {b.anchor} (mask/overlap).");
                Destroy(go);
            }
        }
    }
}
