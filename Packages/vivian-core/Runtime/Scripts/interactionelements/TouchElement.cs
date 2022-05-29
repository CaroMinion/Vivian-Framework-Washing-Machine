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

using System.Collections.Generic;
using UnityEngine;

namespace de.ugoe.cs.vivian.core
{
    /**
     * This class represents a touch area.
     */
    public class TouchElement : InteractionElement<TouchAreaSpec>
    {
        /** the plane of the touch screen */
        private Plane Plane;

        /** the top left point of the touch screen */
        private Vector3 TopLeftPoint;

        /** the x-Axis of the touch screen */
        private Vector3 XAxis;

        /** the y-Axis of the touch screen */
        private Vector3 YAxis;

        /**
         * Called to initialize the visualization element with the specification and the represented game object
         */
        internal override void Initialize(TouchAreaSpec spec, GameObject representedObject)
        {
            base.Initialize(spec, representedObject);

            this.InitializePlane();
        }

        /**
         * This is called when the touch area is touched
         */
        public override void TriggerInteractionStarts(Pose pose)
        {
            Vector2 coordinates = GetTouchscreenCoordinates(pose);

            Debug.Log(base.Spec.Name + ": TOUCH_START " + coordinates);

            if (coordinates.x == Mathf.NegativeInfinity)
            {
                // this happens in case the object got moved. We reinitialize the interaction plane and retry.
                this.InitializePlane();
                coordinates = GetTouchscreenCoordinates(pose);
            }

            base.RaiseInteractionElementEvent(EventSpec.TOUCH_START,
                                              new KeyValuePair<EventParameterSpec, float>(EventParameterSpec.TOUCH_X_COORDINATE, coordinates.x),
                                              new KeyValuePair<EventParameterSpec, float>(EventParameterSpec.TOUCH_Y_COORDINATE, coordinates.y));
        }

        /**
         * handles the slide on the touch area. The ray defined by the pose is considered to show the user intended position of the slide.
         */
        public override void TriggerInteractionContinues(Pose pose)
        {
            //Debug.Log("trigger touch slide");

            this.GetEffectiveTouchPlane(out Vector3 topLeftPoint, out Vector3 xAxis, out Vector3 yAxis);

            Vector2 coordinates = GetTouchscreenCoordinates(pose);

            if (coordinates.x == Mathf.NegativeInfinity)
            {
                // this my happen in case the object got moved. We reinitialize the interaction plane and retry.
                this.InitializePlane();
                coordinates = GetTouchscreenCoordinates(pose);
            }

            base.RaiseInteractionElementEvent(EventSpec.TOUCH_SLIDE,
                                              new KeyValuePair<EventParameterSpec, float>(EventParameterSpec.TOUCH_X_COORDINATE, coordinates.x),
                                              new KeyValuePair<EventParameterSpec, float>(EventParameterSpec.TOUCH_Y_COORDINATE, coordinates.y));

            //Debug.Log(base.Spec.Name + ": TOUCH_SLIDE " + coordinates);
        }

        /**
         * This is called when the touch area is not touched anymore
         */
        public override void TriggerInteractionEnds(Pose pose)
        {
            Vector2 coordinates = GetTouchscreenCoordinates(pose);

            base.RaiseInteractionElementEvent(EventSpec.TOUCH_END,
                                              new KeyValuePair<EventParameterSpec, float>(EventParameterSpec.TOUCH_X_COORDINATE, coordinates.x),
                                              new KeyValuePair<EventParameterSpec, float>(EventParameterSpec.TOUCH_Y_COORDINATE, coordinates.y));

            Debug.Log(base.Spec.Name + ": TOUCH_END " + coordinates);
        }


