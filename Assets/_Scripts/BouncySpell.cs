using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class BouncySpell : MonoBehaviour
{
    public enum SpellState { Guided, Bouncing }

    [Header("Guided (Magic Missile) Phase")]
    [SerializeField] private float guidedFollowSpeed = 20f;
    [SerializeField] private float maxChannelTime = 3.0f;

    [Header("Released (Bouncing) Phase")]
    [SerializeField] private float bounceSpeed = 16f;
    [SerializeField] private int maxBounces = 5;
    [SerializeField] private float maxBounceLifetime = 6f;

    [Header("Fallback Bounce Layers")]
    [SerializeField] private LayerMask fallbackBounceLayers;
    [SerializeField] private float skinWidth = 0.08f;

    [Header("Explosion & Absorb VFX")]
    [SerializeField] private float explosionRadius = 2.5f;
    [SerializeField] private float explosionForce = 500f;
    [SerializeField] private LayerMask explosionTargetLayers = ~0;

    [Tooltip("FireExplosion particle prefab here")]
    [SerializeField] private GameObject explosionVfxPrefab;

    [Tooltip("Optional small smoke/fizzle prefab when Stone absorbs the fireball")]
    [SerializeField] private GameObject absorbVfxPrefab;

    private Rigidbody rb;
    private SpellState currentState = SpellState.Guided;
    private Vector3 targetPosition;
    private Vector3 lastVelocity;
    private float channelTimer = 0f;
    private float bounceTimer = 0f;
    private int currentBounces = 0;
    private bool isConsumed = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
    }

    private void Start()
    {
        targetPosition = transform.position;
    }

    public void SetTargetPosition(Vector3 worldPos)
    {
        if (currentState != SpellState.Guided) return;
        targetPosition = new Vector3(worldPos.x, worldPos.y, 0f);
    }

    public void ReleaseSpell()
    {
        if (currentState == SpellState.Bouncing) return;
        currentState = SpellState.Bouncing;

        Vector3 launchDir = rb.velocity.sqrMagnitude > 0.1f ? rb.velocity.normalized : transform.forward;
        launchDir.z = 0f;
        if (launchDir.sqrMagnitude < 0.001f) launchDir = Vector3.right;
        launchDir.Normalize();

        rb.velocity = launchDir * bounceSpeed;
        transform.rotation = Quaternion.LookRotation(launchDir, Vector3.up);
    }

    private void FixedUpdate()
    {
        if (isConsumed) return;

        if (currentState == SpellState.Guided)
        {
            channelTimer += Time.fixedDeltaTime;
            if (channelTimer >= maxChannelTime)
            {
                ReleaseSpell();
                return;
            }

            Vector3 toTarget = targetPosition - rb.position;
            toTarget.z = 0f;

            Vector3 desiredVelocity = toTarget * guidedFollowSpeed;
            if (desiredVelocity.magnitude > guidedFollowSpeed)
            {
                desiredVelocity = desiredVelocity.normalized * guidedFollowSpeed;
            }

            rb.velocity = desiredVelocity;
            lastVelocity = desiredVelocity; // Track pure fireball velocity in Guided mode too

            if (desiredVelocity.sqrMagnitude > 0.05f)
            {
                transform.rotation = Quaternion.LookRotation(desiredVelocity.normalized, Vector3.up);
            }
        }
        else if (currentState == SpellState.Bouncing)
        {
            bounceTimer += Time.fixedDeltaTime;
            if (bounceTimer >= maxBounceLifetime)
            {
                Explode();
                return;
            }

            if (rb.velocity.sqrMagnitude > 0.01f)
            {
                Vector3 constantDir = rb.velocity.normalized;
                constantDir.z = 0f;
                rb.velocity = constantDir.normalized * bounceSpeed;
            }

            // Store the clean constant-speed velocity before any physics collision solver runs
            lastVelocity = rb.velocity;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isConsumed) return;

        ContactPoint contact = collision.contacts[0];

        // Always use pre-collision fireball velocity (ignoring moving platform velocity)
        Vector3 incomingVel = lastVelocity.sqrMagnitude > 0.001f ? lastVelocity : transform.forward * bounceSpeed;
        incomingVel.z = 0f;

        // 1. Direct Skeleton Hit -> Explode
        SkeletonEnemy skeleton = collision.gameObject.GetComponentInParent<SkeletonEnemy>();
        if (skeleton != null)
        {
            skeleton.Die(incomingVel, contact.point);
            Explode();
            return;
        }

        // 2. Check SurfaceMaterial (Wood / Stone / Metal + Platform / Object)
        SurfaceMaterial surface = collision.gameObject.GetComponentInParent<SurfaceMaterial>();
        if (surface != null)
        {
            surface.OnFireballImpact(contact.point, incomingVel);

            switch (surface.Material)
            {
                case SurfaceMaterial.MaterialType.Wood:
                    transform.position = contact.point + contact.normal * skinWidth;
                    Explode();
                    return;

                case SurfaceMaterial.MaterialType.Stone:
                    transform.position = contact.point + contact.normal * skinWidth;
                    Absorb();
                    return;

                case SurfaceMaterial.MaterialType.Metal:
                    ReflectOffContact(contact, incomingVel);
                    return;
            }
        }

        // 3. Fallback for untagged level boundaries/walls
        int hitLayerMask = 1 << collision.gameObject.layer;
        if ((hitLayerMask & fallbackBounceLayers) != 0)
        {
            ReflectOffContact(contact, incomingVel);
        }
    }

    private void ReflectOffContact(ContactPoint contact, Vector3 incomingVel)
    {
        if (currentState == SpellState.Guided)
        {
            currentState = SpellState.Bouncing;
        }

        currentBounces++;
        if (currentBounces > maxBounces)
        {
            transform.position = contact.point + contact.normal * skinWidth;
            Explode();
            return;
        }

        // 1. Get a clean 2D normal (avoid amplifying tiny X/Y noise if contact hit a Z-bevel)
        Vector3 normal = new Vector3(contact.normal.x, contact.normal.y, 0f);
        if (normal.sqrMagnitude < 0.05f)
        {
            normal = new Vector3(rb.position.x - contact.point.x, rb.position.y - contact.point.y, 0f);
        }
        if (normal.sqrMagnitude < 0.001f)
        {
            normal = Vector3.up;
        }
        normal.Normalize();

        // 2. Use actual travel direction; if nearly hovering, use current facing direction
        Vector3 dir = incomingVel.sqrMagnitude > 0.25f ? incomingVel.normalized : transform.forward;
        dir.z = 0f;
        if (dir.sqrMagnitude < 0.001f)
        {
            dir = Vector3.right;
        }
        dir.Normalize();

        // 3. Ensure dir points INTO the surface (opposite to normal) before reflecting.
        // If a moving platform ran into a hovering/receding fireball, mirror the component along the normal
        // so it preserves its horizontal momentum instead of snapping straight down along `normal`.
        float intoSurface = Vector3.Dot(dir, normal);
        Vector3 reflectDir;
        if (intoSurface < -0.05f)
        {
            reflectDir = Vector3.Reflect(dir, normal);
        }
        else
        {
            // Keep tangent (horizontal) motion, push outward along normal at a clean angle
            Vector3 tangent = dir - normal * intoSurface;
            if (tangent.sqrMagnitude < 0.05f)
            {
                // Pure vertical hit with zero horizontal aim: deflect slightly based on contact offset
                float side = Mathf.Sign(rb.position.x - contact.point.x);
                if (Mathf.Approximately(side, 0f)) side = 1f;
                tangent = Vector3.right * side * 0.5f;
            }
            reflectDir = (tangent.normalized + normal).normalized;
        }

        reflectDir.z = 0f;
        reflectDir.Normalize();

        // Push outside the collider using the spell's sphere radius + skinWidth so it never re-clips
        float spellRadius = 0.25f;
        if (TryGetComponent<SphereCollider>(out SphereCollider sphere))
        {
            spellRadius = sphere.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        }

        Vector3 outVel = reflectDir * bounceSpeed;
        Vector3 safePos = contact.point + normal * (spellRadius + skinWidth);
        safePos.z = 0f;

        rb.position = safePos;
        transform.position = safePos;
        rb.velocity = outVel;
        lastVelocity = outVel;
        transform.rotation = Quaternion.LookRotation(reflectDir, Vector3.up);
    }

    private void Explode()
    {
        if (isConsumed) return;
        isConsumed = true;

        Vector3 origin = transform.position;

        if (explosionVfxPrefab != null)
        {
            GameObject vfx = Instantiate(explosionVfxPrefab, origin, Quaternion.identity);
            Destroy(vfx, 3f);
        }

        Collider[] hits = Physics.OverlapSphere(origin, explosionRadius, explosionTargetLayers);
        foreach (Collider hit in hits)
        {
            SkeletonEnemy enemy = hit.GetComponentInParent<SkeletonEnemy>();
            if (enemy != null)
            {
                enemy.DieFromExplosion(explosionForce, origin, explosionRadius);
            }
        }

        Destroy(gameObject);
    }

    private void Absorb()
    {
        if (isConsumed) return;
        isConsumed = true;

        if (absorbVfxPrefab != null)
        {
            GameObject vfx = Instantiate(absorbVfxPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 2f);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.45f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}