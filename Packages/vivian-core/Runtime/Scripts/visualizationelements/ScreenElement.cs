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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace de.ugoe.cs.vivian.core
{
    /**
     * This class represents a screen for visualizing content
     */
    public class ScreenElement : VisualizationElement<ScreenSpec, string>
    {
        /** the image component used for visualization */
        private RawImage Image;

        /** the list of visualized texts */
        private readonly List<GameObject> Texts = new List<GameObject>();

        /** the asset bundle to use for loading screen content */
        private IResourceLoader ResourceLoader;

        /** the content of the currently shown file */
        private Texture2D ImageContent = null;

        /** the video player component used for visualization */
        private VideoPlayer VideoPlayer;

        /** the content of the currently shown file */
        private VideoClip VideoContent;

        /** the currently selected screen content */
        private string ScreenContent = null;

        /**
         * Called to initialize the visualization element with the specification and the represented game object
         */
        internal new void Initialize(ScreenSpec spec, GameObject representedObject)
        {
            throw new System.NotSupportedException("you need to call the other initialize method for this component");
        }

        /**
         * Called to initialize the visualization element with the specification and the represented game object
         */
        internal void Initialize(ScreenSpec spec, GameObject representedObject, IResourceLoader resourceLoader)
        {
            // First initialize the canvas and its transform, afterwards initialize the base object.
            // only through this, the case class will calcuate a correct collider
            this.InitializeCanvas(spec, representedObject);
            base.Initialize(spec, representedObject);

            this.ResourceLoader = resourceLoader;

            if (spec.IsVideo)
            {
                RenderTextureDescriptor descriptor = new RenderTextureDescriptor();
                descriptor.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
                descriptor.width = 256;
                descriptor.height = 256;
                descriptor.volumeDepth = 1;
                descriptor.msaaSamples = 1;
                descriptor.colorFormat = RenderTextureFormat.ARGB32;
                descriptor.depthBufferBits = 24;
                descriptor.useDynamicScale = false;
                descriptor.autoGenerateMips = true;

                RenderTexture renderTexture = new RenderTexture(descriptor);
                renderTexture.filterMode = FilterMode.Bilinear;
                renderTexture.useMipMap = false;
                renderTexture.wrapMode = TextureWrapMode.Clamp;

                this.Image.texture = renderTexture;

                this.VideoPlayer = this.Image.gameObject.AddComponent<VideoPlayer>();
                // render the video to the render texture given to the raw image
                this.VideoPlayer.targetTexture = (RenderTexture)this.Image.texture;
                // Match the ratio to the screen's
                this.VideoPlayer.aspectRatio = VideoAspectRatio.Stretch;
            }

            this.HideContent();
        }

        /**
         * Called to visualize any value
         */
        public override void Visualize(object value)
        {
            if (value is bool)
            {
                this.Visualize((bool)value);
            }
            else if (value is float)
            {
                this.Visualize((float)value);
            }
            else if (value is string)
            {
                this.Visualize((string)value);
            }
            else if (value is ScreenContentVisualizationSpec)
            {
                this.Visualize((ScreenContentVisualizationSpec)value);
            }
            else
            {
                throw new System.NotSupportedException("cannot visualize values of type " + value.GetType());
            }
        }

        /**
         * Called to visualize a bool value
         */
        public override void Visualize(bool value)
        {
            if (value)
            {
                ShowContent();
            }
            else
            {
                HideContent();
            }
        }

        /**
         * Called to visualize a float value
         */
        public override void Visualize(float value)
        {
            if (value > 0.0)
            {
                ShowContent();
            }
            else
            {
                HideContent();
            }
        }

        /**
         * Called to visualize a string value, which is supposed to be screen content
         */
        internal void Visualize(string value)
        {
            if (!base.Spec.IsVideo)
            {
                this.ImageContent = this.ResourceLoader.LoadAsset<Texture2D>(value);
            }
            else
            {
                if (!value.Contains("://"))
                {
                    this.VideoContent = this.ResourceLoader.LoadAsset<VideoClip>(value);
                    this.VideoPlayer.clip = this.VideoContent;
                }
                else
                {
                    this.VideoPlayer.url = value;
                }
            }

            this.ScreenContent = value;
            ShowContent();
        }

        /**
         * Called to visualize a complete ScreenContentVisualizationSpec including a file and texts
         */
        internal void Visualize(ScreenContentVisualizationSpec value)
        {
            // set the main view
            if (value.FileName != null)
            {
                this.Visualize((string)value.FileName);
            }

            // destroy existing texts
            for (int i = 0; i < this.Texts.Count; i++)
            {
                Destroy(this.Texts[i]);
            }

            // create new ones if required
            if (value.Texts != null)
            {
                for (int i = 0; i < value.Texts.Length; i++)
                {
                    this.Texts.Add(CreateText(value.Texts[i], i));
                }
            }

            ShowContent();
        }

        /**
         * convenience method to actually show the image content
         */
        private void ShowContent()
        {
            if (!base.Spec.IsVideo)
            {
                this.Image.texture = this.ImageContent;
            }
            else
            {
                this.VideoPlayer.Play();
            }

            this.Image.color = Color.white;
            this.Value = this.ScreenContent;
        }

        /**
         * convenience method to actually hide the image content
         */
        private void HideContent()
        {
            if (!base.Spec.IsVideo)
            {
                this.Image.texture = null;
            }
            else
            {
                this.VideoPlayer.Stop();
            }

            this.Image.color = Color.clear;
            this.Value = null;
        }

        /**
         * Convenience method to initialize the canvas and the rect transform
         */
        private void InitializeCanvas(ScreenSpec spec, GameObject representedObject)
        {
            // add a canvas to draw the screen content onto.
            Canvas canvas = this.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            // scale the canvas to that it fits the object size
            Mesh mesh = representedObject.GetComponent<MeshFilter>().mesh;
            RectTransform transform = this.gameObject.GetComponent<RectTransform>();

            Vector3 effectiveSize = Vector3.Scale(mesh.bounds.size, representedObject.transform.lossyScale);

            transform.localScale = new Vector3(mesh.bounds.size.x / spec.Resolution.x,
                                               mesh.bounds.size.y / spec.Resolution.y,
                                               mesh.bounds.size.z);

            // set the resolution of the canvas to the provided one
            transform.sizeDelta = spec.Resolution;

            // move the canvas so that it is minimally above the screen
            transform.Translate((Vector3.Scale(spec.Plane.normalized, effectiveSize) / 2) + (0.0005f * spec.Plane.normalized));

            // compensate for the usually wrong orientation of the canvas (away from camera)
            transform.rotation = representedObject.transform.rotation * Quaternion.LookRotation(-spec.Plane);

            // add an image object to draw image contents
            GameObject imageObject = new GameObject("ScreenContent" + spec.Name);
            imageObject.transform.parent = this.gameObject.transform;

            this.Image = imageObject.AddComponent<RawImage>();
            transform = imageObject.GetComponent<RectTransform>();
            transform.sizeDelta = spec.Resolution;
            transform.localPosition = Vector3.zero;
            transform.localScale = new Vector3(1, 1, 1);
            transform.rotation = new Quaternion();
        }

        /**
         * Convenience method to create some text element
         */
        private GameObject CreateText(TextSpec spec, int index)
        {
            GameObject textObject = new GameObject("ScreenContentText" + index);
            textObject.transform.parent = this.gameObject.transform;

            Text text = textObject.AddComponent<Text>();
            text.font = Font.CreateDynamicFontFromOSFont("OpenSans", spec.Size);
            text.text = spec.Text;
            text.fontSize = spec.Size;

            Color newColor = new Color();
            if (ColorUtility.TryParseHtmlString(spec.Color, out newColor))
            {
                text.color = newColor;
            }
            else
            {
                Debug.LogWarning("color code \"" + spec.Color + "\" of text \"" + spec.Text +
                                 "\" not recognized. Using default black instead.");
                text.color = Color.black;
            }

            text.alignment = TextAnchor.MiddleCenter;

            RectTransform transform = textObject.GetComponent<RectTransform>();
            transform.sizeDelta = base.Spec.Resolution;
            transform.localPosition = new Vector3(spec.Position.x - (base.Spec.Resolution.x / 2),
                                                  -spec.Position.y + (base.Spec.Resolution.y / 2), 0);
            transform.localScale = new Vector3(1, 1, 1);
            transform.rotation = new Quaternion();

            return textObject;
        }
    }
}
