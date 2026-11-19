using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
//made with ai
[RequireComponent(typeof(Rigidbody))]
public class SuperhotImpact : MonoBehaviour
{
    [Header("Throw")]
    public float lookbackSeconds = 0.15f;        // uses the fastest hand speed in this window before release
    public float throwBoost = 1.5f;              // multiplies that speed
    public float minThrowSpeed = 5f;             // slow flicks get lifted to this
    public float maxThrowSpeed = 18f;
    public float minFlick = 0.5f;                // slower than this counts as a drop, not a throw
    [Range(0f, 20f)] public float upwardAssistDegrees = 5f;   // tilts every throw slightly upward

    [Header("Flight")]
    [Range(0f, 1f)] public float flightGravityScale = 0.1f;   // only applies after a throw, until it hits something

    [Header("Feedback")]
    public AudioClip impactSound;
    [Range(0f, 1f)] public float soundVolume = 1f;
    public GameObject impactParticlePrefab;
    public float minImpactSpeed = 1f;

    [Header("Debug")]
    public bool debugThrows = false;

    struct Sample { public float time; public Vector3 vel; }

    Rigidbody rb;
    XRGrabInteractable grab;
    Shatterable shatter;
    readonly List<Sample> samples = new List<Sample>();
    Vector3 lastPos;
    Vector3 releaseVelocity;
    Vector3 lastVelocity;
    bool hasRelease;
    bool inFlight;
    bool hasImpacted;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();
        shatter = GetComponent<Shatterable>();
        lastPos = transform.position;
        grab.selectExited.AddListener(_ => OnReleased());
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        if (grab.isSelected)
        {
            samples.Add(new Sample { time = Time.time, vel = (transform.position - lastPos) / dt });
            samples.RemoveAll(s => Time.time - s.time > 0.4f);
        }
        lastPos = transform.position;
    }

    void OnReleased()
    {
        Vector3 best = Vector3.zero;
        float bestSpeed = 0f;
        foreach (var s in samples)
        {
            if (Time.time - s.time > lookbackSeconds) continue;
            float sp = s.vel.magnitude;
            if (sp > bestSpeed) { bestSpeed = sp; best = s.vel; }
        }
        samples.Clear();
        releaseVelocity = best;
        hasRelease = true;
    }

    void FixedUpdate()
    {
        if (grab.isSelected) return;

        if (hasRelease)
        {
            hasRelease = false;
            float speed = releaseVelocity.magnitude;
            if (speed > minFlick)
            {
                Vector3 dir = releaseVelocity.normalized;

                // tilt the throw upward, unless it is already nearly vertical
                Vector3 right = Vector3.Cross(Vector3.up, dir);
                if (right.sqrMagnitude > 0.01f)
                    dir = Quaternion.AngleAxis(-upwardAssistDegrees, right.normalized) * dir;

                float target = Mathf.Clamp(speed * throwBoost, minThrowSpeed, maxThrowSpeed);
                rb.linearVelocity = dir * target;
                inFlight = true;

                if (debugThrows)
                    Debug.Log($"{name}: hand {speed:F1} m/s -> throw {target:F1} m/s, pitch {Mathf.Asin(dir.y) * Mathf.Rad2Deg:F0} deg");
            }
        }

        if (inFlight && rb.linearVelocity.magnitude < 0.5f) inFlight = false;

        if (inFlight)
        {
            rb.useGravity = false;
            rb.AddForce(Physics.gravity * flightGravityScale, ForceMode.Acceleration);
        }
        else
        {
            rb.useGravity = true;   // objects resting on the table behave normally
        }

        lastVelocity = rb.linearVelocity;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasImpacted || grab.isSelected) return;
        inFlight = false;
        if (collision.relativeVelocity.magnitude < minImpactSpeed) return;
        hasImpacted = true;

        ContactPoint contact = collision.GetContact(0);

        if (impactSound != null)
            AudioSource.PlayClipAtPoint(impactSound, contact.point, soundVolume);
        if (impactParticlePrefab != null)
            Destroy(Instantiate(impactParticlePrefab, contact.point, Quaternion.LookRotation(contact.normal)), 3f);
        if (shatter != null)
            shatter.Shatter(contact.point, contact.normal, lastVelocity);

        GetComponent<Collider>().enabled = false;
        Destroy(gameObject);
    }
}