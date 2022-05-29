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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace de.ugoe.cs.vivian.core
{
    public class VirtualPrototype : MonoBehaviour
    {
        // Reference to the bundle url that contains the prototype.
        public string BundleURL;

        // name of the prototype prefab to load.
        public string PrototypePrefabName;

        // flag indicating if the prototype was loaded completely
        public bool Loaded { get; private set; } = false;

        // the state machine of the prototype
        public StateMachine StateMachine { get; private set; }

        // Reference to the actual virtual prototype
        private GameObject prototypeInstance;

        // information about current interactions
        private Dictionary<int, VirtualPrototypeElement> currentInteractions = new Dictionary<int, VirtualPrototypeElement>();
        
        // Flag indicating if the ObjectPositioner script triggers the instantiation of the VP 
        [NonSerialized] public bool InstantiatedByObjectPositioner = false;
        
        // The Virtual Prototype's prefab
        private GameObject virtualPrototypePrefab;
        
        // Flag indicating if loading of the resources is done
        private bool isLoadingReady;

        // Arrays loaded with the info parsed from the JSON files
        private InteractionElementSpec[] interactionElementSpecs;

        private VisualizationElementSpec[] visualizationElementSpecs;

        private VisualizationArraySpec[] visualizationArraySpecs;

        private VisualizationSpec[] allVisualizationElements;

        private StateSpec[] states;

        private TransitionSpec[] transitionSpecs;
        
        private IResourceLoader resourceLoader;


        // This script will simply instantiate the Prefab when the game starts.
        void Start()
        {
            StartCoroutine(LoadPrototypeResources(this.BundleURL, this.PrototypePrefabName));
        }

        /**
         * This is called when the user starts interacting with this element
         */
        public virtual void TriggerInteractionStarts(Pose pose, int poseId)
        {
            if (this.currentInteractions.ContainsKey(poseId))
            {
                throw new ArgumentException("the given pose id is already in interaction");
            }

            VirtualPrototypeElement vpElement = this.GetRelevantObject(pose);

            if (vpElement != null)
            {
                this.currentInteractions.Add(poseId, vpElement);
                vpElement.TriggerInteractionStarts(pose);
            }
        }

        /**
         * This is called when the user continues interacting with this element
         */
        public virtual void TriggerInteractionContinues(Pose pose, int poseId)
        {
            if (this.currentInteractions.TryGetValue(poseId, out VirtualPrototypeElement vpElement))
            {
                vpElement.TriggerInteractionContinues(pose);
            }
            else
            {
                Debug.Log("the pose id is currently not in interaction");
            }
        }

        /**
         * This is called when the user ends interacting with this element
         */
        public virtual void TriggerInteractionEnds(Pose pose, int poseId)
        {
            if (this.currentInteractions.TryGetValue(poseId, out VirtualPrototypeElement vpElement))
            {
                vpElement.TriggerInteractionEnds(pose);
                this.currentInteractions.Remove(poseId);
            }
            else
            {
                Debug.Log("the pose id is currently not in interaction");
            }
        }

        /**
         * 
         */
        private VirtualPrototypeElement GetRelevantObject(Pose pose)
        {
            VirtualPrototypeElement closestVPElement = null;

            RaycastHit[] hits = Physics.RaycastAll(pose.position, pose.forward);

            if (hits != null)
            {
                //Debug.Log("hits " + hits.Length);
                InteractionElement closestInteractionElement = null;
                VisualizationElement closestVisualizationElement = null;

                float closestInteractionElementDistance = float.MaxValue;
                float closestVisualizationElementDistance = float.MaxValue;
                float closestVPElementDistance = float.MaxValue;

                foreach (RaycastHit hit in hits)
                {
                    //Debug.Log("checking " + hit.collider);
                    VirtualPrototypeElement vpElement = hit.collider.GetComponent<VirtualPrototypeElement>();

                    if (vpElement != null)
                    {
                        if ((vpElement is InteractionElement) && (hit.distance < closestInteractionElementDistance))
                        {
                            //Debug.Log("variant 1");
                            closestInteractionElement = (InteractionElement)vpElement;
                            closestInteractionElementDistance = hit.distance;
                        }
                        else if ((vpElement is VisualizationElement) && (hit.distance < closestVisualizationElementDistance))
                        {
                            //Debug.Log("variant 2");
                            closestVisualizationElement = (VisualizationElement)vpElement;
                            closestVisualizationElementDistance = hit.distance;
                        }
                        else if (hit.distance < closestVPElementDistance)
                        {
                            //Debug.Log("variant 3");
                            closestVPElement = vpElement;
                            closestVPElementDistance = hit.distance;
                        }
                    }
                }

                //Debug.Log(closestInteractionElement + "  " + closestInteractionElementDistance + "  " + closestVisualizationElement + "  " + closestVisualizationElementDistance);

                if (closestVisualizationElementDistance < float.MaxValue)
                {
                    // this penalty is used to ensure that interaction elements being on the same distance than visualization
                    // elements are preferred.
                    closestVisualizationElementDistance += 0.001f;
                }

                if ((closestInteractionElement != null) &&
                    (closestInteractionElementDistance <= closestVisualizationElementDistance))
                {
                    closestVPElement = closestInteractionElement;
                }

                if ((closestVisualizationElement != null) &&
                    (closestVisualizationElementDistance <= closestInteractionElementDistance))
                {
                    closestVPElement = closestVisualizationElement;
                }
            }

            return closestVPElement;
        }

        /**
         * Loads the resources of the VP (Prefab and JSON files)
         * If InstantiatedByObjectPositioner is false, this method also triggers the instantiation of the VP 
         */
        IEnumerator LoadPrototypeResources(string url, string prefabName)
        {
            resourceLoader = null;

            if (url.Contains("://") || url.StartsWith("AssetBundles/") || url.StartsWith("StreamingAssets/"))
            {
                resourceLoader = new AssetBundleResourceLoader(url);
            }
            else
            {
                resourceLoader = new PackedResourceLoader(url);
            }

            yield return resourceLoader.Init();

            this.virtualPrototypePrefab = resourceLoader.LoadAsset<GameObject>(prefabName);

            if (this.virtualPrototypePrefab == null)
            {
                throw new ArgumentException("cannot retrieve a virtual prototype prefab name " + prefabName);
            }
            
            Debug.Log("loaded resources");

            // load all elements of the prototype
            this.interactionElementSpecs =
                this.GetFromJSON<InteractionElementSpecArrayJSONWrapper>(resourceLoader, "InteractionElements.json").GetSpecsArray();

            this.visualizationElementSpecs =
                this.GetFromJSON<VisualizationElementSpecArrayJSONWrapper>(resourceLoader, "VisualizationElements.json").GetSpecsArray();

            this.visualizationArraySpecs =
                this.GetFromJSON<VisualizationArraySpecArrayJSONWrapper>(resourceLoader, "VisualizationArrays.json").GetSpecsArray(visualizationElementSpecs);

            this.allVisualizationElements =
                new VisualizationSpec[visualizationElementSpecs.Length + visualizationArraySpecs.Length];

            visualizationElementSpecs.CopyTo(allVisualizationElements, 0);
            visualizationArraySpecs.CopyTo(allVisualizationElements, visualizationElementSpecs.Length);

            Debug.Log("loaded prototype configuration");
            yield return null;

            // load the state machine
            this.states =
                this.GetFromJSON<StateSpecArrayJSONWrapper>(resourceLoader, "States.json").GetSpecsArray(interactionElementSpecs, allVisualizationElements);

            this.transitionSpecs =
                this.GetFromJSON<TransitionSpecArrayJSONWrapper>(resourceLoader, "Transitions.json").GetSpecsArray(states, interactionElementSpecs);

            Debug.Log("loaded prototype statemachine");
            yield return null;

            this.isLoadingReady = true;
            
            // Once the resources are loaded, instantiate the prototype 
            // Only instantiate if not triggered by the ObjectPositioner 
            if (!this.InstantiatedByObjectPositioner) {
                InstantiatePrototype();
            }
        }

        /*
         * Instantiates the Virtual Prototype
         */
        public void InstantiatePrototype()
        {
            // if there is already an instance of the prefab as the child of this, reuse it, else instantiate it
            bool foundPrefabInstance = false;

            for (int i = 0; i < this.transform.childCount; i++) {
                GameObject child = this.transform.GetChild(i).gameObject;
                if (child.name == this.virtualPrototypePrefab.name)
                {
                    this.prototypeInstance = child;
                    foundPrefabInstance = true;
                    Debug.Log("There is already an object with the same name of the prefab to be instantiated (" +
                              this.virtualPrototypePrefab.name + "). Reusing the object as prototype instance and " +
                              "crossing fingers that this object is the right one.");
                    break;
                }
            }

            if (!foundPrefabInstance)
            {
                // Instantiate at position (0, 0, 0) and zero rotation.
                this.prototypeInstance = Instantiate(this.virtualPrototypePrefab, new Vector3(0, 0, 0), Quaternion.identity);
                //this.PrototypeInstance = Instantiate(virtualPrototypePrefab, new Vector3(0, 0, 0), Quaternion.Euler(new Vector3(-5, -45, 10)));
                this.prototypeInstance.name = this.virtualPrototypePrefab.name;
                this.prototypeInstance.transform.SetParent(this.transform, false);
            }

            // finally, create the state machine, register all prototype elements and start the state machine
            this.StateMachine = new StateMachine(transitionSpecs, this);

            CreateVisualizationElements(visualizationElementSpecs, resourceLoader);
            CreateVisualizationArrays(visualizationArraySpecs);
            CreateInteractionElements(interactionElementSpecs);
            CreateOtherVirtualPrototypeElements(this.prototypeInstance.transform);

            Debug.Log("created elements");
            if (states.Length > 0)
            {
                this.StateMachine.Start(states[0]);
                Debug.Log("started state machine");
            }
            else
            {
                Debug.Log("state machine not started as no states are configured");
            }

            this.Loaded = true;
        } 

        /*
         * 
         */
        private T GetFromJSON<T>(IResourceLoader resourceLoader, string fileName)
        {
            TextAsset textAsset = resourceLoader.LoadAsset<TextAsset>(fileName);
            if (textAsset == null)
            {
                throw new ArgumentException("no " + fileName + " found in prototype bundle");
            }

            return JsonUtility.FromJson<T>(textAsset.text);
        }

        /*
         * 
         */
        private void CreateInteractionElements(InteractionElementSpec[] elementSpecs)
        {
            // we first need to order the interaction elements so that we create them starting with the deepest nodes
            // and finishing with the highest parent nodes
            List<KeyValuePair<int, InteractionElementSpec>> sortedElements = new List<KeyValuePair<int, InteractionElementSpec>>();

            foreach (InteractionElementSpec elementSpec in elementSpecs)
            {
                GameObject element = GameObject.Find(elementSpec.Name);

                if (element == null)
                {
                    throw new ArgumentException("could not find object with name " + elementSpec.Name);
                }

                int depth = 1;
                Transform transform = element.transform;

                while (transform.parent != null)
                {
                    depth++;
                    transform = transform.parent;
                }

                bool added = false;

                for (int i = 0; i < sortedElements.Count; i++)
                {
                    if (depth > sortedElements[i].Key)
                    {
                        sortedElements.Insert(i, new KeyValuePair<int, InteractionElementSpec>(depth, elementSpec));
                        added = true;
                        break;
                    }
                }

                if (!added)
                {
                    sortedElements.Add(new KeyValuePair<int, InteractionElementSpec>(depth, elementSpec));
                }
            }

            foreach (KeyValuePair<int, InteractionElementSpec> element in sortedElements)
            {
                GameObject interactionElement = CreateInteractionElement(element.Value);

                if (interactionElement == null)
                {
                    throw new ArgumentException("could not find object with name " + element.Value.Name);
                }
            }


        }

        /*
         * 
         */
        private GameObject CreateInteractionElement(InteractionElementSpec elementSpec)
        {
            GameObject effectiveElement = GameObject.Find(elementSpec.Name);

            if (effectiveElement != null)
            {
                GameObject interactionElementGo = new GameObject("ColliderObject" + elementSpec.Name);

                interactionElementGo.transform.SetParent(effectiveElement.transform, false);

                InteractionElement interactionElement;

                if (elementSpec is ToggleButtonSpec)
                {
                    interactionElement = interactionElementGo.AddComponent<ToggleButtonElement>();
                    ((ToggleButtonElement)interactionElement).Initialize((ToggleButtonSpec)elementSpec, effectiveElement);
                }
                else if (elementSpec is ButtonSpec)
                {
                    interactionElement = interactionElementGo.AddComponent<ButtonElement>();
                    ((ButtonElement)interactionElement).Initialize((ButtonSpec)elementSpec, effectiveElement);
                }
                else if (elementSpec is SliderSpec)
                {
                    interactionElement = interactionElementGo.AddComponent<SliderElement>();
                    ((SliderElement)interactionElement).Initialize((SliderSpec)elementSpec, effectiveElement);
                }
                else if (elementSpec is RotatableSpec)
                {
                    interactionElement = interactionElementGo.AddComponent<RotatableElement>();
                    ((RotatableElement)interactionElement).Initialize((RotatableSpec)elementSpec, effectiveElement);
                }
                else if (elementSpec is TouchAreaSpec)
                {
                    interactionElement = interactionElementGo.AddComponent<TouchElement>();
                    ((TouchElement)interactionElement).Initialize((TouchAreaSpec)elementSpec, effectiveElement);
                }
                else if (elementSpec is MovableSpec)
                {
                    interactionElement = interactionElementGo.AddComponent<MovableElement>();
                    ((MovableElement)interactionElement).Initialize((MovableSpec)elementSpec, effectiveElement);
                }
                else
                {
                    throw new NotSupportedException("interaction element spec of type " + elementSpec.GetType() + " not supported");
                }

                // register the state machine to handle events
                interactionElement.VPElementEvent += this.StateMachine.HandleInteractionEvent;

                return interactionElementGo;
            }

            return null;
        }

        /*
         * 
         */
        private void CreateVisualizationElements(VisualizationElementSpec[] elementSpecs, IResourceLoader resourceLoader)
        {
            foreach (VisualizationElementSpec elementSpec in elementSpecs)
            {
                GameObject visualizationElement = CreateVisualizationElement(elementSpec, resourceLoader);

                if (visualizationElement == null)
                {
                    throw new ArgumentException("could not find object with name " + elementSpec.Name);
                }
             }
        }

        /*
         * 
         */
        private GameObject CreateVisualizationElement(VisualizationElementSpec elementSpec, IResourceLoader resourceLoader)
        {
            GameObject effectiveElement = GameObject.Find(elementSpec.Name);

            if (effectiveElement != null)
            {
                GameObject visualizationElement = new GameObject("VisualizationObject" + elementSpec.Name);

                visualizationElement.transform.SetParent(effectiveElement.transform, false);

                if (elementSpec is LightSpec)
                {
                    visualizationElement.AddComponent<LightElement>().Initialize((LightSpec)elementSpec, effectiveElement);
                }
                else if (elementSpec is ScreenSpec)
                {
                    visualizationElement.AddComponent<ScreenElement>().Initialize((ScreenSpec)elementSpec, effectiveElement, resourceLoader);
                }
                else if (elementSpec is ParticleSpec)
                {
                    visualizationElement.AddComponent<ParticleElement>().Initialize((ParticleSpec)elementSpec, effectiveElement);
                }
                else if (elementSpec is AnimationSpec)
                {
                    visualizationElement.AddComponent<AnimationElement>().Initialize((AnimationSpec)elementSpec, effectiveElement);
                }
                else if (elementSpec is AppearingObjectSpec)
                {
                    visualizationElement.AddComponent<AppearingElement>().Initialize((AppearingObjectSpec)elementSpec, effectiveElement);
                }
                else if (elementSpec is SoundSourceSpec)
                {
                    visualizationElement.AddComponent<SoundSourceElement>().Initialize((SoundSourceSpec)elementSpec, effectiveElement, resourceLoader);
                }
                else
                {
                    throw new NotSupportedException("visualization spec of type " + elementSpec.GetType() + " not supported");
                }

                return visualizationElement;
            }

            return null;
        }

        /*
         * 
         */
        private void CreateVisualizationArrays(VisualizationArraySpec[] arraySpecs)
        {
            GameObject visualizationArrays = new GameObject("VisualizationArrays");
            visualizationArrays.transform.parent = this.transform;

            foreach (VisualizationArraySpec arraySpec in arraySpecs)
            {
                GameObject visualizationArray = new GameObject("VisualizationArray" + arraySpec.Name);

                if (arraySpec is LightArraySpec)
                {
                    visualizationArray.AddComponent<LightArrayElement>().Initialize((LightArraySpec)arraySpec);
                }
                else
                {
                    throw new NotSupportedException("cannot handle visualization arrays of type " + arraySpec.GetType());
                }

                visualizationArray.transform.SetParent(visualizationArrays.transform, false);
            }

        }

        /*
         * 
         */
        private void CreateOtherVirtualPrototypeElements(Transform transform)
        {
            bool isInteractionElement = false;

            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).GetComponent<VirtualPrototypeElement>() != null)
                {
                    isInteractionElement = true;
                    break;
                }
            }

            if (!isInteractionElement)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    CreateOtherVirtualPrototypeElements(transform.GetChild(i));
                }

                GameObject vpElementObject = new GameObject("VPElementObject" + transform.name);

                vpElementObject.transform.SetParent(transform, false);
                vpElementObject.AddComponent<VirtualPrototypeElement>().Initialize(transform.gameObject);
            }
        }

        /*
         * 
         */
        public void SetPrototypeActive(bool active)
        {
            prototypeInstance.SetActive(active);
        }

        /*
         * 
         */
        public void SetPrototypePosition(Vector3 position)
        {
            prototypeInstance.transform.position = position;
        }

        /*
         * 
         */
        public void SetPrototypeRotation(Quaternion rotation)
        {
            prototypeInstance.transform.rotation = rotation;
        }

        /*
         * Getter for the VP's prefab
         */
        public GameObject GetVirtualPrototypePrefab()
        {
            return this.virtualPrototypePrefab;
        }

        /*
         * Returns true if the LoadPrototypeResources() function has finished executing
         */
        public bool IsResourceLoadingReady()
        {
            return this.isLoadingReady;
        }
    }
}
