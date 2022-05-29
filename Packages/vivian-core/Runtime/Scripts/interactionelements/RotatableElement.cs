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

namespace de.ugoe.cs.vivian.core
{
    /**
     * This class represents a rotatable element. It registers the start of a rotation, the actual rotation,
     * and the end of a rotation.
     */
    public class RotatableElement : PositionableElement<RotatableSpec>
    {
        /** stores the parent space axis around which the rotatable rotates */
        private AxisSpec parentLocalRotationAxis;

        /** stores the current rotation angle of the rotatable */
        private float currentAngle;

        /** during a rotation, stores the sphere around the interaction axis origin on which the interaction takes place */
        private InteractionSphere interactionSphere;

        /** during a rotation, stores the rotation vector of the last frame */
        private Vector3 rotationVectorOfLastFrame;

        /** stores the intended angle of the rotatable which may exceed the boundaries */
        private float intendedAngle;


        /**
         * initializes the member variables and sets the knob to the initial value given by the specification
         */
        internal override void Initialize(RotatableSpec spec, GameObject representedObject)
        {
            base.Value = 0f;

            parentLocalRotationAxis = new AxisSpec
                (representedObject.transform.localPosition + spec.RotationAxis.Origin, spec.RotationAxis.Direction);

            base.Initialize(spec, representedObject);
        }

        /**
         * called to apply a certain attribute
         */
        internal override void SetAttribute(InteractionElementSpec.Attribute attribute, object value)
        {
            switch (attribute)
            {
                case InteractionElementSpec.Attribute.VALUE:
                    if (!(value is float))
                    {
                        throw new ArgumentException("setting the attribute VALUE to a rotatable element requires " +
                                                    "a value of type float but was " + value + " when setting it for the rotatable element " +
                                                    base.Spec.Name);
                    }

                    //Debug.Log(value);
                    RotateToValue((float)value);
                    break;

                case InteractionElementSpec.Attribute.FIXED:
                    base.SetAttribute(attribute, value); break;

                default:
                    throw new ArgumentException("the rotatable element " + base.Spec.Name + " cannot handle an attribute of type " +
                                                attribute + "with the value " + value + " of type " + value.GetType() + ". Allowed are: " +
                                                InteractionElementSpec.Attribute.VALUE + " of type float, " +
                                                InteractionElementSpec.Attribute.FIXED + " of type bool");
            }
        }

        /**
         * handles the beginning of a rotation change
         */
        public override void TriggerInteractionStarts(Pose pose)
        {
            Debug.Log(base.Spec.Name + ": ROTATABLE_DRAG_START " + base.Value);

            // determine, where this object was hit to identify the interaction sphere
            AxisSpec effectiveRotationAxis = GetRotationAxisInWorldSpace();
            Ray ray = new Ray(pose.position, pose.forward);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Collider[] colliders = this.GetComponents<Collider>();
                bool ownColliderHit = false;

                foreach (Collider candidate in colliders)
                {
                    if (candidate == hit.collider)
                    {
                        ownColliderHit = true;
                        break;
                    }
                }

                if (ownColliderHit)
                {
                    Vector3 relativeInteractionPoint = hit.point - effectiveRotationAxis.Origin;
                    this.interactionSphere =
                        new InteractionSphere(effectiveRotationAxis.Origin, relativeInteractionPoint.magnitude);
                }
            }

            if (this.interactionSphere == null)
            {
                // this should never happen, but if it does, try to determine a sphere with a radius being the average
                // distance of all mesh points to the sphere
                try
                {
                    float distance = 0;
                    float verticeCount = 0;
                    MeshFilter[] meshFilters = base.RepresentedObject.GetComponentsInChildren<MeshFilter>();

                    foreach (MeshFilter meshFilter in meshFilters)
                    {
                        // the object requires a collider --> calculate one
                        Mesh mesh = meshFilter.sharedMesh;

                        if (mesh == null)
                        {
                            mesh = meshFilter.mesh;
                        }

                        if (mesh != null)
                        {
                            foreach (Vector3 vertice in mesh.vertices)
                            {
                                distance += (meshFilter.transform.TransformPoint(vertice) - effectiveRotationAxis.Origin).magnitude;
                                verticeCount++;
                            }
                        }
                    }

                    if (verticeCount > 0)
                    {
                        this.interactionSphere =
                            new InteractionSphere(effectiveRotationAxis.Origin, distance / verticeCount);
                    }
                }
                catch (Exception)
                {
                    // ignore
                }
            }


            if (this.interactionSphere == null)
            {
                // in this case, we take the radius to be a quarter of the distance between the ray origin and the object
                this.interactionSphere =
                    new InteractionSphere(effectiveRotationAxis.Origin, (ray.origin - effectiveRotationAxis.Origin).magnitude / 4);
            }

            rotationVectorOfLastFrame = Vector3.zero;
            intendedAngle = currentAngle;

            base.RaiseInteractionElementEvent(EventSpec.ROTATABLE_DRAG_START,
                                              new KeyValuePair<EventParameterSpec, float>(EventParameterSpec.SELECTED_VALUE, (float) base.Value));
        }

