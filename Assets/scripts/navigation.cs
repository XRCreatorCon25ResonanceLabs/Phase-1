using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class GuitarNavigator_NoFadeSmoothOnly : MonoBehaviour
{
    public Transform xrOrigin;
    public Transform teleportPointsParent;
    public List<Transform> teleportPoints = new List<Transform>();

    public List<GameObject> hotspotUIs = new List<GameObject>();
    public List<AudioSource> hotspotAudios = new List<AudioSource>();
    public List<GameObject> hotspotLights = new List<GameObject>();

    public List<GameObject> hotspotEnableOnActivate = new List<GameObject>();
    public List<GameObject> hotspotDisableOnActivate = new List<GameObject>();

    public bool activateOnStart = true;

    public float joystickDeadzone = 0.5f;
    public float inputCooldown = 0.45f;
    public float teleportHeightOffset = 0.0f;

    public float moveSpeed = 1.5f;
    public float stickDeadzone = 0.15f;
    public LayerMask guitarLayer;
    public float raycastDistance = 2f;
    public float gravity = -9.81f;

    public bool useSmoothTeleport = true;
    public float lerpDuration = 0.35f;

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

        if (teleportPointsParent != null && teleportPoints.Count == 0)
        {
            foreach (Transform t in teleportPointsParent)
                teleportPoints.Add(t);
        }

        AutoFillHotspotLists();
        CreateInputActions();

        if (teleportPoints.Count > 0)
        {
            if (activateOnStart)
            {
                DeactivateAllHotspots();
                ActivateHotspot(0);
            }
            StartTeleportRoutine(0);
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
        leftStickAction = new InputAction("LeftStick", InputActionType.Value);
        leftStickAction.AddBinding("<Gamepad>/leftStick");
        leftStickAction.AddBinding("<XRController>{LeftHand}/thumbstick");

        rightStickAction = new InputAction("RightStick", InputActionType.Value);
        rightStickAction.AddBinding("<Gamepad>/rightStick");
        rightStickAction.AddBinding("<XRController>{RightHand}/thumbstick");
    }

    void AutoFillHotspotLists()
    {
        while (hotspotUIs.Count < teleportPoints.Count) hotspotUIs.Add(null);
        while (hotspotAudios.Count < teleportPoints.Count) hotspotAudios.Add(null);
        while (hotspotLights.Count < teleportPoints.Count) hotspotLights.Add(null);
        while (hotspotEnableOnActivate.Count < teleportPoints.Count) hotspotEnableOnActivate.Add(null);
        while (hotspotDisableOnActivate.Count < teleportPoints.Count) hotspotDisableOnActivate.Add(null);

        for (int i = 0; i < teleportPoints.Count; i++)
        {
            if (teleportPoints[i] == null) continue;

            if (hotspotUIs[i] == null)
            {
                Transform uiChild = teleportPoints[i].Find("UI");
                if (uiChild != null) hotspotUIs[i] = uiChild.gameObject;
                else
                {
                    Canvas c = teleportPoints[i].GetComponentInChildren<Canvas>(true);
                    if (c != null) hotspotUIs[i] = c.gameObject;
                }
            }

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

            if (hotspotLights[i] == null)
            {
                Transform lightChild = teleportPoints[i].Find("Spotlight");
                if (lightChild == null) lightChild = teleportPoints[i].Find("Lights");
                if (lightChild != null) hotspotLights[i] = lightChild.gameObject;
                else
                {
                    Light anyLight = teleportPoints[i].GetComponentInChildren<Light>(true);
                    if (anyLight != null) hotspotLights[i] = anyLight.gameObject;
                }
            }
        }
    }

    void DeactivateAllHotspots()
    {
        for (int i = 0; i < hotspotUIs.Count && i < teleportPoints.Count; i++)
            if (hotspotUIs[i] != null) hotspotUIs[i].SetActive(false);

        for (int i = 0; i < hotspotAudios.Count && i < teleportPoints.Count; i++)
            if (hotspotAudios[i] != null && hotspotAudios[i].isPlaying)
                hotspotAudios[i].Stop();

        for (int i = 0; i < hotspotLights.Count && i < teleportPoints.Count; i++)
            if (hotspotLights[i] != null) hotspotLights[i].SetActive(false);

        for (int i = 0; i < teleportPoints.Count; i++)
        {
            if (hotspotEnableOnActivate.Count > i && hotspotEnableOnActivate[i] != null)
                hotspotEnableOnActivate[i].SetActive(false);

            if (hotspotDisableOnActivate.Count > i && hotspotDisableOnActivate[i] != null)
                hotspotDisableOnActivate[i].SetActive(true);
        }
    }

    void ActivateHotspot(int index)
    {
        if (index < 0 || index >= teleportPoints.Count) return;

        for (int i = 0; i < hotspotUIs.Count; i++)
            if (i != index && hotspotUIs[i] != null) hotspotUIs[i].SetActive(false);

        for (int i = 0; i < hotspotAudios.Count; i++)
            if (i != index && hotspotAudios[i] != null && hotspotAudios[i].isPlaying)
                hotspotAudios[i].Stop();

        for (int i = 0; i < hotspotLights.Count; i++)
            if (i != index && hotspotLights[i] != null) hotspotLights[i].SetActive(false);

        for (int i = 0; i < teleportPoints.Count; i++)
        {
            if (i == index) continue;

            if (hotspotEnableOnActivate[i] != null)
                hotspotEnableOnActivate[i].SetActive(false);

            if (hotspotDisableOnActivate[i] != null)
                hotspotDisableOnActivate[i].SetActive(true);
        }

        if (hotspotUIs[index] != null) hotspotUIs[index].SetActive(true);

        if (hotspotAudios[index] != null)
        {
            hotspotAudios[index].Stop();
            hotspotAudios[index].Play();
        }

        if (hotspotLights[index] != null) hotspotLights[index].SetActive(true);

        if (hotspotEnableOnActivate[index] != null)
            hotspotEnableOnActivate[index].SetActive(true);

        if (hotspotDisableOnActivate[index] != null)
            hotspotDisableOnActivate[index].SetActive(false);
    }

    void Update()
    {
        if (isTeleporting) return;

        Vector2 right = rightStickAction.ReadValue<Vector2>();

        if (Mathf.Abs(right.x) > joystickDeadzone)
        {
            HandleRightStick(right);
            return;
        }

        HandleLeftStickMovement();
    }

    void HandleRightStick(Vector2 right)
    {
        if (Time.time - lastInputTime < inputCooldown) return;

        if (right.x > joystickDeadzone) StepNext();
        else if (right.x < -joystickDeadzone) StepPrevious();

        lastInputTime = Time.time;
    }

    void StepNext()
    {
        if (teleportPoints.Count == 0) return;
        currentIndex = (currentIndex + 1) % teleportPoints.Count;
        StartTeleportRoutine(currentIndex);
    }

    void StepPrevious()
    {
        if (teleportPoints.Count == 0) return;
        currentIndex = (currentIndex - 1 + teleportPoints.Count) % teleportPoints.Count;
        StartTeleportRoutine(currentIndex);
    }

    public void StartTeleportRoutine(int index)
    {
        if (!isTeleporting) StartCoroutine(TeleportCoroutine(index));
    }

    IEnumerator TeleportCoroutine(int index)
    {
        if (teleportPoints.Count == 0) yield break;
        if (index < 0 || index >= teleportPoints.Count) yield break;

        isTeleporting = true;

        Transform t = teleportPoints[index];

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

        DeactivateAllHotspots();
        ActivateHotspot(index);

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

    void HandleLeftStickMovement()
    {
        Vector2 left = leftStickAction.ReadValue<Vector2>();
        if (left.magnitude < stickDeadzone) left = Vector2.zero;

        Transform cam = Camera.main != null ? Camera.main.transform : xrOrigin;

        Vector3 forward = cam.forward; forward.y = 0; forward.Normalize();
        Vector3 right = cam.right; right.y = 0; right.Normalize();

        Vector3 desired = (forward * left.y + right * left.x) * moveSpeed;
        Vector3 move = ProjectOntoGuitar(desired) * Time.deltaTime;

        if (cc.isGrounded)
            verticalVelocity = Vector3.zero;
        else
            verticalVelocity += Vector3.up * gravity * Time.deltaTime;

        cc.Move(move + verticalVelocity * Time.deltaTime);
    }

    Vector3 ProjectOntoGuitar(Vector3 desired)
    {
        Vector3 origin = xrOrigin.position + Vector3.up * 0.3f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastDistance, guitarLayer))
            return Vector3.ProjectOnPlane(desired, hit.normal);

        return Vector3.ProjectOnPlane(desired, Vector3.up);
    }

    public void TeleportToByName(string name)
    {
        for (int i = 0; i < teleportPoints.Count; i++)
        {
            if (teleportPoints[i] != null && teleportPoints[i].name == name)
            {
                currentIndex = i;
                StartTeleportRoutine(i);
                return;
            }
        }
    }
}
