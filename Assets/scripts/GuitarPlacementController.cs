using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

// Attach this script to an empty GameObject in your scene (e.g., "Placement Manager")

public class GuitarPlacementController : MonoBehaviour
{
    [Tooltip("The AR Raycast Manager component in your scene.")]
    public ARRaycastManager arRaycastManager;

    [Tooltip("The Prefab of the 1:1 scale Guitar model to place.")]
    public GameObject guitarPrefab;

    [Tooltip("The Transform of the user's main camera (HMD).")]
    public Transform hmdTransform;

    [Tooltip("The minimum allowed distance for placement (e.g., 5m radius constraint).")]
    public float minPlacementDistance = 5f;

    // Used to store the results of the raycast hit test
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    // Tracks the currently placed guitar instance
    private GameObject currentGuitarInstance;

    // Flag to enable placement only after UI selection
    private bool isPlacementEnabled = false;

    // --- Public method called by the UI ---
    public void EnablePlacementMode(GameObject mainMenuCanvas)
    {
        isPlacementEnabled = true;
        gameObject.SetActive(true);
        if (mainMenuCanvas != null)
        {
            mainMenuCanvas.SetActive(false);
        }
    }

    void Update()
    {
        if (!isPlacementEnabled)
            return;

        // Use the Right Hand's Ray Interactor position as the source (Assuming ray is on the right)
        // For simplicity, we'll use the center of the screen/HMD for now. 
        // In a real XR setup, you would use the ray interactor's endpoint.
        // For testing, let's use the center of the camera view:
        Vector3 raycastOrigin = hmdTransform.position + hmdTransform.forward * 2f;

        // 1. Perform Raycast against detected planes
        if (arRaycastManager.Raycast(raycastOrigin, hits, TrackableType.PlaneWithinPolygon))
        {
            // Get the pose (position/rotation) of the first hit plane
            Pose hitPose = hits[0].pose;

            // 2. Check the placement constraint
            float distance = Vector3.Distance(hmdTransform.position, hitPose.position);

            if (distance >= minPlacementDistance)
            {
                // Visual Cue: Show a floating placement ghost/reticle here (optional, but highly recommended UX)
                // e.g., Reticle.transform.position = hitPose.position;

                // 3. Detect Placement Input (Assuming the trigger button is mapped to 'Select')
                // IMPORTANT: You need to check the state of the Select action here.
                // Since this is a simple script, we'll assume a placement function is called on button release.

                // If the user presses the 'Select' input (trigger):
                // PlaceGuitar(hitPose); 
            }
            else
            {
                // Visual Cue: Show a 'Too Close' reticle color (UX)
            }
        }
    }

    // --- Method to be linked to your Ray Interactor's Select End Action ---
    public void PlaceGuitar(Pose placementPose)
    {
        // 1. Enforce single placement
        if (currentGuitarInstance != null)
        {
            Destroy(currentGuitarInstance);
        }

        // 2. Instantiate the guitar at the plane hit position
        currentGuitarInstance = Instantiate(guitarPrefab, placementPose.position, placementPose.rotation);

        // 3. Disable the placement mode
        isPlacementEnabled = false;
        gameObject.SetActive(false);

        // --- NEXT STEP: Trigger the MR to VR Transition logic here ---
        // currentGuitarInstance.GetComponent<MRtoVRTransition>().SetupWalkTrigger(); 
    }
}