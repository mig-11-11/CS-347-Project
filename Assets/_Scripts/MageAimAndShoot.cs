using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MageAimAndShoot : MonoBehaviour
{
    public enum PitchAxis { X, Y, Z }

    [Header("References")]
    [Tooltip("Mage's upper right arm bone here")]
    public Transform armBone;
    [Tooltip("SpellSpawnPoint object here")]
    public Transform spellSpawnPoint;
    [Tooltip("SpellProjectile prefab here")]
    public GameObject spellPrefab;

    [Header("Aiming Tuning")]
    public PitchAxis shoulderPitchAxis = PitchAxis.Z;
    public bool invertPitch = false;
    public float armAngleOffset = 0f;
    [Range(30f, 180f)]
    public float maxAimAngle = 75f;

    [Header("Casting Settings")]
    [Tooltip("Brief cooldown after a spell finishes before another can be cast")]
    public float recastCooldown = 0.2f;

    private Camera mainCam;
    private Quaternion initialArmLocalRot;
    private BouncySpell activeSpell;
    private bool isChanneling = false;
    private float nextAllowedCastTime = 0f;

    void Start()
    {
        mainCam = Camera.main;
        if (armBone != null)
        {
            initialArmLocalRot = armBone.localRotation;
        }
    }

    void Update()
    {
        HandleCasting();
    }

    void LateUpdate()
    {
        AimAtMouse();
    }

    void HandleCasting()
    {
        if (mainCam == null) return;

        // 1. Start casting ONLY if no spell currently exists in the scene
        if (Input.GetMouseButtonDown(0) && activeSpell == null && Time.time >= nextAllowedCastTime)
        {
            SpawnGuidedSpell();
        }

        // 2. While holding Left-Click and channeling, steer the spell to the cursor
        if (isChanneling && activeSpell != null)
        {
            if (Input.GetMouseButton(0))
            {
                if (TryGetMouseWorldPos(out Vector3 mousePos))
                {
                    activeSpell.SetTargetPosition(mousePos);
                }
            }

            // 3. Release when player lets go of Left-Click
            if (Input.GetMouseButtonUp(0))
            {
                activeSpell.ReleaseSpell();
                isChanneling = false;
                nextAllowedCastTime = Time.time + recastCooldown;
            }
        }
        else if (isChanneling && activeSpell == null)
        {
            // Reset state if the spell auto-released and destroyed itself while button was still held
            isChanneling = false;
            nextAllowedCastTime = Time.time + recastCooldown;
        }
    }

    void SpawnGuidedSpell()
    {
        if (spellPrefab == null || spellSpawnPoint == null) return;

        if (TryGetMouseWorldPos(out Vector3 mouseWorldPos))
        {
            if (LevelManager.Instance != null && !LevelManager.Instance.TryConsumeShot())
            {
                return;
            }

            Vector3 spawnPos = new Vector3(spellSpawnPoint.position.x, spellSpawnPoint.position.y, 0f);
            Vector3 shootDir = mouseWorldPos - spawnPos;
            shootDir.z = 0f;

            if (shootDir.sqrMagnitude < 0.001f) shootDir = Vector3.right;
            shootDir.Normalize();

            Quaternion spawnRot = Quaternion.LookRotation(shootDir, Vector3.up);
            GameObject spellObj = Instantiate(spellPrefab, spawnPos, spawnRot);

            activeSpell = spellObj.GetComponent<BouncySpell>();
            if (activeSpell != null)
            {
                activeSpell.SetTargetPosition(mouseWorldPos);
                isChanneling = true;
            }
        }
    }

    bool TryGetMouseWorldPos(out Vector3 worldPos)
    {
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        Plane gamePlane = new Plane(Vector3.forward, Vector3.zero);

        if (gamePlane.Raycast(ray, out float enter))
        {
            worldPos = ray.GetPoint(enter);
            worldPos.z = 0f;
            return true;
        }

        worldPos = Vector3.zero;
        return false;
    }

    void AimAtMouse()
    {
        if (armBone == null || mainCam == null) return;

        if (TryGetMouseWorldPos(out Vector3 mouseWorldPos))
        {
            Vector3 aimDir = mouseWorldPos - armBone.position;
            aimDir.z = 0f;

            if (aimDir.sqrMagnitude < 0.001f) return;

            Vector3 localAimDir = transform.InverseTransformDirection(aimDir.normalized);
            float angle = Mathf.Atan2(localAimDir.y, localAimDir.z) * Mathf.Rad2Deg;
            angle = Mathf.Clamp(angle, -maxAimAngle, maxAimAngle);

            float finalAngle = (invertPitch ? -angle : angle) + armAngleOffset;

            Vector3 axisVector = Vector3.right;
            if (shoulderPitchAxis == PitchAxis.Y) axisVector = Vector3.up;
            else if (shoulderPitchAxis == PitchAxis.Z) axisVector = Vector3.forward;

            armBone.localRotation = initialArmLocalRot * Quaternion.AngleAxis(finalAngle, axisVector);
        }
    }
}