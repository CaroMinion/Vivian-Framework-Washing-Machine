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
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace de.ugoe.cs.vivian.core
{
    public class VivianFramework : MonoBehaviour
    {
    }

    public class Utils
    {
        /**
         * convenience to get all interaction elements instantiated by the framework
         */
        public static InteractionElement[] GetInteractionElements(VirtualPrototype Prototype)
        {
            return Prototype.GetComponentsInChildren<InteractionElement>();
        }
        
        /**
         * convenience to get all virtual prototype elements instantiated by the framework
         */
        public static VirtualPrototypeElement[] GetVirtualPrototypeElements(VirtualPrototype Prototype)
        {
            return Prototype.GetComponentsInChildren<VirtualPrototypeElement>();
        }

        /**
         * creates a box collider based on a given mesh
         */
        public static Collider GetColliderFromMesh(GameObject objectToAddColliderTo, MeshFilter meshFilter)
        {
            Vector3[] boundPoints = GetLocalPointsRepresentingMesh(meshFilter);

            if (boundPoints == null)
            {
                return null;
            }

            Collider collider = objectToAddColliderTo.AddComponent<BoxCollider>();

            for (int i = 0; i < boundPoints.Length; i++)
            {
                //Debug.DrawLine(Vector3.zero, boundPoints[i], Color.green);
                boundPoints[i] = meshFilter.transform.TransformPoint(boundPoints[i]);
                //Debug.DrawLine(Vector3.zero, boundPoints[i], Color.red);
            }

            Bounds bounds = GeometryUtility.CalculateBounds(boundPoints, objectToAddColliderTo.transform.worldToLocalMatrix);

            //Debug.DrawLine(Vector3.zero, bounds.center, Color.yellow);

            // increase the collider size minimally to compensate for rounding issues.
            ((BoxCollider)collider).size = 1.001f * bounds.size;
            ((BoxCollider)collider).center = bounds.center;

            collider.isTrigger = true;

            return collider;
        }

        /**
         * convenience method to get the points representing the mesh best
         */
        public static Vector3[] GetLocalPointsRepresentingMesh(MeshFilter meshFilter)
        {
            if (meshFilter == null)
            {
                return null;
            }

            Mesh mesh = meshFilter.sharedMesh;

            if (mesh == null)
            {
                mesh = meshFilter.mesh;
            }

            if (mesh != null)
            {
                if (mesh.isReadable)
                {
                    return mesh.vertices;
                }
                else
                {
                    // return the bound points instead
                    return new Vector3[] {
                        mesh.bounds.min,
                        mesh.bounds.max,
                        new Vector3(mesh.bounds.min.x, mesh.bounds.min.y, mesh.bounds.max.z),
                        new Vector3(mesh.bounds.min.x, mesh.bounds.max.y, mesh.bounds.min.z),
                        new Vector3(mesh.bounds.max.x, mesh.bounds.min.y, mesh.bounds.min.z),
                        new Vector3(mesh.bounds.min.x, mesh.bounds.max.y, mesh.bounds.max.z),
                        new Vector3(mesh.bounds.max.x, mesh.bounds.min.y, mesh.bounds.max.z),
                        new Vector3(mesh.bounds.max.x, mesh.bounds.max.y, mesh.bounds.min.z)
                    };
                }
            }

            return null;
        }

        /**
         * convenience method to copy component values
         */
        internal static T CopyComponentValues<T>(T comp, T other) where T : Component
        {
            Type type = comp.GetType();

            if (type != other.GetType())
            {
                return null; // type mis-match
            }

            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
            PropertyInfo[] pinfos = type.GetProperties(flags);

            foreach (var pinfo in pinfos)
            {
                if (pinfo.CanWrite)
                {
                    try
                    {
                        pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                    }
                    catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
                }
            }

            FieldInfo[] finfos = type.GetFields(flags);
            foreach (var finfo in finfos)
            {
                finfo.SetValue(comp, finfo.GetValue(other));
            }

            return comp as T;
        }

        /**
         * 
         */
        public static Vector3 ParseVector3(string sVector)
        {
            CultureInfo ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            ci.NumberFormat.CurrencyDecimalSeparator = ".";

            // Remove the parentheses
            if (sVector.StartsWith("(") && sVector.EndsWith(")"))
            {
                sVector = sVector.Substring(1, sVector.Length - 2);
            }

            // split the items
            string[] sArray = sVector.Split(',');

            // store as a Vector3
            Vector3 result = new Vector3(
                float.Parse(sArray[0], NumberStyles.Any, ci),
                float.Parse(sArray[1], NumberStyles.Any, ci),
                float.Parse(sArray[2], NumberStyles.Any, ci));

            return result;
        }


        /**
         * 
         */
        public static Vector2 ParseVector2(string sVector)
        {
            CultureInfo ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            ci.NumberFormat.CurrencyDecimalSeparator = ".";

            // Remove the parentheses
            if (sVector.StartsWith("(") && sVector.EndsWith(")"))
            {
                sVector = sVector.Substring(1, sVector.Length - 2);
            }

            // split the items
            string[] sArray = sVector.Split(',');

            // store as a Vector2
            Vector2 result = new Vector2(
                float.Parse(sArray[0], NumberStyles.Any, ci),
                float.Parse(sArray[1], NumberStyles.Any, ci));

            return result;
        }

        /**
         * 
         */
        public static object ParseValue(string valueStr)
        {
            if (valueStr == null)
            {
                return null;
            }

            // try parsing a float
            try
            {
                CultureInfo ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
                ci.NumberFormat.CurrencyDecimalSeparator = ".";

                return float.Parse(valueStr, NumberStyles.Any, ci);
            }
            catch (Exception)
            {
                // it wasn't a float. Ignore this attempt and try something else
            }

            // try parsing a Vector3
            try
            {
                return ParseVector3(valueStr);
            }
            catch (Exception)
            {
                // it wasn't a vector 3. Ignore this attempt and try something else
            }

            // try parsing a Vector2
            try
            {
                return ParseVector2(valueStr);
            }
            catch (Exception)
            {
                // it wasn't a vector 2. Ignore this attempt and try something else
            }

            // try parsing a bool
            if ("true" == valueStr.ToLower())
            {
                return true;
            }
            else if ("false" == valueStr.ToLower())
            {
                return false;
            }

            // seems to be a normal string
            return valueStr;
        }
    }

    /**
     * Interface for loading ressources, either from asset bundles or from local resources
     */
    interface IResourceLoader
    {
        /**
         * Must be called to initialize the loader, e.g. to prefetch data
         */
        IEnumerator Init();

        /**
         * Loads an individual asset using the loader
         */
        T LoadAsset<T>(string fileName) where T : UnityEngine.Object;
    }

    /**
     * Reader for resources from an asset bundle whose location is defined by a URL.
     * If the URL is relative, the loader considers the asset bundle to be in the
     * project directory.
     */
    class AssetBundleResourceLoader : IResourceLoader
    {
        /** */
        private string url;

        /** */
        private AssetBundle bundle;

        /**
         * 
         */
        public AssetBundleResourceLoader(string url)
        {
            //string url = "file:///" + Application.dataPath + "/AssetBundles/" + assetBundleName;
            this.url = url;

            if (!this.url.Contains("://"))
            {
                // we denote a file on the disk. Check, whether it is an absolute or relative path
                if (this.url.StartsWith("/"))
                {
                    // its an absolute path. Add only file://
                    this.url = "file://" + this.url;
                }
                else if (this.url.StartsWith("StreamingAssets/"))
                {
                    // it's a relative path for Android. Include the jar prefix as well as the location of the project
                    this.url = Path.Combine("jar:file://" + Application.dataPath + "!/assets/", this.url.Substring(url.LastIndexOf('/') +1));
                }
                else
                {
                    // its a relative path. Add the protocol as well as location of the project
                    this.url = "file:///" + Application.dataPath + "/" + this.url;
                }
            }

            Debug.Log(this.url);
        }

        /**
         *
         */
        public IEnumerator Init()
        {
            UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(this.url, 0);
            yield return request.SendWebRequest();
            this.bundle = DownloadHandlerAssetBundle.GetContent(request);

            if (this.bundle == null)
            {
                throw new ArgumentException("cannot retrieve an asset bundle from the provided URL: " + this.url);
            }
            else
            {
                Debug.Log("bundle initialized");
            }
        }

        /**
         * tries to load an asset using the given name. If this fails and the name
         * denotes a path, the path is cut off and the effective name is tried.
         */
        public T LoadAsset<T>(string fileName) where T : UnityEngine.Object
        {
            T retVal = (T)this.bundle.LoadAsset<T>(fileName);

            if (retVal != null)
            {
                return retVal;
            }

            int index = fileName.LastIndexOf('/');

            if (index >= 0)
            {
                retVal = (T)this.bundle.LoadAsset<T>(fileName.Substring(index + 1));
            }

            if (retVal != null)
            {
                return retVal;
            }

            throw new ArgumentException("there is no asset named " + fileName +
                                        " available in the bundle from the provided URL: " + this.url);
        }
    }

    /**
     * Reader for resources from a resources folder whose location is defined by a URL.
     * The URL must always be relative to the project workspace
     */
    class PackedResourceLoader : IResourceLoader
    {
        /** */
        private string[] CHECKED_PATHS = { "", "FunctionalSpecification/", "Screens/" };

        /** */
        private string url;

        /**
         * 
         */
        public PackedResourceLoader(string url)
        {
            this.url = url;
        }

        /**
         * 
         */
        public IEnumerator Init()
        {
            return null;
        }

        /**
         * Loads an asset from the workspace. It first tries to load it by combining
         * the give URL and the given file name. If this fails, it tries to load also
         * from url + "FunctionalSpecification/" + fileName and
         * url + "Screens/" + fileName. This is done for backwards compatibility with
         * older prototype configurations.
         */
        public T LoadAsset<T>(string fileName) where T : UnityEngine.Object
        {
            foreach (string path in CHECKED_PATHS)
            {
                string effectiveUrl = url + "/" + path + fileName;

                int index = effectiveUrl.LastIndexOf('.');

                if (index > 0)
                {
                    effectiveUrl = effectiveUrl.Substring(0, index);
                }

                T retVal = (T)Resources.Load<T>(effectiveUrl);

                if (retVal != null)
                {
                    return retVal;
                }
            }

            throw new ArgumentException("there is no asset named " + fileName +
                                        " available in the resources at " + this.url);
        }
    }
}
