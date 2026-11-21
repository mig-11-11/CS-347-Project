using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurfaceMaterial : MonoBehaviour
{
    public enum MaterialType { Wood, Stone, Metal }
    public enum StructureRole { Platform, Object }

    [Header("Surface Classification")]
    [SerializeField] private MaterialType materialType = MaterialType.Stone;
    [SerializeField] private StructureRole role = StructureRole.Platform;

    [Header("Wood Platform Durability")]
    [SerializeField] private int woodPlatformMaxHits = 3;
    [SerializeField] private Color hit1Tint = new Color(1.0f, 0.55f, 0.55f, 1f);
    [SerializeField] private Color hit2Tint = new Color(0.75f, 0.15f, 0.15f, 1f);

    [Header("Dynamic Object Knockback")]
    [SerializeField] private float stoneImpactForce = 7.5f;
    [SerializeField] private float metalImpactForce = 3.0f;

    public MaterialType Material => materialType;
    public StructureRole Role => role;

    private int currentHits = 0;
    private Renderer[] renderers;
    private MaterialPropertyBlock propBlock;
    private Rigidbody rb;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP Lit
    private static readonly int ColorId = Shader.PropertyToID("_Color");         // Standard / Built-in

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();
        rb = GetComponent<Rigidbody>();

        if (role == StructureRole.Platform && rb != null)
        {
            // Platforms never get knocked around by impacts
            rb.isKinematic = true;
        }
    }

    /// <summary>
    /// Called by BouncySpell when the fireball strikes this surface.
    /// </summary>
    public void OnFireballImpact(Vector3 hitPoint, Vector3 incomingVelocity)
    {
        if (role == StructureRole.Platform)
        {
            HandlePlatformHit();
        }
        else
        {
            HandleObjectHit(hitPoint, incomingVelocity);
        }
    }

    private void HandlePlatformHit()
    {
        if (materialType != MaterialType.Wood) return;

        currentHits++;

        if (currentHits >= woodPlatformMaxHits)
        {
            // Destroy after BouncySpell finishes triggering the explosion on this frame
            Destroy(gameObject, 0.02f);
        }
        else if (currentHits == 1)
        {
            ApplyTint(hit1Tint);
        }
        else if (currentHits == 2)
        {
            ApplyTint(hit2Tint);
        }
    }

    private void HandleObjectHit(Vector3 hitPoint, Vector3 incomingVelocity)
    {
        if (materialType == MaterialType.Wood)
        {
            // Destroyed upon contact in an explosion
            Destroy(gameObject, 0.02f);
            return;
        }

        if (rb == null || rb.isKinematic) return;

        Vector3 pushDir = incomingVelocity.sqrMagnitude > 0.01f
            ? incomingVelocity.normalized
            : (transform.position - hitPoint).normalized;
        pushDir.z = 0f;
        pushDir.Normalize();

        float impulse = (materialType == MaterialType.Metal) ? metalImpactForce : stoneImpactForce;
        rb.AddForceAtPosition(pushDir * impulse, hitPoint, ForceMode.Impulse);
    }

    private void ApplyTint(Color tint)
    {
        if (renderers == null) return;

        foreach (Renderer rend in renderers)
        {
            if (rend == null) continue;
            rend.GetPropertyBlock(propBlock);
            propBlock.SetColor(BaseColorId, tint);
            propBlock.SetColor(ColorId, tint);
            rend.SetPropertyBlock(propBlock);
        }
    }
}
