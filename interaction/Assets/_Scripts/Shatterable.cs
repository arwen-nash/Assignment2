using System.Collections.Generic;
using UnityEngine;
//made with ai
public class Shatterable : MonoBehaviour
{
    [Header("Optional hand-made pieces (leave empty to auto-generate)")]
    public GameObject debrisPrefab;

    [Header("Auto-generated pieces")]
    public int minPieces = 4;
    public int maxPieces = 6;
    [Range(0.2f, 0.8f)] public float pieceScale = 0.6f;
    [Range(0f, 0.6f)] public float jaggedness = 0.35f;
    public bool sharpShards = false;   // true = pointy tetrahedron shards

    [Header("Motion (metres per second)")]
    public float scatterSpeed = 1.5f;
    [Range(0f, 1f)] public float inheritVelocity = 0.3f;
    public float maxPieceSpeed = 4f;
    public float pieceLifetime = 4f;

    public void Shatter(Vector3 hitPoint, Vector3 normal, Vector3 velocity)
    {
        var pieces = new List<Rigidbody>();
        Vector3 baseVel = Vector3.Reflect(velocity, normal) * inheritVelocity;

        if (debrisPrefab != null)
        {
            var go = Instantiate(debrisPrefab, transform.position, transform.rotation);
            go.transform.localScale = transform.lossyScale;
            pieces.AddRange(go.GetComponentsInChildren<Rigidbody>());
        }
        else
        {
            int count = Random.Range(minPieces, maxPieces + 1);   // int version excludes the max, so +1
            var rend = GetComponentInChildren<Renderer>();
            Bounds b = rend ? rend.bounds : new Bounds(transform.position, Vector3.one * 0.2f);
            Vector3 lift = normal * b.extents.magnitude * 0.3f;   // keep pieces off the surface

            for (int i = 0; i < count; i++)
            {
                var piece = new GameObject("Shard");
                var mesh = sharpShards ? ShardMesh.Tetra() : ShardMesh.JaggedBox(jaggedness);
                piece.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = piece.AddComponent<MeshRenderer>();
                if (rend) mr.sharedMaterial = rend.sharedMaterial;

                Vector3 offset = Vector3.Scale(Random.insideUnitSphere, b.extents) * 0.5f;
                piece.transform.SetPositionAndRotation(b.center + lift + offset, Random.rotation);

                // non-uniform scale so pieces differ in proportions as well as shape
                Vector3 s = b.size * pieceScale;
                piece.transform.localScale = new Vector3(
                    s.x * Random.Range(0.6f, 1.3f),
                    s.y * Random.Range(0.6f, 1.3f),
                    s.z * Random.Range(0.6f, 1.3f));

                var mc = piece.AddComponent<MeshCollider>();
                mc.sharedMesh = mesh;
                mc.convex = true;

                var rb = piece.AddComponent<Rigidbody>();
                rb.mass = 0.1f;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                pieces.Add(rb);
            }
        }

        // Pieces never collide with each other, so overlaps can't fling them
        var cols = new List<Collider>();
        foreach (var p in pieces) cols.AddRange(p.GetComponents<Collider>());
        for (int i = 0; i < cols.Count; i++)
            for (int j = i + 1; j < cols.Count; j++)
                Physics.IgnoreCollision(cols[i], cols[j]);

        foreach (var rb in pieces)
        {
            Vector3 dir = (Random.onUnitSphere + normal).normalized;
            Vector3 v = baseVel + dir * scatterSpeed * Random.Range(0.6f, 1.2f);
            rb.linearVelocity = Vector3.ClampMagnitude(v, maxPieceSpeed);
            rb.angularVelocity = Random.insideUnitSphere * 8f;

            var d = rb.gameObject.AddComponent<DebrisPiece>();
            d.lifetime = pieceLifetime;
        }
    }
}