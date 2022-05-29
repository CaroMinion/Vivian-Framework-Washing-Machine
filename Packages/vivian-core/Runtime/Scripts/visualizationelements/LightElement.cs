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

using UnityEngine;

namespace de.ugoe.cs.vivian.core
{
    /**
     * This class represents a light for visualizing values
     */
    public class LightElement : VisualizationElement<LightSpec, float>
    {
        /** the mesh renderer component used for visualization */
        private MeshRenderer MeshRenderer;

        /** the light component used for visualization */
        private Light Light;

        /**
         * Called to initialize the visualization element with the specification and the represented game object
         */
        internal new void Initialize(LightSpec spec, GameObject representedObject)
        {
            base.Initialize(spec, representedObject);

            MeshFilter meshFilter = representedObject.GetComponent<MeshFilter>();

            if (meshFilter != null)
            {
                MeshFilter meshFilterCopy = (MeshFilter)this.gameObject.AddComponent(meshFilter.GetType());
                Utils.CopyComponentValues(meshFilterCopy, meshFilter);
            }

            MeshRenderer originalMeshRenderer = representedObject.GetComponent<MeshRenderer>();

            if (originalMeshRenderer != null)
            {
                this.MeshRenderer = (MeshRenderer)this.gameObject.AddComponent(originalMeshRenderer.GetType());
                Utils.CopyComponentValues(this.MeshRenderer, originalMeshRenderer);

                this.MeshRenderer.material = new Material(Shader.Find("UI/Default"));
                //this.Material.SetColor("_Color", Color.clear);
                this.MeshRenderer.material.color = new Color(this.Spec.EmissionColor.r, this.Spec.EmissionColor.g, this.Spec.EmissionColor.b, 0);
            }

            this.Light = representedObject.GetComponent<Light>();
        }

        /**
         * Called to visualize a bool value
         */
        public override void Visualize(bool value)
        {
            if (value)
            {
                if (this.MeshRenderer != null)
                {
                    this.MeshRenderer.material.color = new Color(this.Spec.EmissionColor.r, this.Spec.EmissionColor.g, this.Spec.EmissionColor.b, 1);
                }

                if (this.Light != null)
                {
                    this.Light.enabled = true;
                }

                this.Value = 1.0f;
            }
            else
            {
                if (this.MeshRenderer != null)
                {
                    this.MeshRenderer.material.color = new Color(this.Spec.EmissionColor.r, this.Spec.EmissionColor.g, this.Spec.EmissionColor.b, 0);
                }

                if (this.Light != null)
                {
                    this.Light.enabled = false;
                }

                this.Value = 1.0f;
            }
        }

        /**
         * Called to visualize a float value
         */
        public override void Visualize(float value)
        {
            if (value > 0.0)
            {
                if (this.MeshRenderer != null)
                {
                    this.MeshRenderer.material.color = new Color(this.Spec.EmissionColor.r, this.Spec.EmissionColor.g, this.Spec.EmissionColor.b, value);
                }

                if (this.Light != null)
                {
                    this.Light.enabled = true;
                }
            }
            else
            {
                if (this.MeshRenderer != null)
                {
                    this.MeshRenderer.material.color = new Color(this.Spec.EmissionColor.r, this.Spec.EmissionColor.g, this.Spec.EmissionColor.b, 0);
                }

                if (this.Light != null)
                {
                    this.Light.enabled = false;
                }
            }

            this.Value = value;
        }
    }
}
