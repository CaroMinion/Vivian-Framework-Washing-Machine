// Copyright 2019 Patrick Harms
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections;
using System.Collections.Generic;
using de.ugoe.cs.vivian.core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

/**
 * Creates an AR anchor on the touched plane, then attaches the instantiated VP to it.
 * Supports two modes: the "manipulation" mode that allows the user to move and rotate the VP
 * with respect to the AR planes, and the "use" mode, where the user interacts with the anchored VP.
 */
public class ObjectPositioner : MonoBehaviour
{
    // AR Foundation Game Object that contains the necessary managers
    public GameObject arSessionOrigin;
    // Transparent material applied to the preview prefab 
    public Material previewMaterial;
    // The preview position validation button to exit manipulation mode
    public GameObject validatePreviewButton;
    // The manipulation button to go enter manipulation mode
    public GameObject manipulationButton;
    // The TouchKit GameObject in the Scene to be activated after validation
    public GameObject touchKit;
    // Flag determining if the user touches the screen for the 1st time in the scene
    private bool firstTouch = true;    
    // Flag determining if we are in manipulation mode
    private bool bManipulationMode;
    // AR Foundation managers
    private ARRaycastManager raycastManager;
    private ARAnchorManager anchorManager;
    private ARPlaneManager planeManager;
    private ARPointCloudManager pointCloudManager;
    // The game object to position
    private GameObject gameObjectToPosition;
    // The VP component taken from gameObjectToPosition
    private VirtualPrototype virtualPrototype;
    // Contains the original materials for the VP
    private List<Material> listOriginalMaterials = new List<Material>();
    // List of raycast hits resulting from the AR Raycast Manager's raycast 
    private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();
    // Rotation values of the VP before we start rotating it
    private Quaternion prevRot = Quaternion.identity;
    // Flag indicating if we touched a button
    private bool bTouchOverUI;
    // Used to handle the situation where touch began was not fired
    private HashSet<int> activeTouches = new HashSet<int>();


    private void Awake()
    {
        raycastManager = arSessionOrigin.GetComponent<ARRaycastManager>(); 
        anchorManager = arSessionOrigin.GetComponent<ARAnchorManager>();
        planeManager = arSessionOrigin.GetComponent<ARPlaneManager>();
        pointCloudManager = arSessionOrigin.GetComponent<ARPointCloudManager>();
    }

    private void Start()
    {
        // Set listeners on buttons to allow switching modes
        validatePreviewButton.GetComponent<Button>().onClick.AddListener(() => ToggleManipulation(false));
        manipulationButton.GetComponent<Button>().onClick.AddListener(() => ToggleManipulation(true));
    }
    
    private void Update()
    {
        // If the user has not touched the screen or has touched a part of the UI (button), we are done with this update.
        if (Touch.activeTouches.Count < 1)
        {
            bTouchOverUI = false;
            return;
        }
        if (EventSystem.current.IsPointerOverGameObject())
            bTouchOverUI = true;
        // We check again for the flag because for some reason, IsPointerOverGameObject becomes false after one Update(), although
        // the touch still exists. This is here to avoid positioning the VP at the position of the button in the bottom right corner.
        if (bTouchOverUI) return;
            
        // AR Raycasts only test against planes and points in point clouds. We first test if we're hitting a plane 
        raycastManager.Raycast(Touch.activeTouches[0].screenPosition, raycastHits, TrackableType.Planes);
        if (raycastHits.Count > 0)
        {
            // If this is the first touch on the screen, instantiate preview on touched plane
            if (firstTouch)
                StartCoroutine(AnchorVirtualPrototype());
            if (bManipulationMode)
                ManipulateVirtualPrototype();
        }
        /*else
        {
            // The user did not touch a detected plane, nothing happens so we give feedback
            // TODO Adapt TouchKit feedback into cross platform feedback
            AndroidToast.ShowFeedback("Touch a detected plane to create Virtual Prototype", AndroidToast.Gravity.TOP, 0, 0, true);
        }*/
    }