        /**
         * convenience method to get the location of the hit of the ray defined by the pose
         */
        private Vector2 GetTouchscreenCoordinates(Pose pose)
        {
            Vector3 planeHit = GetPlaneHit(pose);

            if (planeHit.x == Mathf.NegativeInfinity)
            {
                Debug.Log("not hitting object");
                return Vector2.negativeInfinity;
            }

            Vector3 touchscreenHit = planeHit - this.TopLeftPoint;

            float xAxisRatio = Vector3.Project(touchscreenHit, this.XAxis).magnitude / this.XAxis.magnitude;
            float yAxisRatio = Vector3.Project(touchscreenHit, this.YAxis).magnitude / this.YAxis.magnitude;

            if (Vector3.Dot(base.RepresentedObject.transform.TransformVector(base.Spec.Plane), pose.forward) > 0)
            {
                // we are pointing at the plane from behind --> inverse the axis ratios
                xAxisRatio = 1.0f - xAxisRatio;
                yAxisRatio = 1.0f - yAxisRatio;
            }

            return new Vector2(xAxisRatio * this.Spec.Resolution.x, yAxisRatio * this.Spec.Resolution.y);
        }

        /**
         * convenience method to get the location of the hit of the ray defined by the pose
         */
        private Vector3 GetPlaneHit(Pose pose)
        {
            Collider collider = this.GetComponent<Collider>();
            Ray destination = new Ray(pose.position, pose.forward);

            if (collider.Raycast(destination, out RaycastHit hitInfo, 100))
            {
                // the collider is hit by the ray, let us project this point onto the plane
                Vector3 planeHit = this.Plane.ClosestPointOnPlane(hitInfo.point);

                // if this plane hit is not too far from the collider hit (not more than 1mm in world space),
                // we consider the hit on the right side of the collider, i.e. in the plane
                if ((planeHit / 100) == (hitInfo.point / 100))
                {
                    //Debug.DrawLine(Vector3.zero, planeHit, Color.green);
                    //Debug.DrawLine(this.RepresentedObject.transform.position, hitInfo.point, Color.blue);

                    return planeHit;
                }
            }

            return Vector3.negativeInfinity;
        }

        /**
         * 
         */
        private void InitializePlane()
        {
            this.GetEffectiveTouchPlane(out Vector3 topLeftPoint, out Vector3 xAxis, out Vector3 yAxis);

            this.TopLeftPoint = this.RepresentedObject.transform.position + topLeftPoint;
            this.XAxis = xAxis;
            this.YAxis = yAxis;

            this.Plane = new Plane(Vector3.Cross(xAxis, yAxis),
                                   this.TopLeftPoint + this.XAxis / 2 + this.YAxis / 2);
        }

        /**
         * 
         */
        private void GetEffectiveTouchPlane(out Vector3 topLeftPoint, out Vector3 xAxis, out Vector3 yAxis)
        {
            // determine the object local coordinate system defined by the plane normal
            Vector3 meshLocalPlaneNormal = base.Spec.Plane;
            this.GetMeshLocalCoordinateSystem(base.RepresentedObject, meshLocalPlaneNormal,
                                              out Vector3 meshLocalXAxisDirection,
                                              out Vector3 meshLocalYAxisDirection);

            // get the bounds of the object considered we were looking at its touchable plane
            Vector3[] points = Utils.GetLocalPointsRepresentingMesh(base.RepresentedObject.GetComponent<MeshFilter>());
            //DrawMesh(points, new Color(0, 1, 1, 0.5f));

            Quaternion rotation = Quaternion.LookRotation(meshLocalPlaneNormal, -meshLocalYAxisDirection);
            Matrix4x4 matrix = Matrix4x4.Rotate(Quaternion.Inverse(rotation));

            Bounds boundsRelativeToLocalCoordinateSystem = GeometryUtility.CalculateBounds(points, matrix);

            //DrawBounds(boundsRelativeToLocalCoordinateSystem, new Color(0, 1, 1, 1f));

            // now use these bounds to determine the effective length of the x and y axis as well as the
            // plane offset (x and y axis must be inverted to point right and downward);
            Vector3 boundsLocalOrigin = boundsRelativeToLocalCoordinateSystem.center + boundsRelativeToLocalCoordinateSystem.extents;
            Vector3 boundsLocalXAxis = -Vector3.Project(boundsRelativeToLocalCoordinateSystem.size, Vector3.right);
            Vector3 boundsLocalYAxis = -Vector3.Project(boundsRelativeToLocalCoordinateSystem.size, Vector3.up);

            //Debug.DrawLine(boundsLocalOrigin, boundsLocalOrigin + Vector3.forward, Color.red);
            //Debug.DrawLine(boundsLocalOrigin, boundsLocalOrigin + boundsLocalXAxis, Color.green);
            //Debug.DrawLine(boundsLocalOrigin, boundsLocalOrigin + boundsLocalYAxis, Color.blue);

            // now we need to rotate everything back to the local coordinate system of the object
            Vector3 meshLocalOrigin = rotation * boundsLocalOrigin;
            Vector3 meshLocalXAxis = rotation * boundsLocalXAxis;
            Vector3 meshLocalYAxis = rotation * boundsLocalYAxis;

            //Debug.DrawLine(meshLocalOrigin, meshLocalOrigin + meshLocalPlaneNormal, Color.red);
            //Debug.DrawLine(meshLocalOrigin, meshLocalOrigin + meshLocalXAxis, Color.green);
            //Debug.DrawLine(meshLocalOrigin, meshLocalOrigin + meshLocalYAxis, Color.blue);

            // and finally we need to transform this to world space
            topLeftPoint = base.RepresentedObject.transform.TransformVector(meshLocalOrigin);
            xAxis = base.RepresentedObject.transform.TransformVector(meshLocalXAxis);
            yAxis = base.RepresentedObject.transform.TransformVector(meshLocalYAxis);

            //Debug.DrawLine(base.RepresentedObject.transform.position,
            //               base.RepresentedObject.transform.position + topLeftPoint, Color.red);
            //Debug.DrawLine(base.RepresentedObject.transform.position + topLeftPoint,
            //               base.RepresentedObject.transform.position + topLeftPoint + xAxis, Color.green);
            //Debug.DrawLine(base.RepresentedObject.transform.position + topLeftPoint,
            //               base.RepresentedObject.transform.position + topLeftPoint + yAxis, Color.blue);
        }

