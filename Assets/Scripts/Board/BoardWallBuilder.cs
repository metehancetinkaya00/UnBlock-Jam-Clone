using System;
using System.Collections.Generic;
using UnityEngine;

public class BoardWallBuilder : MonoBehaviour
{
    public enum EdgeSide { North, South, East, West }

    [Serializable]
    public class GateSlot
    {
        public Vector2Int cell;
        public EdgeSide side;

        public GameObject gatePrefabOverride;
        public Vector3 gateModelEulerFix = Vector3.zero;
        public float extraPush = 0f;
        public BlockColor acceptsColor = BlockColor.White;
    }

    [Header("Refs")]
    public GridManager grid;

    [Header("Wall Prefabs")]
    public GameObject wallStraightPrefab;
    public GameObject wallCornerPrefab;

    [Header("Gate Prefab (Default)")]
    public GameObject defaultGatePrefab;

    [Header("Gate Slots")]
    public GateSlot[] gateSlots;

    [Header("Placement")]
    public float wallHeightZ = 0f;
    public float wallThickness = 0.02f;
    public float zRotationOffset = 0f;

    [Header("Corner")]
    public Vector3 cornerModelEulerFix = Vector3.zero;
    public float cornerAngle_NW = -180f;
    public float cornerAngle_NE = 90f;
    public float cornerAngle_SE = 0f;
    public float cornerAngle_SW = -90f;

    [Header("Build Options")]
    public bool buildOnStart = false;
    public bool clearOnBuild = true;

    [Tooltip("TRUE: If there is a slot, tries to spawn the gate without checking the boundary condition (for debug).")]
    public bool forceSpawnGates = true;

    [Tooltip("TRUE: Logs why it didn't spawn for each slot.")]
    public bool verboseGateLogs = true;

    [Header("Generated Parent")]
    public Transform wallsParent;

    Dictionary<(Vector2Int, EdgeSide), GateSlot> gateMap;

    void Start()
    {
        if (buildOnStart) BuildWalls();
    }

    [ContextMenu("Build Walls + Gates")]
    public void BuildWalls()
    {
        if (!grid) grid = GetComponent<GridManager>();
        if (!grid) { Debug.LogError("BoardWallBuilder: GridManager missing."); return; }

        if (!wallStraightPrefab)
        {
            Debug.LogError("BoardWallBuilder: wallStraightPrefab missing.");
            return;
        }

        BuildGateMap();

        if (wallsParent == null)
        {
            var go = new GameObject("Walls");
            go.transform.SetParent(transform, false);
            wallsParent = go.transform;
        }

        wallsParent.localScale = Vector3.one;

        if (clearOnBuild)
        {
            for (int i = wallsParent.childCount - 1; i >= 0; i--)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(wallsParent.GetChild(i).gameObject);
                else Destroy(wallsParent.GetChild(i).gameObject);
#else
                Destroy(wallsParent.GetChild(i).gameObject);
#endif
            }
        }

        int w = grid.columns;
        int h = grid.rows;
        float cs = grid.cellSize;

        int straightCount = 0;
        int cornerCount = 0;

        int gateRequests = 0;
        int gateSpawned = 0;

       
        if (gateMap != null)
        {
            foreach (var kv in gateMap)
            {
                gateRequests++;

                var cell = kv.Key.Item1;
                var side = kv.Key.Item2;
                var slot = kv.Value;

                bool cellValid = grid.IsValidCell(cell.x, cell.y);
                if (!cellValid)
                {
                    if (verboseGateLogs)
                        Debug.LogWarning($"[GATE] SKIP: cell invalid in mask: cell={cell} side={side}");
                    continue;
                }

                bool boundary = IsBoundaryEdge(cell.x, cell.y, side);
                if (!boundary && !forceSpawnGates)
                {
                    if (verboseGateLogs)
                        Debug.LogWarning($"[GATE] SKIP: not boundary (neighbor is valid): cell={cell} side={side}. Set forceSpawnGates=true to override.");
                    continue;
                }

                GameObject prefab = slot.gatePrefabOverride ? slot.gatePrefabOverride : defaultGatePrefab;
                if (!prefab)
                {
                    Debug.LogError($"[GATE] FAIL: prefab missing. Assign defaultGatePrefab or slot override. cell={cell} side={side}");
                    continue;
                }

                Vector3 c = grid.CellCenterWorld(cell.x, cell.y);
                c.z += wallHeightZ;

                Vector3 edgePos = c + EdgeOffset(side, cs * 0.5f);
                float edgeZRot = EdgeAngleZ(side);

                bool ok = SpawnGate(prefab, slot, cell, side, edgePos, edgeZRot);
                if (ok) gateSpawned++;
            }
        }

        
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (!grid.IsValidCell(x, y)) continue;

