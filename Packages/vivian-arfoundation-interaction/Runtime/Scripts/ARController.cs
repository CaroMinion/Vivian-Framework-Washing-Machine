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

namespace de.ugoe.cs.vivian.arfoundationinteraction
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.InputSystem.EnhancedTouch;
    using de.ugoe.cs.vivian.core;
    using System.Collections.Generic;

#if UNITY_EDITOR
    // Set up touch input propagation while using Instant Preview in the editor.
    // Using ARFoundation, it seems like Instant Preview doesn't exist anymore
    // using Input = GoogleARCore.InstantPreviewInput;
#endif

    /**
     * controller for AR Core based interaction
     */
    public class ARController : MonoBehaviour
    {
        // the prototypes in the scene while interacting
        private VirtualPrototype[] Prototypes = null;

        // the AR Camera
        public Camera arCamera;

        // used to handle the situation where touch began was not fired
        private HashSet<int> activeTouches = new HashSet<int>();

        /**
         * called on awake to set the target framerate
         */
        public void Awake()
        {
            // Enable ARCore to target 60fps camera capture frame rate on supported devices.
            // Note, Application.targetFrameRate is ignored when QualitySettings.vSyncCount != 0.
            // Application.targetFrameRate = 60;
            EnhancedTouchSupport.Enable();
        }

        /**
         * Called every frame to check for touches
         */
        public void Update()
        {
            foreach (var touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
            {
                // Should not handle input if the player is pointing on UI.
                if (EventSystem.current.IsPointerOverGameObject(touch.touchId))
                {
                    return;
                }

                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    activeTouches.Add(touch.touchId);
                    HandleTouchBegan(touch);
                }
                else if ((touch.phase == UnityEngine.InputSystem.TouchPhase.Moved) ||
                         (touch.phase == UnityEngine.InputSystem.TouchPhase.Stationary))
                {
                    // we also handle stationary touches as the user may have moved the phone
                    
                    if (!activeTouches.Contains(touch.touchId))
                    {
                        // handle case where TouchPhase.Began did not occur
                        activeTouches.Add(touch.touchId);
                        HandleTouchBegan(touch);
                    }

                    HandleTouchMoved(touch);
                }
                else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended)
                {
                    if (!activeTouches.Contains(touch.touchId))
                    {
                        // handle case where TouchPhase.Began did not occur
                        activeTouches.Add(touch.touchId);
                        HandleTouchBegan(touch);
                    }

                    HandleTouchEnded(touch);
                    activeTouches.Remove(touch.touchId);
                }
            }
        }

        /**
         * handles a touch begin
         */
        private void HandleTouchBegan(UnityEngine.InputSystem.EnhancedTouch.Touch touch)
        {
            this.Prototypes = FindObjectsOfType<VirtualPrototype>();

            if (this.Prototypes != null)
            {
                Pose pose = this.GetPose(touch);

                foreach (VirtualPrototype prototype in this.Prototypes)
                {
                    prototype.TriggerInteractionStarts(pose, touch.touchId);
                }
            }
        }

        /**
         * handles a touch move
         */
        private void HandleTouchMoved(UnityEngine.InputSystem.EnhancedTouch.Touch touch)
        {
            if (this.Prototypes != null)
            {
                Pose pose = this.GetPose(touch);

                foreach (VirtualPrototype prototype in this.Prototypes)
                {
                    prototype.TriggerInteractionContinues(pose, touch.touchId);
                }
            }
        }

        /**
         * handles a touch end
         */
        private void HandleTouchEnded(UnityEngine.InputSystem.EnhancedTouch.Touch touch)
        {
            if (this.Prototypes != null)
            {
                Pose pose = this.GetPose(touch);

                foreach (VirtualPrototype prototype in this.Prototypes)
                {
                    prototype.TriggerInteractionEnds(pose, touch.touchId);
                }
            }
        }

        /**
         * convenience method to determine the pose of interaction
         */
        private Pose GetPose(UnityEngine.InputSystem.EnhancedTouch.Touch touch)
        {
            Ray ray = arCamera.ScreenPointToRay(touch.screenPosition);
            return new Pose(ray.origin, Quaternion.LookRotation(ray.direction, arCamera.transform.up));
        }
    }
}

