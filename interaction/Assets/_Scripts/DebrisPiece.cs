using UnityEngine;
//made with ai
public class DebrisPiece : MonoBehaviour
{
    public float lifetime = 3f;
    public float shrinkTime = 0.5f;
    float t;
    Vector3 startScale;

    void Start() => startScale = transform.localScale;

    void Update()
    {
        t += Time.deltaTime;
        float shrinkStart = lifetime - shrinkTime;
        if (t > shrinkStart)
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, (t - shrinkStart) / shrinkTime);
        if (t >= lifetime) Destroy(gameObject);
    }

    void OnDestroy()
    {
        var mf = GetComponent<MeshFilter>();
        if (mf && mf.sharedMesh && mf.sharedMesh.name == "Shard") Destroy(mf.sharedMesh);
    }
}