using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class RocketProjectile : MonoBehaviour
{
    [Header("Explosion")]
    public GameObject explosionPrefab;
    public float explosionRadius = 200f;
    public float explosionForce = 500f;
    public float damage = 50f;
    public LayerMask damageMask = ~0;

    [Header("Lifetime")]
    public float lifeTime = 6f;

    private Rigidbody rb;
    private bool hasExploded = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    public void Launch(Vector3 direction, float speed)
    {
        rb.linearVelocity = direction.normalized * speed;
        transform.rotation = Quaternion.LookRotation(direction);
    }

    private void FixedUpdate()
    {
        if (rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(rb.linearVelocity.normalized);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Explode();
    }

    private void OnTriggerEnter(Collider other)
    {
        Explode();
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            explosionRadius,
            damageMask
        );

        // handle hits:
        
        // foreach (Collider hit in hits)
        // {
        //     Health health = hit.GetComponentInParent<Health>();
        //     if (health != null)
        //     {
        //         health.TakeDamage(damage);
        //     }
        //
        //     Rigidbody targetRb = hit.attachedRigidbody;
        //     if (targetRb != null)
        //     {
        //         targetRb.AddExplosionForce(
        //             explosionForce,
        //             transform.position,
        //             explosionRadius
        //         );
        //     }
        // }

        Destroy(gameObject);
    }
}