        /**
         * handles the roation of the rotatable. The ray defined by the pose is considered to show the user intended rotation of the rotatable.
         * The rotatable follows the ray. If the ray leaves the rotatable, the rotatable still follows it measuring only the roation angle.
         */
        public override void TriggerInteractionContinues(Pose pose)
        {
            bool raiseEvent = false;

            if (!this.IsFixed)
            {
                // we first determine a plane in which the knob is supposed to rotate. This is given by the rotation axis.
                AxisSpec effectiveRotationAxis = GetRotationAxisInWorldSpace();
                Plane interactionPlane = new Plane(effectiveRotationAxis.Direction, effectiveRotationAxis.Origin);

                // we update the interaction sphere's center as it may have moved
                this.interactionSphere.center = effectiveRotationAxis.Origin;

                // then we check, where the ray interacts with the interaction sphere
                Ray ray = new Ray(pose.position, pose.forward);
                Vector3 interactionPoint = this.interactionSphere.GetClosestIntersectionOrProjectionFromCenter(ray);

                // now we project the interaction point onto the interaction plane
                interactionPoint = interactionPlane.ClosestPointOnPlane(effectiveRotationAxis.Origin + interactionPoint);

                // Then we determine the intended rotation vector.
                Vector3 anticipatedRotation = interactionPoint - effectiveRotationAxis.Origin;

                if (rotationVectorOfLastFrame != Vector3.zero)
                {
                    // determine the angle between the last and the new vector pointed at by the user. It
                    // represents the intended change
                    float angularChange = Vector3.SignedAngle(rotationVectorOfLastFrame, anticipatedRotation, effectiveRotationAxis.Direction);

                    if (Math.Abs(angularChange) > 0.1)
                    {
                        this.intendedAngle += angularChange;
                        RotateToAngle(this.intendedAngle);
                        raiseEvent = true;
                    }
                }

                rotationVectorOfLastFrame = anticipatedRotation;
            }

            if (raiseEvent)
            {
                base.RaiseInteractionElementEvent(EventSpec.ROTATABLE_DRAG,
                                                  new KeyValuePair<EventParameterSpec, float>(EventParameterSpec.SELECTED_VALUE, (float)base.Value));

                //Debug.Log(base.Spec.Name + ": ROTATABLE_DRAG " + base.Value);
            }
        }

        /**
         * handles the end of a rotation change
         */
        public override void TriggerInteractionEnds(Pose pose)
        {
            rotationVectorOfLastFrame = Vector3.zero;
            interactionSphere = null;

            // call this to ensure matching the position resolution
            this.RotateToValue((float)base.Value);

            base.RaiseInteractionElementEvent(EventSpec.ROTATABLE_DRAG_END,
                                              new KeyValuePair<EventParameterSpec, float>(EventParameterSpec.SELECTED_VALUE, (float)base.Value));

            Debug.Log(base.Spec.Name + ": ROTATABLE_DRAG_END " + base.Value);
        }

        /**
         * convenience method to set the knob to a defined value
         */
        private void RotateToValue(float value)
        {
            float effectiveValue = value;

            if (base.Spec.PositionResolution < int.MaxValue)
            {
                effectiveValue = (float)Math.Round(value * (base.Spec.PositionResolution - 1), 0) / (base.Spec.PositionResolution - 1);
                //Debug.Log("adapt value to resolution " + effectiveValue);
            }

            float rotationAngle = base.Spec.MinRotation + ((base.Spec.MaxRotation - base.Spec.MinRotation) * effectiveValue);
            RotateToAngle(rotationAngle);
        }

        /**
         * convenience method to set the knob to a defined angle
         */
        private void RotateToAngle(float rotationAngle)
        {
            // we apply the rotation angle on the rotatable. We consider the max and min value correctly by not applying smaller
            // or larger rotations
            if (rotationAngle < base.Spec.MinRotation)
            {
                rotationAngle = base.Spec.MinRotation;
            }
            else if (rotationAngle > base.Spec.MaxRotation)
            {
                rotationAngle = base.Spec.MaxRotation;
            }

            if (Math.Abs(rotationAngle - this.currentAngle) > 1)
            {
                AxisSpec effectiveRotationAxis = GetRotationAxisInWorldSpace();
                base.RepresentedObject.transform.RotateAround(effectiveRotationAxis.Origin, effectiveRotationAxis.Direction, rotationAngle - this.currentAngle);

                this.currentAngle = rotationAngle;
                base.Value = (rotationAngle - base.Spec.MinRotation) / (base.Spec.MaxRotation - base.Spec.MinRotation);
            }
        }

        /**
         * convenience method to get the rotation axis in world space
         */
        private AxisSpec GetRotationAxisInWorldSpace()
        {
            return new AxisSpec
                (base.RepresentedObject.transform.parent.TransformPoint(parentLocalRotationAxis.Origin),
                 base.RepresentedObject.transform.parent.TransformVector(parentLocalRotationAxis.Direction));
        }

