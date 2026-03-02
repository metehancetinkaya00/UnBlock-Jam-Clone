using System.Collections.Generic;
using UnityEngine;

public class GridDragController : MonoBehaviour
{
    [Header("References")]
    public GridManager grid;
    public LayerMask blockLayer;

    [Header("Feel")]
    public float followLerp = 25f;
    public float holdScale = 1.05f;

    [Header("Step Movement")]
    public int maxStepsPerFrame = 2;

    [Header("Release Sound (Single Clip)")]
    [SerializeField] private AudioClip releaseMoveClip;
    [Range(0f, 1f)]
    [SerializeField] private float releaseMoveVolume = 1f;

    private AudioSource audioSrc;

    private Camera cam;

    private GridBlock held;
    private BlockMoveRule heldRule;

    private Vector3 originalScale;
    private Vector2Int originalAnchor;

    private float dragPlaneZ;

    private readonly List<Vector2Int> reachable = new List<Vector2Int>(256);
    private readonly HashSet<Vector2Int> reachableSet = new HashSet<Vector2Int>();

    private Vector2Int lastValidAnchor;
    private Vector3 lastValidWorld;

    void Awake()
    {
        cam = Camera.main;
        if (!grid) grid = FindObjectOfType<GridManager>();

       
        audioSrc = GetComponent<AudioSource>();
        if (audioSrc == null) audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.playOnAwake = false;
        audioSrc.loop = false;
    }

