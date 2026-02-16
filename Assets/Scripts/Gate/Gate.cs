using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Gate : MonoBehaviour
{
    public static readonly List<Gate> All = new List<Gate>();

    [Header("Gate Settings")]
    public BlockColor acceptsColor = BlockColor.White;
    public GridManager grid;
    public Vector2Int gateCell = new Vector2Int(0, 0);

    [Header("Move Into Gate (Optional)")]
    public Transform entryPoint;
    public Transform endPoint;
    public float moveIntoGateTime = 0.55f;

    [Header("Grind Feel (Optional)")]
    public float jitter = 0.04f;

    [Header("Shred (Optional)")]
    public BlockShredder shredder;

    [Header("Audio (From Old Code)")]
    public AudioSource audioSource;
    public AudioClip grinderClip;

    [Header("Trigger Object Movement (Your Feature)")]
    public Transform triggerObject;

    public enum MoveAxis { X, Y, Z }
    public enum MoveDirection { Negative = -1, Positive = 1 }

    [Tooltip("Which axis the triggerObject will move along.")]
    public MoveAxis moveAxis = MoveAxis.Z;

    [Tooltip("Direction along the selected axis.")]
    public MoveDirection moveDirection = MoveDirection.Positive;

    [Tooltip("How far the triggerObject moves along the selected axis.")]
    public float moveAmount = 0.25f;

    [Tooltip("If true, uses localPosition. If false, uses world position.")]
    public bool useLocalPosition = true;

    [Tooltip("Seconds to move to the pushed position.")]
    public float moveDownTime = 0.10f;

    [Tooltip("Optional hold time at the pushed position.")]
    public float bottomHoldTime = 0.00f;

    [Tooltip("Seconds to return to the start position.")]
    public float moveUpTime = 0.12f;

    private Coroutine triggerRoutine;

    private void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    public bool BlockCoversGateCell(GridBlock b)
    {
        for (int dx = 0; dx < b.size.x; dx++)
        {
            for (int dy = 0; dy < b.size.y; dy++)
            {
                int x = b.anchorCell.x + dx;
                int y = b.anchorCell.y + dy;
                if (x == gateCell.x && y == gateCell.y)
                    return true;
            }
        }
        return false;
    }

    public static void TryConsumeIfOnGate(GridBlock block)
    {
        if (!block) return;

    
        if (block.GetComponent<GateConsumeLock>() != null) return;

        for (int i = 0; i < All.Count; i++)
        {
            Gate g = All[i];
            if (!g || !g.grid) continue;
            if (g.acceptsColor != block.color) continue;
            if (!g.BlockCoversGateCell(block)) continue;

            block.gameObject.AddComponent<GateConsumeLock>();

        
            g.TriggerObjectMove();

           
            g.StartCoroutine(g.MoveIntoGateAndDestroy(block));
            return;
        }
    }

    private void TriggerObjectMove()
    {
        if (triggerObject == null) return;

        if (triggerRoutine != null)
            StopCoroutine(triggerRoutine);

        triggerRoutine = StartCoroutine(CoMoveDownAndBack(triggerObject));
    }

    private IEnumerator CoMoveDownAndBack(Transform t)
    {
        Vector3 startPos = useLocalPosition ? t.localPosition : t.position;

        float a = Mathf.Abs(moveAmount);
        float dir = (float)moveDirection;

        Vector3 delta = Vector3.zero;
        switch (moveAxis)
        {
            case MoveAxis.X: delta = new Vector3(dir * a, 0f, 0f); break;
            case MoveAxis.Y: delta = new Vector3(0f, dir * a, 0f); break;
            case MoveAxis.Z: delta = new Vector3(0f, 0f, dir * a); break;
        }

        Vector3 downPos = startPos + delta;

      
        if (moveDownTime <= 0f)
        {
            if (useLocalPosition) t.localPosition = downPos;
            else t.position = downPos;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < moveDownTime)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.Clamp01(elapsed / moveDownTime);
                Vector3 p = Vector3.Lerp(startPos, downPos, k);

                if (useLocalPosition) t.localPosition = p;
                else t.position = p;

                yield return null;
            }
        }

        if (bottomHoldTime > 0f)
            yield return new WaitForSeconds(bottomHoldTime);

     
        if (moveUpTime <= 0f)
        {
            if (useLocalPosition) t.localPosition = startPos;
            else t.position = startPos;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < moveUpTime)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.Clamp01(elapsed / moveUpTime);
                Vector3 p = Vector3.Lerp(downPos, startPos, k);

                if (useLocalPosition) t.localPosition = p;
                else t.position = p;

                yield return null;
            }
        }

        if (useLocalPosition) t.localPosition = startPos;
        else t.position = startPos;

        triggerRoutine = null;
    }

    private IEnumerator MoveIntoGateAndDestroy(GridBlock block)
    {
        if (grid) grid.Clear(block);

      
        if (audioSource && grinderClip)
            audioSource.PlayOneShot(grinderClip, 1f);

        MeshRenderer blockMr = block.GetComponentInChildren<MeshRenderer>();
        Vector3 startPos = block.transform.position;

       
        if (entryPoint != null)
        {
            Vector3 p0 = block.transform.position;
            Vector3 p1 = entryPoint.position;
            float alignT = 0f;

            while (alignT < 1f)
            {
                alignT += Time.deltaTime / 0.12f;
                block.transform.position = Vector3.Lerp(p0, p1, alignT);
                yield return null;
            }

            startPos = entryPoint.position;
        }


        Vector3 targetPos;
        if (endPoint != null) targetPos = endPoint.position;
        else
        {
            float dist = (grid != null) ? grid.cellSize * 1.6f : 1.6f;
            targetPos = startPos + (-transform.up) * dist;
        }

        float t = 0f;
        float dur = Mathf.Max(0.05f, moveIntoGateTime);

        while (t < 1f)
        {
            t += Time.deltaTime / dur;

            Vector3 p = Vector3.Lerp(startPos, targetPos, t);

            float jx = (Mathf.PerlinNoise(Time.time * 40f, 0f) - 0.5f) * 2f * jitter;
            float jy = (Mathf.PerlinNoise(0f, Time.time * 40f) - 0.5f) * 2f * jitter;

            block.transform.position = p + new Vector3(jx, jy, 0f);

            if (shredder != null)
                shredder.Tick(Time.deltaTime, blockMr);

            yield return null;
        }

        Destroy(block.gameObject);
    }
}