        /**
         * 
         */
        private void GetMeshLocalCoordinateSystem(GameObject representedObject,
                                                  Vector3 meshLocalPlaneNormal,
                                                  out Vector3 meshLocalXAxisDirection,
                                                  out Vector3 meshLocalYAxisDirection)
        {
            float angleToUpward = Vector3.Angle(meshLocalPlaneNormal, Vector3.up);

            if (angleToUpward == 0)
            {
                // the plane normal points upward, i.e. 
                meshLocalXAxisDirection = new Vector3(1, 0, 0);
                meshLocalYAxisDirection = new Vector3(0, 0, -1);
            }
            else if (angleToUpward == 180)
            {
                // the plane normal points downward, i.e. 
                meshLocalXAxisDirection = new Vector3(1, 0, 0);
                meshLocalYAxisDirection = new Vector3(0, 0, 1);
            }
            else
            {
                meshLocalXAxisDirection = Vector3.Cross(meshLocalPlaneNormal, Vector3.up);
                meshLocalYAxisDirection = Vector3.Cross(meshLocalPlaneNormal, meshLocalXAxisDirection);
            }

            //Debug.DrawRay(Vector3.zero, meshLocalPlaneNormal, new Color(1, 0, 0, 0.5f));
            //Debug.DrawRay(Vector3.zero, meshLocalXAxisDirection, new Color(0, 1, 0, 0.5f));
            //Debug.DrawRay(Vector3.zero, meshLocalYAxisDirection, new Color(0, 0, 1, 0.5f));
        }

        /**
         * 
         */
        private void DrawBounds(Bounds bounds, Color color)
        {
            Vector3[] points = new Vector3[]
            {
                bounds.center + bounds.extents,
                bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, -bounds.extents.z),
                bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, bounds.extents.z),
                bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, -bounds.extents.z),
                bounds.center - bounds.extents,
                bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, bounds.extents.z),
                bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, -bounds.extents.z),
                bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, bounds.extents.z)
            };

            DrawMesh(points, color);
        }

        /**
         * 
         */
        private void DrawMesh(Vector3[] points, Color color)
        {
            foreach (Vector3 point1 in points)
            {
                foreach (Vector3 point2 in points)
                {
                    if ((point1.x == point2.x) || (point1.y == point2.y) || (point1.z == point2.z))
                    {
                        Debug.DrawLine(point1, point2, color);
                    }
                }
            }
        }
    }
}