                Vector2Int cell = new Vector2Int(x, y);

                Vector3 c = grid.CellCenterWorld(x, y);
                c.z += wallHeightZ;

                // North
                if (!grid.IsValidCell(x, y + 1))
                {
                    if (!HasGateSlot(cell, EdgeSide.North) || (!IsBoundaryEdge(x, y, EdgeSide.North) && !forceSpawnGates))
                    {
                        SpawnStraight(c + new Vector3(0f, cs * 0.5f, 0f), 0f);
                        straightCount++;
                    }
                }

                // South
                if (!grid.IsValidCell(x, y - 1))
                {
                    if (!HasGateSlot(cell, EdgeSide.South) || (!IsBoundaryEdge(x, y, EdgeSide.South) && !forceSpawnGates))
                    {
                        SpawnStraight(c + new Vector3(0f, -cs * 0.5f, 0f), 180f);
                        straightCount++;
                    }
                }

                // East
                if (!grid.IsValidCell(x + 1, y))
                {
                    if (!HasGateSlot(cell, EdgeSide.East) || (!IsBoundaryEdge(x, y, EdgeSide.East) && !forceSpawnGates))
                    {
                        SpawnStraight(c + new Vector3(cs * 0.5f, 0f, 0f), 90f);
                        straightCount++;
                    }
                }

                // West
                if (!grid.IsValidCell(x - 1, y))
                {
                    if (!HasGateSlot(cell, EdgeSide.West) || (!IsBoundaryEdge(x, y, EdgeSide.West) && !forceSpawnGates))
                    {
                        SpawnStraight(c + new Vector3(-cs * 0.5f, 0f, 0f), -90f);
                        straightCount++;
                    }
                }

