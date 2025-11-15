// navigation.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class GuitarNavigator_NoFadeSmoothOnly : MonoBehaviour
{
    [Header("References")]
    public Transform xrOrigin;
    public Transform teleportPointsParent;
    public List<Transform> teleportPoints = new List<Transform>();

    [Header("Hotspot UI, Audio & Lights (align with teleportPoints by index)")]
    [Tooltip("Assign a UI GameObject for each teleport point (same ordering as teleportPoints).")]
    public List<GameObject> hotspotUIs = new List<GameObject>();
    [Tooltip("Assign an AudioSource for each teleport point (same ordering as teleportPoints).")]
    public List<AudioSource> hotspotAudios = new List<AudioSource>();
    [Tooltip("Assign a GameObject (or parent) that contains the spotlight(s) for each hotspot.")]
    public List<GameObject> hotspotLights = new List<GameObject>();
    [Tooltip("If true, the first hotspot (index 0) UI/audio/light will be activated at start.")]
    public bool activateOnStart = true;

    [Header("Teleport Settings")]
    public float joystickDeadzone = 0.5f;    // right stick horizontal threshold
    public float inputCooldown = 0.45f;      // seconds between steps
    public float teleportHeightOffset = 0.0f;

    [Header("Movement Settings")]
    public float moveSpeed = 1.5f;
    public float stickDeadzone = 0.15f;
    public LayerMask guitarLayer;            // layer for guitar collider
    public float raycastDistance = 2f;
    public float gravity = -9.81f;

    [Header("Smooth Teleport Settings")]
    public bool useSmoothTeleport = true;
    public float lerpDuration = 0.35f;       // seconds to lerp the camera between points

    // runtime
    int currentIndex = 0;
    float lastInputTime = -10f;
    CharacterController cc;
    Vector3 verticalVelocity = Vector3.zero;

    InputAction leftStickAction;
    InputAction rightStickAction;

    bool isTeleporting = false;

    void Awake()
    {
        if (xrOrigin == null) xrOrigin = transform;

        cc = GetComponent<CharacterController>();
        if (cc == null) cc = gameObject.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.25f;
        cc.center = new Vector3(0, 0.9f, 0);

        // populate teleport list from parent if provided
        if (teleportPointsParent != null && teleportPoints.Count == 0)
        {
            foreach (Transform t in teleportPointsParent)
                teleportPoints.Add(t);
        }

        // Try auto-fill hotspot lists if lengths mismatch
        TryAutoFillHotspotLists();

        CreateInputActions();

        // Activate first hotspot optionally
        if (teleportPoints.Count > 0)
        {
            if (activateOnStart)
            {
                DeactivateAllHotspots();
                ActivateHotspot(0);
            }
            StartTeleportRoutine(0); // move to first point at start (smooth)
        }
    }

    void OnEnable()
    {
        leftStickAction?.Enable();
        rightStickAction?.Enable();
    }

    void OnDisable()
    {
        leftStickAction?.Disable();
        rightStickAction?.Disable();
    }

    void OnDestroy()
    {
        leftStickAction?.Disable();
        rightStickAction?.Disable();
        leftStickAction?.Dispose();
        rightStickAction?.Dispose();
    }

    void CreateInputActions()
    {
        // left joystick (explicit left-hand + gamepad)
        leftStickAction = new InputAction("LeftStick", InputActionType.Value);
        leftStickAction.AddBinding("<Gamepad>/leftStick");
        leftStickAction.AddBinding("<XRController>{LeftHand}/thumbstick");

        // right joystick (explicit right-hand + gamepad)
        rightStickAction = new InputAction("RightStick", InputActionType.Value);
        rightStickAction.AddBinding("<Gamepad>/rightStick");
        rightStickAction.AddBinding("<XRController>{RightHand}/thumbstick");
    }

    void TryAutoFillHotspotLists()
    {
        // Ensure lists are at least as long as teleportPoints (fill with nulls)
        while (hotspotUIs.Count < teleportPoints.Count) hotspotUIs.Add(null);
        while (hotspotAudios.Count < teleportPoints.Count) hotspotAudios.Add(null);
        while (hotspotLights.Count < teleportPoints.Count) hotspotLights.Add(null);

        // For any null entries, attempt to find child objects under the teleport point:
        for (int i = 0; i < teleportPoints.Count; i++)
        {
            if (teleportPoints[i] == null) continue;

            // Auto-fill UI
            if (hotspotUIs[i] == null)
            {
                Transform uiChild = teleportPoints[i].Find("UI");
                if (uiChild != null)
                    hotspotUIs[i] = uiChild.gameObject;
                else
                {
                    Canvas c = teleportPoints[i].GetComponentInChildren<Canvas>(true);
                    if (c != null) hotspotUIs[i] = c.gameObject;
                }
            }

            // Auto-fill AudioSource
            if (hotspotAudios[i] == null)
            {
                AudioSource a = teleportPoints[i].GetComponent<AudioSource>();
                if (a != null) hotspotAudios[i] = a;
                else
                {
                    AudioSource aChild = teleportPoints[i].GetComponentInChildren<AudioSource>(true);
                    if (aChild != null) hotspotAudios[i] = aChild;
                }
            }

            // Auto-fill Light GameObject
            if (hotspotLights[i] == null)
            {
                // look for child named "Spotlight" or "Lights"
                Transform lightChild = teleportPoints[i].Find("Spotlight");
                if (lightChild == null) lightChild = teleportPoints[i].Find("Lights");
                if (lightChild != null)
                {
                    hotspotLights[i] = lightChild.gameObject;
                }
                else
                {
                    // fallback: find any child that contains a Light component
                    Light anyLight = teleportPoints[i].GetComponentInChildren<Light>(true);
                    if (anyLight != null) hotspotLights[i] = anyLight.gameObject;
                }
            }
        }
    }

    void DeactivateAllHotspots()
    {
        for (int i = 0; i < hotspotUIs.Count && i < teleportPoints.Count; i++)
        {
            if (hotspotUIs[i] != null)
                hotspotUIs[i].SetActive(false);
        }
        for (int i = 0; i < hotspotAudios.Count && i < teleportPoints.Count; i++)
        {
            if (hotspotAudios[i] != null && hotspotAudios[i].isPlaying)
                hotspotAudios[i].Stop();
        }
        for (int i = 0; i < hotspotLights.Count && i < teleportPoints.Count; i++)
        {
            if (hotspotLights[i] != null)
                hotspotLights[i].SetActive(false);
        }
    }

    void ActivateHotspot(int index)
    {
        if (index < 0 || index >= teleportPoints.Count) return;

        // Deactivate previous (all others)
        for (int i = 0; i < hotspotUIs.Count && i < teleportPoints.Count; i++)
        {
            if (i == index) continue;
            if (hotspotUIs[i] != null)
                hotspotUIs[i].SetActive(false);
        }
        for (int i = 0; i < hotspotAudios.Count && i < teleportPoints.Count; i++)
        {
            if (i == index) continue;
            if (hotspotAudios[i] != null && hotspotAudios[i].isPlaying)
                hotspotAudios[i].Stop();
        }
        for (int i = 0; i < hotspotLights.Count && i < teleportPoints.Count; i++)
        {
            if (i == index) continue;
            if (hotspotLights[i] != null)
                hotspotLights[i].SetActive(false);
        }

        // Activate current
        if (hotspotUIs.Count > index && hotspotUIs[index] != null)
            hotspotUIs[index].SetActive(true);

        if (hotspotAudios.Count > index && hotspotAudios[index] != null)
        {
            hotspotAudios[index].Stop();
            hotspotAudios[index].Play();
        }

        if (hotspotLights.Count > index && hotspotLights[index] != null)
            hotspotLights[index].SetActive(true);
    }

    void Update()
    {
        if (isTeleporting) return;

        Vector2 right = rightStickAction.ReadValue<Vector2>();

        // prioritize right-stick horizontal for teleport steps
        if (Mathf.Abs(right.x) > joystickDeadzone)
        {
            HandleRightStick(right);
            return; // skip movement this frame when right-stick active
        }

        HandleLeftStickMovement();
    }

    // ---------------- TELEPORT ----------------
    void HandleRightStick(Vector2 right)
    {
        if (Time.time - lastInputTime < inputCooldown) return;

        if (right.x > joystickDeadzone)
        {
            StepNext();
        }
        else if (right.x < -joystickDeadzone)
        {
            StepPrevious();
        }

        lastInputTime = Time.time;
    }

    void StepNext()
    {
        if (teleportPoints.Count == 0) return;
        int next = (currentIndex + 1) % teleportPoints.Count;
        currentIndex = next;
        StartTeleportRoutine(currentIndex);
    }

    void StepPrevious()
    {
        if (teleportPoints.Count == 0) return;
        int prev = (currentIndex - 1 + teleportPoints.Count) % teleportPoints.Count;
        currentIndex = prev;
        StartTeleportRoutine(currentIndex);
    }

    public void StartTeleportRoutine(int index)
    {
        if (isTeleporting) return;
        StartCoroutine(TeleportCoroutine(index));
    }

    IEnumerator TeleportCoroutine(int index)
    {
        if (teleportPoints.Count == 0) yield break;
        if (index < 0 || index >= teleportPoints.Count) yield break;

        isTeleporting = true;
        Transform t = teleportPoints[index];

        // compute target xrOrigin position keeping camera local offset if present
        Vector3 targetPos = t.position + Vector3.up * teleportHeightOffset;
        Camera cam = Camera.main;
        Vector3 targetOriginPos;
        if (cam != null && cam.transform.parent == xrOrigin)
        {
            Vector3 camLocal = cam.transform.localPosition;
            targetOriginPos = targetPos - camLocal;
        }
        else
        {
            targetOriginPos = targetPos;
        }
        Quaternion targetOriginRot = Quaternion.Euler(0f, t.rotation.eulerAngles.y, 0f);

        // Console message
        Debug.Log($"Teleported to: {t.name} (Index {index})");

        // manage hotspot UI/audio/light: deactivate others, activate this (so user sees the UI during/after teleport)
        DeactivateAllHotspots();
        ActivateHotspot(index);

        // disable CharacterController during reposition to avoid physics glitches
        bool hadController = cc != null && cc.enabled;
        if (hadController) cc.enabled = false;

        if (useSmoothTeleport && lerpDuration > 0f)
        {
            Vector3 startPos = xrOrigin.position;
            Quaternion startRot = xrOrigin.rotation;
            float elapsed = 0f;
            while (elapsed < lerpDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float tL = Mathf.Clamp01(elapsed / lerpDuration);
                float smooth = Mathf.SmoothStep(0f, 1f, tL);
                xrOrigin.position = Vector3.Lerp(startPos, targetOriginPos, smooth);
                xrOrigin.rotation = Quaternion.Slerp(startRot, targetOriginRot, smooth);
                yield return null;
            }
            xrOrigin.position = targetOriginPos;
            xrOrigin.rotation = targetOriginRot;
        }
        else
        {
            xrOrigin.position = targetOriginPos;
            xrOrigin.rotation = targetOriginRot;
        }

        if (hadController) cc.enabled = true;

        isTeleporting = false;
    }

    // ---------------- MOVEMENT ----------------
    void HandleLeftStickMovement()
    {
        Vector2 left = leftStickAction.ReadValue<Vector2>();
        if (left.magnitude < stickDeadzone) left = Vector2.zero;

        Transform cam = Camera.main != null ? Camera.main.transform : xrOrigin;
        Vector3 forward = cam.forward; forward.y = 0; forward.Normalize();
        Vector3 right = cam.right; right.y = 0; right.Normalize();

        Vector3 desired = (forward * left.y + right * left.x) * moveSpeed;
        Vector3 move = ProjectOntoGuitar(desired) * Time.deltaTime;

        // gravity
        if (cc.isGrounded) verticalVelocity = Vector3.zero;
        else verticalVelocity += Vector3.up * gravity * Time.deltaTime;

        cc.Move(move + verticalVelocity * Time.deltaTime);
    }

    Vector3 ProjectOntoGuitar(Vector3 desired)
    {
        Vector3 origin = xrOrigin.position + Vector3.up * 0.3f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastDistance, guitarLayer))
            return Vector3.ProjectOnPlane(desired, hit.normal);

        return Vector3.ProjectOnPlane(desired, Vector3.up);
    }

    // Optional external helper: teleport by point name
    public void TeleportToByName(string name)
    {
        for (int i = 0; i < teleportPoints.Count; i++)
            if (teleportPoints[i] != null && teleportPoints[i].name == name)
            {
                currentIndex = i;
                StartTeleportRoutine(i);
                return;
            }
    }
}