    void Update()
    {
        if (!grid || !cam) return;

        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began) Begin(t.position);
            if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary) Move(t.position);
            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) End();
        }
        else
        {
            if (Input.GetMouseButtonDown(0)) Begin(Input.mousePosition);
            if (Input.GetMouseButton(0)) Move(Input.mousePosition);
            if (Input.GetMouseButtonUp(0)) End();
        }
    }

    void Begin(Vector2 screen)
    {
        Ray ray = cam.ScreenPointToRay(new Vector3(screen.x, screen.y, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, 300f, blockLayer)) return;

        held = hit.collider.GetComponentInParent<GridBlock>();
        if (!held) return;

        heldRule = held.GetComponent<BlockMoveRule>();

        originalScale = held.transform.localScale;
        originalAnchor = held.anchorCell;

        held.transform.localScale = originalScale * holdScale;
        dragPlaneZ = held.transform.position.z;

        grid.RebuildOccupancyFromScene();
        grid.Clear(held);

        BuildReachableRegion(originalAnchor);

        if (reachable.Count == 0)
        {
            grid.Place(held, originalAnchor);
            held.transform.localScale = originalScale;
            held = null;
            heldRule = null;
            return;
        }

        lastValidAnchor = originalAnchor;
        lastValidWorld = AnchorToWorld(lastValidAnchor);

        held.transform.position = lastValidWorld;
        held.anchorCell = lastValidAnchor;
    }

    void Move(Vector2 screen)
    {
        if (!held) return;

        Vector3 world = ScreenToWorldOnPlane(screen, dragPlaneZ);
        world.z = dragPlaneZ;

        MoveAxis axis = MoveAxis.Free;
        if (heldRule != null) axis = heldRule.ResolveAxis(held.size);

        if (axis == MoveAxis.Horizontal)
            world.y = lastValidWorld.y;
        else if (axis == MoveAxis.Vertical)
            world.x = lastValidWorld.x;

        Vector2Int desiredAnchor = grid.SnapWorldToAnchor(held, world);

    
        if (!reachableSet.Contains(desiredAnchor))
            desiredAnchor = NearestReachableToWorld(world);

        
        int steps = Mathf.Max(1, maxStepsPerFrame);

        Vector2Int cur = lastValidAnchor;
        for (int i = 0; i < steps; i++)
        {
            if (cur == desiredAnchor) break;

            Vector2Int next = GetNextStepBfs(cur, desiredAnchor);
            if (next == cur) break;

           
            if (!reachableSet.Contains(next)) break;
            if (!grid.CanPlace(held, next)) break;

            cur = next;
        }

        lastValidAnchor = cur;
        lastValidWorld = AnchorToWorld(lastValidAnchor);

        held.transform.position = Vector3.Lerp(held.transform.position, lastValidWorld, Time.deltaTime * followLerp);
        held.anchorCell = lastValidAnchor;
    }

    void End()
    {
        if (!held) return;

        held.transform.localScale = originalScale;

        grid.RebuildOccupancyFromScene();
        grid.Clear(held);

        Vector2Int finalAnchor = lastValidAnchor;
        if (!grid.CanPlace(held, finalAnchor))
            finalAnchor = originalAnchor;

 
        grid.Place(held, finalAnchor);
        Gate.TryConsumeIfOnGate(held);


        if (finalAnchor != originalAnchor)
        {
            PlayReleaseMoveSound();
        }

        held = null;
        heldRule = null;
        reachable.Clear();
        reachableSet.Clear();
    }

    private void PlayReleaseMoveSound()
    {
        if (releaseMoveClip == null) return;
        if (audioSrc == null) return;

        audioSrc.PlayOneShot(releaseMoveClip, releaseMoveVolume);
    }

    void BuildReachableRegion(Vector2Int start)
    {
        reachable.Clear();
        reachableSet.Clear();

        if (!grid.CanPlace(held, start))
            return;

        Queue<Vector2Int> q = new Queue<Vector2Int>(256);
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        q.Enqueue(start);
        visited.Add(start);

        while (q.Count > 0)
        {
            Vector2Int a = q.Dequeue();
            reachable.Add(a);
            reachableSet.Add(a);

            Vector2Int[] dirs = new[]
            {
                new Vector2Int( 1, 0),
                new Vector2Int(-1, 0),
                new Vector2Int( 0, 1),
                new Vector2Int( 0,-1),
            };

            for (int i = 0; i < dirs.Length; i++)
            {
                Vector2Int n = new Vector2Int(a.x + dirs[i].x, a.y + dirs[i].y);
                n = grid.ClampAnchor(held, n);

                if (n == a) continue;
                if (visited.Contains(n)) continue;

                if (!grid.CanPlace(held, n)) continue;

                visited.Add(n);
                q.Enqueue(n);
            }
        }
    }

    Vector2Int NearestReachableToWorld(Vector3 world)
    {
        Vector2Int best = lastValidAnchor;
        float bestD2 = float.PositiveInfinity;

        for (int i = 0; i < reachable.Count; i++)
        {
            Vector2Int a = reachable[i];
            if (!grid.CanPlace(held, a)) continue;

            Vector3 c = AnchorToWorld(a);
            float d2 = (c - world).sqrMagnitude;
            if (d2 < bestD2)
            {
                bestD2 = d2;
                best = a;
            }
        }

        return best;
    }

    Vector2Int GetNextStepBfs(Vector2Int start, Vector2Int goal)
    {
        if (start == goal) return start;

        Queue<Vector2Int> q = new Queue<Vector2Int>(256);
        Dictionary<Vector2Int, Vector2Int> parent = new Dictionary<Vector2Int, Vector2Int>(256);

        q.Enqueue(start);
        parent[start] = start;

        Vector2Int[] dirs = new[]
        {
            new Vector2Int( 1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int( 0, 1),
            new Vector2Int( 0,-1),
        };

        while (q.Count > 0)
        {
            Vector2Int cur = q.Dequeue();
            if (cur == goal) break;

            for (int i = 0; i < dirs.Length; i++)
            {
                Vector2Int n = new Vector2Int(cur.x + dirs[i].x, cur.y + dirs[i].y);
                n = grid.ClampAnchor(held, n);

                if (n == cur) continue;
                if (parent.ContainsKey(n)) continue;

                if (!reachableSet.Contains(n)) continue;
                if (!grid.CanPlace(held, n)) continue;

                parent[n] = cur;
                q.Enqueue(n);
            }
        }

        if (!parent.ContainsKey(goal))
            return start;

        Vector2Int step = goal;
        while (parent[step] != start)
            step = parent[step];

        return step;
    }

    Vector3 AnchorToWorld(Vector2Int anchor)
    {
        Vector3 w = grid.CellCenterWorld(anchor.x, anchor.y) + held.CenterOffset(grid.cellSize);
        w.z = dragPlaneZ;
        return w;
    }

    Vector3 ScreenToWorldOnPlane(Vector2 screen, float planeZ)
    {
        Ray ray = cam.ScreenPointToRay(new Vector3(screen.x, screen.y, 0f));
        Plane p = new Plane(Vector3.forward, new Vector3(0f, 0f, planeZ));

        if (p.Raycast(ray, out float enter))
            return ray.GetPoint(enter);

        return Vector3.zero;
    }
}