                if (wallCornerPrefab)
                    cornerCount += SpawnCorners(x, y, c, cs);
            }
        }

        Debug.Log($"BoardWallBuilder: gates requested={gateRequests}, gates spawned={gateSpawned}, straight={straightCount}, corners={cornerCount}");
    }

    void BuildGateMap()
    {
        gateMap = new Dictionary<(Vector2Int, EdgeSide), GateSlot>();

        if (gateSlots == null || gateSlots.Length == 0) return;

        for (int i = 0; i < gateSlots.Length; i++)
        {
            var s = gateSlots[i];
            var key = (s.cell, s.side);

            if (gateMap.ContainsKey(key))
                Debug.LogWarning($"[GATE] Duplicate slot: cell={s.cell} side={s.side}. Last overrides.");

            gateMap[key] = s;
        }
    }

    bool HasGateSlot(Vector2Int cell, EdgeSide side)
    {
        return gateMap != null && gateMap.ContainsKey((cell, side));
    }

    bool SpawnGate(GameObject prefab, GateSlot slot, Vector2Int cell, EdgeSide side, Vector3 pos, float edgeZRot)
    {
        // Wrapper
        var wrapper = new GameObject($"Gate_{cell.x}_{cell.y}_{side}");
        wrapper.transform.SetParent(wallsParent, false);
        wrapper.transform.position = pos;
        wrapper.transform.localScale = Vector3.one;

        Quaternion edgeRot = Quaternion.Euler(0f, 0f, edgeZRot);
        wrapper.transform.rotation = edgeRot;

        GameObject child;
        try
        {
            child = Instantiate(prefab, wrapper.transform);
        }
        catch (Exception e)
        {
            Debug.LogError($"[GATE] Instantiate failed: cell={cell} side={side}. Exception: {e}");
            return false;
        }

        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.Euler(slot.gateModelEulerFix);

        // outward push
        Vector3 outward = (edgeRot * Vector3.up).normalized;
        float push = wallThickness + slot.extraPush + GetHalfExtentAlong(child, outward);
        wrapper.transform.position += outward * push;

        // Set Gate script if exists
        var gate = child.GetComponentInChildren<Gate>();
        if (gate != null)
        {
            gate.grid = grid;
            gate.gateCell = cell;
            gate.acceptsColor = slot.acceptsColor;
        }
        else if (verboseGateLogs)
        {
            Debug.Log($"[GATE] Spawned but no Gate component found in prefab (ok if visual only). cell={cell} side={side}");
        }

        if (verboseGateLogs)
            Debug.Log($"[GATE] SPAWN OK: cell={cell} side={side} at {wrapper.transform.position}");

        return true;
    }

    bool IsBoundaryEdge(int x, int y, EdgeSide side)
    {
        switch (side)
        {
            case EdgeSide.North: return !grid.IsValidCell(x, y + 1);
            case EdgeSide.South: return !grid.IsValidCell(x, y - 1);
            case EdgeSide.East: return !grid.IsValidCell(x + 1, y);
            case EdgeSide.West: return !grid.IsValidCell(x - 1, y);
        }
        return false;
    }

    static Vector3 EdgeOffset(EdgeSide side, float halfCell)
    {
        switch (side)
        {
            case EdgeSide.North: return new Vector3(0f, +halfCell, 0f);
            case EdgeSide.South: return new Vector3(0f, -halfCell, 0f);
            case EdgeSide.East: return new Vector3(+halfCell, 0f, 0f);
            case EdgeSide.West: return new Vector3(-halfCell, 0f, 0f);
        }
        return Vector3.zero;
    }

    static float EdgeAngleZ(EdgeSide side)
    {
        switch (side)
        {
            case EdgeSide.North: return 0f;
            case EdgeSide.South: return 180f;
            case EdgeSide.East: return 90f;
            case EdgeSide.West: return -90f;
        }
        return 0f;
    }

    void SpawnStraight(Vector3 pos, float zRot)
    {
        var wrapper = new GameObject("WallStraight");
        wrapper.transform.SetParent(wallsParent, false);
        wrapper.transform.position = pos;
        wrapper.transform.localScale = Vector3.one;

        Quaternion edgeRot = Quaternion.Euler(0f, 0f, zRot + zRotationOffset);
        wrapper.transform.rotation = edgeRot;

        var child = Instantiate(wallStraightPrefab, wrapper.transform);
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;

        Vector3 outward = (edgeRot * Vector3.up).normalized;
        float push = wallThickness + GetHalfExtentAlong(child, outward);
        wrapper.transform.position += outward * push;
    }

    int SpawnCorners(int x, int y, Vector3 cellCenter, float cs)
    {
        int count = 0;

        if (!grid.IsValidCell(x, y + 1) && !grid.IsValidCell(x - 1, y))
        {
            SpawnCorner(cellCenter + new Vector3(-cs * 0.5f, cs * 0.5f, 0f), cornerAngle_NW);
            count++;
        }
        if (!grid.IsValidCell(x, y + 1) && !grid.IsValidCell(x + 1, y))
        {
            SpawnCorner(cellCenter + new Vector3(cs * 0.5f, cs * 0.5f, 0f), cornerAngle_NE);
            count++;
        }
        if (!grid.IsValidCell(x, y - 1) && !grid.IsValidCell(x + 1, y))
        {
            SpawnCorner(cellCenter + new Vector3(cs * 0.5f, -cs * 0.5f, 0f), cornerAngle_SE);
            count++;
        }
        if (!grid.IsValidCell(x, y - 1) && !grid.IsValidCell(x - 1, y))
        {
            SpawnCorner(cellCenter + new Vector3(-cs * 0.5f, -cs * 0.5f, 0f), cornerAngle_SW);
            count++;
        }

        return count;
    }

    void SpawnCorner(Vector3 pos, float cornerZAngle)
    {
        var wrapper = new GameObject("WallCorner");
        wrapper.transform.SetParent(wallsParent, false);
        wrapper.transform.position = pos;
        wrapper.transform.localScale = Vector3.one;

        Quaternion rot = Quaternion.Euler(0f, 0f, cornerZAngle);
        wrapper.transform.rotation = rot;

        var child = Instantiate(wallCornerPrefab, wrapper.transform);
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.Euler(cornerModelEulerFix);

        Vector3 outward = (rot * Vector3.up).normalized;
        float push = wallThickness + GetHalfExtentAlong(child, outward);
        wrapper.transform.position += outward * push;
    }

    float GetHalfExtentAlong(GameObject go, Vector3 worldDir)
    {
        var r = go.GetComponentInChildren<Renderer>();
        if (r == null) return 0f;
        Vector3 e = r.bounds.extents;
        return Mathf.Abs(worldDir.x) * e.x + Mathf.Abs(worldDir.y) * e.y + Mathf.Abs(worldDir.z) * e.z;
    }
}
