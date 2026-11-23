using UnityEngine;

public class SkeletonEnemy : MonoBehaviour
{
    [Header("Death Trigger Settings")]
    [SerializeField] private float lethalObjectImpactSpeed = 1.8f;
    [SerializeField] private float lethalFallLandingSpeed = 6.5f;
    [SerializeField] private LayerMask safeGroundLayers;
    [SerializeField] private float spawnGracePeriod = 0.25f;

    [Header("Death Effects (Smoke + Bone Burst)")]
    [Tooltip("Prefab containing the Smoke Puff and 3D Mesh Bone Burst particle systems")]
    [SerializeField] private GameObject deathVfxPrefab;
    [SerializeField] private float vfxVerticalOffset = 0.85f;
    [SerializeField] private float vfxLifetime = 3.0f;

    private Animator animator;
    private Collider rootCollider;
    private Rigidbody rootRb;
    private Renderer[] allRenderers;

    private bool isDead = false;
    public bool IsDead => isDead;

    private float spawnTimer = 0f;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        rootCollider = GetComponent<Collider>();
        rootRb = GetComponent<Rigidbody>();
        allRenderers = GetComponentsInChildren<Renderer>(true);

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        if (rootRb != null)
        {
            rootRb.constraints = RigidbodyConstraints.FreezePositionZ
                               | RigidbodyConstraints.FreezeRotationX
                               | RigidbodyConstraints.FreezeRotationY
                               | RigidbodyConstraints.FreezeRotationZ;

            rootRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        Vector3 pos = transform.position;
        pos.z = 0f;
        transform.position = pos;
    }

    private void OnEnable()
    {
        spawnTimer = spawnGracePeriod;
    }

    private void Update()
    {
        if (spawnTimer > 0f)
        {
            spawnTimer -= Time.deltaTime;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;

        // 1. Direct projectile hit always kills immediately
        if (collision.gameObject.GetComponent<BouncySpell>() != null)
        {
            Die(collision.relativeVelocity, collision.contacts[0].point);
            return;
        }

        // 2. Ignore initial physics settling on level load
        if (spawnTimer > 0f) return;

        float impactSpeed = collision.relativeVelocity.magnitude;
        Vector3 contactNormal = collision.contacts[0].normal;
        bool isSafeSurface = ((1 << collision.gameObject.layer) & safeGroundLayers) != 0;

        if (isSafeSurface)
        {
            bool landedOnTop = contactNormal.y > 0.5f;
            if (landedOnTop && impactSpeed < lethalFallLandingSpeed) return;
            if (!landedOnTop && impactSpeed < lethalObjectImpactSpeed) return;
        }
        else if (impactSpeed < lethalObjectImpactSpeed)
        {
            return;
        }

        Die(collision.relativeVelocity, collision.contacts[0].point);
    }

    public void Die(Vector3 impactVelocity, Vector3 hitPoint)
    {
        if (isDead) return;
        isDead = true;

        OnEnemyKilled(impactVelocity, hitPoint);
    }

    public void DieFromExplosion(float explosionForce, Vector3 explosionOrigin, float explosionRadius, float upwardsModifier = 0.5f)
    {
        if (isDead) return;
        isDead = true;

        Vector3 blastDir = (transform.position - explosionOrigin).normalized * Mathf.Clamp(explosionForce, 4f, 12f);
        OnEnemyKilled(blastDir, explosionOrigin);
    }

    private void OnEnemyKilled(Vector3 impactVelocity, Vector3 hitPoint)
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.RegisterSkeletonKill();
        }
        
        if (rootCollider != null) rootCollider.enabled = false;

        if (rootRb != null)
        {
            rootRb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rootRb.isKinematic = true;
            rootRb.detectCollisions = false;
        }

        // Hide the skeleton visuals immediately so the smoke + bones replace him cleanly
        if (allRenderers != null)
        {
            foreach (Renderer r in allRenderers)
            {
                if (r != null) r.enabled = false;
            }
        }

        if (deathVfxPrefab != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * vfxVerticalOffset;
            spawnPos.z = 0f;

            // Tilt the burst slightly away from the impact point in the 2.5D X-Y plane
            Vector3 awayDir = spawnPos - hitPoint;
            awayDir.z = 0f;
            Quaternion burstRot = Quaternion.identity;
            if (awayDir.sqrMagnitude > 0.01f)
            {
                float tiltAngle = Mathf.Clamp(-awayDir.normalized.x * 25f, -25f, 25f);
                burstRot = Quaternion.Euler(0f, 0f, tiltAngle);
            }

            GameObject vfx = Instantiate(deathVfxPrefab, spawnPos, burstRot);
            Destroy(vfx, vfxLifetime);
        }

        Destroy(gameObject, 0.05f);
    }
}