        /**
         * convenience class to represent the interaction sphere
         */
        internal class InteractionSphere
        {
            /** the center of the sphere */
            internal Vector3 center { private get; set; }

            /** the radius of the sphere */
            private float radius;

            /**
             * 
             */
            internal InteractionSphere(Vector3 center, float radius)
            {
                this.center = center;
                this.radius = radius;
            }

            /**
             * Determines a position on the sphere in reference to the sphere center depending on the ray.
             * If the ray intersects the sphere, the position is the intersection point of the ray closest
             * to the ray's origin. If the ray does not intersect with the sphere, it is the closest point
             * of the sphere in relation to the ray.
             * 
             * The intersection with the sphere is calculated as follows: The sphere is defined as
             * 
             *   (x - center.x)² + (y - center.y)² + (z - center.z) = radius²
             * 
             * The ray is considered to be a line defined as
             *   x = ray.origin.x + t * ray.direction.x
             *   y = ray.origin.y + t * ray.direction.y
             *   z = ray.origin.z + t * ray.direction.z
             * 
             * We put the coordinates of the ray into the sphere formula and then need to solve
             * 
             *   (ray.origin.x + t * ray.direction.x - center.x)² +
             *   (ray.origin.y + t * ray.direction.y - center.y)² +
             *   (ray.origin.z + t * ray.direction.z - center.z)² = radius²
             * 
             * We know all of these values except t. We rewrite the formula to show this. We can combine the
             * available values, being ray.origin and the sphere center:
             * 
             *   ((ray.origin.x - center.x) + t * ray.direction.x)² +
             *   ((ray.origin.y - center.y) + t * ray.direction.y)² +
             *   ((ray.origin.z - center.z) + t * ray.direction.z)² = radius²
             * 
             * We make this easier to read by replacing (ray.origin.x - center.x) with a.x, etc. and
             * ray.direction.x with d.x, etc. This results in:
             * 
             *   (a.x + t * d.x)² + (a.y + t * d.y)² + (a.z + t * d.z)² = radius²
             * 
             * Then we use binomic formulas (a + b)² = a² + 2ab + b² to clear the squares:
             * 
             *   a.x² + 2 * a.x * t * d.x + (t * d.x)² +
             *   a.y² + 2 * a.y * t * d.y + (t * d.y)² +
             *   a.z² + 2 * a.z * t * d.z + (t * d.z)² = radius²
             * 
             * Now we create an easier to read square function out of it by removing the remaining
             * brakets and reformulating everything:
             * 
             *   (d.x² + d.y² + d.z²) * t² +
             *   2 * (a.x * d.x + a.y * d.y + a.z * d.z) * t +
             *   a.x² + a.y² + a.z² - radius² = 0
             * 
             * We see, that there are two dot products in there, one being dd, one being ad, and one being aa. Hence,
             * the fomula actually means:
             * 
             *   dd * t² + 2 * ad * t + aa - radius² = 0
             * 
             * Then we divide by dd resulting in:
             *   
             *         2 * ad     aa - radius²
             *   t² +  ------ t + ------------ = 0
             *           dd            dd
             * 
             * This corresponds to t² + pt + q = 0 and can be solved with 
             * 
             *                          _________
             *             p           / p²
             *   t1,2 = - ---  +/-    / ---  - q
             *             2        \/   4
             */
            internal Vector3 GetClosestIntersectionOrProjectionFromCenter(Ray ray)
            {
                Vector3 closestPointOnRay =
                    ray.origin + Vector3.Project(this.center - ray.origin, ray.direction);

                Vector3 centerToClosestPointOnRay = closestPointOnRay - this.center;

                if (centerToClosestPointOnRay.magnitude < radius)
                {
                    // the ray intersects with the sphere --> calculate intersection vector
                    Vector3 a = ray.origin - center;
                    Vector3 d = ray.direction;
                    float dd = Vector3.Dot(d, d);
                    float ad = Vector3.Dot(a, d);
                    float aa = Vector3.Dot(a, a);

                    double p = 2 * ad / dd;
                    double q = (aa - Math.Pow(this.radius, 2)) / dd;

                    double belowTheSquareRoot = (Math.Pow(p, 2) / 4) - q;

                    if (belowTheSquareRoot >= 0)
                    {
                        double phalf = p / 2;
                        double squareRoot = Math.Pow(belowTheSquareRoot, 0.5);

                        float t1 = (float)(-phalf + squareRoot);
                        float t2 = (float)(-phalf - squareRoot);

                        Vector3 result = ray.origin;

                        if (Math.Abs(t1) < Math.Abs(t2))
                        {
                            result = result + (t1 * d);
                        }
                        else
                        {
                            result = result + (t2 * d);
                        }

                        // put the result into coordinates in reference to the sphere center
                        result = result - this.center;

                        return result;
                    }
                }

                // the ray does not intersect with the sphere --> provide projection Vector
                return centerToClosestPointOnRay.normalized * this.radius;
            }

        }
    }
}
