using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
// Add the Meta/Oculus SDK namespace for passthrough control
// The specific using statement may vary (e.g., Meta.XR.Util, OVR; check your installed package)
// For OVRManager, you often only need the default Unity ones if OVRManager is in a root folder.

public class MRtoVRTransition : MonoBehaviour
{
    [Header("Core Components")]
    [Tooltip("The root of your XR player rig (XR Origin).")]
    public Transform xrOrigin;

    [Header("Passthrough Control")]
    [Tooltip("The OVRPassthroughLayer component in your scene.")]
    public OVRPassthroughLayer passthroughLayer;

    [Tooltip("The root GameObject of the MASSIVE, scaled-up VR Guitar World.")]
    public GameObject vrWorldRoot;

    [Tooltip("The component that handles your screen fade logic (ScreenFadeController).")]
    public ScreenFadeController screenFader;

    [Header("Transition Settings")]
    [Tooltip("The transform defining the player's new position and rotation INSIDE the VR environment (e.g., at the Sound Hole).")]
    public Transform targetVRTransform;

    [Tooltip("The target scale of the VR World Root after transition (e.g., 50x or 100x).")]
    public Vector3 vrWorldTargetScale = new Vector3(100f, 100f, 100f);

    // --- 1. Detect Interaction (Select Entered) ---
    public void StartTransition(SelectEnterEventArgs args)
    {
        // Disable the interactable immediately so it can't be triggered again
        var interactable = GetComponent<XRBaseInteractable>();
        if (interactable != null)
        {
            interactable.enabled = false;
        }

        // 2. Initiate a quick screen fade-out (THE MASK)
        if (screenFader != null)
        {
            // The action that runs once the screen is fully black
            screenFader.FadeOut(0.5f, OnFadeOutComplete);
        }
        else
        {
            Debug.LogWarning("Screen Fader is missing! Transition will not be seamless.");
            OnFadeOutComplete(); // Transition immediately without masking
        }
    }

    // --- 3. Perform Overpass / Transformation Logic (Screen is Black) ---
    private void OnFadeOutComplete()
    {
        // a) SCALE THE WORLD UP (to make the user seem 'fly-sized')
        vrWorldRoot.transform.localScale = vrWorldTargetScale;
        vrWorldRoot.SetActive(true);

        // b) REPOSITION AND ORIENT THE XR RIG
        xrOrigin.position = targetVRTransform.position;
        xrOrigin.rotation = targetVRTransform.rotation;

        // c) PLATFORM-SPECIFIC PASSTHROUGH TOGGLE (MR -> VR)
        // FIX: Use the OVRPassthroughLayer.hidden property to hide the passthrough camera feed
        if (passthroughLayer != null)
        {
            // Setting hidden = true switches to the default skybox/environment, which is full VR.
            passthroughLayer.hidden = true;
            Debug.Log("Passthrough hidden. Transitioned to full VR.");
        }
        else
        {
            Debug.LogError("Passthrough Layer is not assigned. MR-to-VR transition will fail!");
        }

        // 4. Initiate the screen fade-in (THE REVEAL)
        if (screenFader != null)
        {
            screenFader.FadeIn(0.5f);
        }
    }

}

       