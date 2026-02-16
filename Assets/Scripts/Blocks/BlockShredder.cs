using UnityEngine;

public class BlockShredder : MonoBehaviour
{
    [Header("Voxel chunk look")]
    public float cubeSize = 0.12f;
    public float spawnRate = 30f;     // how many cubes per second
    public float lifetime = 1.1f;

    [Header("Physics")]
    public float force = 2.5f;
    public float spread = 0.35f;
    public float upward = 0.4f;

    [Header("Emit")]
    public Transform emitPoint;       // gate mouth
    public Material overrideMaterial; // empty = use the block's material

    float acc;

    public void Tick(float dt, MeshRenderer sourceRenderer)
    {
        acc += dt * spawnRate;
        int n = Mathf.FloorToInt(acc);
        if (n <= 0) return;
        acc -= n;

        for (int i = 0; i < n; i++)
            SpawnOne(sourceRenderer);
    }

    void SpawnOne(MeshRenderer sourceRenderer)
    {
        Vector3 p = emitPoint ? emitPoint.position : transform.position;

        GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        piece.transform.position = p + new Vector3(
            Random.Range(-cubeSize, cubeSize),
            Random.Range(-cubeSize, cubeSize),
            Random.Range(-cubeSize, cubeSize)
        );
        piece.transform.localScale = Vector3.one * cubeSize;

        var mr = piece.GetComponent<MeshRenderer>();
        if (overrideMaterial != null)
            mr.material = overrideMaterial;
        else if (sourceRenderer != null && sourceRenderer.sharedMaterial != null)
            mr.material = sourceRenderer.sharedMaterial;

        Rigidbody rb = piece.AddComponent<Rigidbody>();
        rb.mass = cubeSize;

        Vector3 dir = (Vector3.up * upward) + new Vector3(
            Random.Range(-spread, spread),
            Random.Range(-spread, spread),
            0f
        );

        rb.AddForce(dir.normalized * force, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);

        Destroy(piece, lifetime);
    }
}
