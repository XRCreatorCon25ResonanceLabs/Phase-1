using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnableObjectAndMove : MonoBehaviour
{
    public GameObject[] sceneObjects;
    public Transform[] moveTargets;

    public Transform xrRig;

    public bool smoothMove = true;
    public float moveDuration = 0.8f;

    public Button[] buttons;
    public Button backButton;

    public Transform objectsParent;

    private int currentActiveIndex = -1;
    private Vector3 startPos;
    private Quaternion startRot;
    private bool isMoving = false;

    void Start()
    {
        startPos = xrRig != null ? xrRig.position : Vector3.zero;
        startRot = xrRig != null ? xrRig.rotation : Quaternion.identity;

        for (int i = 0; i < buttons.Length; i++)
        {
            int idx = i;
            if (buttons[idx] != null)
                buttons[idx].onClick.AddListener(() => OnOptionButtonPressed(idx));
        }

        if (backButton != null)
            backButton.onClick.AddListener(OnBackButtonPressed);

        currentActiveIndex = -1;
    }

    void OnOptionButtonPressed(int index)
    {
        if (index < 0 || index >= sceneObjects.Length)
            return;

        GameObject obj = sceneObjects[index];

        if (currentActiveIndex != -1 && currentActiveIndex != index)
        {
            GameObject prev = sceneObjects[currentActiveIndex];
            if (prev != null) prev.SetActive(false);
        }

        if (obj != null)
        {
            obj.SetActive(true);
            if (objectsParent != null)
                obj.transform.SetParent(objectsParent, true);

            currentActiveIndex = index;
        }

        if (xrRig == null) return;
        if (index >= moveTargets.Length || moveTargets[index] == null) return;

        if (smoothMove)
            StartSmoothMove(moveTargets[index].position, moveTargets[index].rotation);
        else
        {
            xrRig.position = moveTargets[index].position;
            xrRig.rotation = moveTargets[index].rotation;
        }
    }

    void OnBackButtonPressed()
    {
        if (currentActiveIndex != -1 && currentActiveIndex < sceneObjects.Length)
        {
            GameObject currentObj = sceneObjects[currentActiveIndex];
            if (currentObj != null) currentObj.SetActive(false);
            currentActiveIndex = -1;
        }

        if (xrRig == null) return;

        if (smoothMove)
            StartSmoothMove(startPos, startRot);
        else
        {
            xrRig.position = startPos;
            xrRig.rotation = startRot;
        }
    }

    private void StartSmoothMove(Vector3 targetPos, Quaternion targetRot)
    {
        StopAllCoroutines();
        StartCoroutine(SmoothMoveRig(targetPos, targetRot));
    }

    private IEnumerator SmoothMoveRig(Vector3 targetPos, Quaternion targetRot)
    {
        if (xrRig == null)
            yield break;

        isMoving = true;

        Vector3 initialPos = xrRig.position;
        Quaternion initialRot = xrRig.rotation;

        float elapsed = 0f;
        float duration = Mathf.Max(0.0001f, moveDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float s = t * t * (3f - 2f * t);

            xrRig.position = Vector3.Lerp(initialPos, targetPos, s);
            xrRig.rotation = Quaternion.Slerp(initialRot, targetRot, s);

            yield return null;
        }

        xrRig.position = targetPos;
        xrRig.rotation = targetRot;

        isMoving = false;
    }
}