    /**
     * Positions a preview on the first touch, by creating an anchor on a touched detected plane.
     * The positioning happens relative to the plane.
     */
    private IEnumerator AnchorVirtualPrototype()
    {
        Debug.Log("Registered initial touch --> Creating anchor at touch point and attaching VP to this anchor");
        firstTouch = false;
        // Wait until VP resources are loaded
        yield return new WaitUntil(() => virtualPrototype.IsResourceLoadingReady());
                    
        // Create anchor on touched plane
        ARRaycastHit hit = raycastHits[0];
        ARAnchor anchor = anchorManager.AttachAnchor((ARPlane) hit.trackable, hit.pose);
        // If creating an anchor failed, cancel the process
        if (anchor == null)
        {
            firstTouch = true;
            yield return null;
        }
        // Set VP as child of anchor
        gameObjectToPosition.transform.parent = anchor.transform;
        gameObjectToPosition.transform.SetPositionAndRotation(hit.pose.position, Quaternion.Euler(0,-180, 0));
        // Start the state machine
        virtualPrototype.InstantiatePrototype();
        
        // Clear the list of materials of a potential previous VP
        listOriginalMaterials.Clear();
        // Copy the VP's materials for later 
        foreach (MeshRenderer child in gameObjectToPosition.GetComponentsInChildren<MeshRenderer>())
            listOriginalMaterials.Add(child.material);

        this.manipulationButton.SetActive(true);
        ToggleManipulation(true);
    }

    /**
     * Manipulates the position and rotation of the VP
     */
    private void ManipulateVirtualPrototype()
    {
        // Set position of VP to position of the raycast hit's pose
        gameObjectToPosition.transform.position = raycastHits[0].pose.position;
        
        // If two touches are registered, update the VP's rotation
        if (Touch.activeTouches.Count > 1)
        {
            // Save the initial rotation value of the VP when the second finger just touched the screen
            if (Touch.activeTouches[1].phase == TouchPhase.Began)
            {
                activeTouches.Add(Touch.activeTouches[1].touchId);
                prevRot = gameObjectToPosition.transform.rotation;
            } else if (Touch.activeTouches[1].phase == TouchPhase.Moved ||
                       Touch.activeTouches[1].phase == TouchPhase.Stationary)
            {
                if (!activeTouches.Contains(Touch.activeTouches[1].touchId))
                {
                    // handle case where TouchPhase.Began did not occur
                    activeTouches.Add(Touch.activeTouches[1].touchId);
                    prevRot = gameObjectToPosition.transform.rotation;
                }
            }
            // Calculate the direction vectors of both touches, at their starting position and current direction
            Vector2 currentDirection = (Touch.activeTouches[1].screenPosition - Touch.activeTouches[0].screenPosition).normalized;
            Vector2 previousDirection = (Touch.activeTouches[1].startScreenPosition - Touch.activeTouches[0].startScreenPosition).normalized;
            // Calculate the sign and angle of the rotation we want to add to the VP
            float sign = Mathf.Sign(previousDirection.y * currentDirection.x - previousDirection.x * currentDirection.y);
            float angle = Vector2.Angle(currentDirection, previousDirection) * sign * 2;
            // Final rotation: value of previous rotation + calculated angle
            Vector3 rot = prevRot.eulerAngles + new Vector3(0, angle, 0);
            gameObjectToPosition.transform.rotation = Quaternion.Euler(rot);
        }
    }

    /**
     * Activates or deactivates the plane and point cloud managers as well as their visualizations
     */
    private void ShowPlanes(bool bShow)
    {
        // De/Reactivate plane visualization
        planeManager.enabled = bShow;
        foreach (var plane in planeManager.trackables)
        {
            plane.gameObject.SetActive(bShow);
        }
        // De/Reactivate point cloud visualization
        pointCloudManager.enabled = bShow;
        foreach (var point in pointCloudManager.trackables)
        {
            point.gameObject.SetActive(bShow);
        }
    }

    private void ToggleManipulation(bool bToggle)
    {
        // Toggle transparency of VP
        int i = 0;
        foreach (MeshRenderer child in gameObjectToPosition.GetComponentsInChildren<MeshRenderer>())
        {
            child.material = bToggle ? previewMaterial : listOriginalMaterials[i];
            i++;
        }

        // Toggle Plane visualization
        ShowPlanes(bToggle);
        // Switch buttons
        this.validatePreviewButton.SetActive(bToggle);
        this.manipulationButton.SetActive(!bToggle);
        // Toggle Touch Kit
        // this.touchKit.SetActive(!bToggle);

        bManipulationMode = bToggle;
    }

    /**
     * External method called by AppController when a different prototype is selected from the menu.
     * Resets the VP game object.
     */
    public void ResetVirtualPrototype(VirtualPrototype newVp)
    {
        gameObjectToPosition = newVp.gameObject;
        virtualPrototype = newVp;
        virtualPrototype.InstantiatedByObjectPositioner = true;
        prevRot = Quaternion.identity;
        firstTouch = true;
        ShowPlanes(true);
    }
}
