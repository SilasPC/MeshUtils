
using System;
using UnityEngine;

namespace MeshCutter {

    // ---------------------------------------------
    // Used to define a plane to seperate meshes
    // ---------------------------------------------
    public class CuttingPlane {

            public static CuttingPlane InLocalSpace(Vector3 normal, Vector3 pointInPlane, Transform transform) {
                CuttingPlane worldSpace = new CuttingPlane(
                    transform.worldToLocalMatrix.transpose * normal,
                    transform.TransformPoint(pointInPlane),
                    null
                );
                return new CuttingPlane(normal, pointInPlane, worldSpace);
            }

            public static CuttingPlane InWorldSpace(Vector3 normal, Vector3 pointInPlane) {
                return new CuttingPlane(normal, pointInPlane, null);
            }

            private readonly CuttingPlane worldSpace;
            private readonly float d; 
            public readonly Vector3 pointInPlane, normal;

            private readonly Matrix4x4 projectionMatrix;

            private CuttingPlane(Vector3 normal, Vector3 pointInPlane, CuttingPlane worldSpace) {
                if (worldSpace != null) this.worldSpace = worldSpace;
                else this.worldSpace = this;
                this.pointInPlane = pointInPlane;
                this.normal = normal;
                this.d = -Vector3.Dot(pointInPlane,normal);
                /*Matrix4x4 m = new Matrix4x4();
                Vector3 d1 = Util.UnitPerpendicular(normal);
                Vector3 d2 = Vector3.Cross(normal,d1).normalized;
                m.SetColumn(0,d1);
                m.SetColumn(1,d2);
                m.SetColumn(2,pointInPlane);
                m[3,3] = 1;
                this.projectionMatrix = m.inverse; */
            }

            /*public Vector2 Project(Vector3 v) {
                Vector3 res = this.projectionMatrix * v;
                return new Vector3(res.x,res.y);
            }*/

            public CuttingPlane ToWorldSpace() {return this.worldSpace;}

            public CuttingPlane ToLocalSpace(Transform transform) {
                return new CuttingPlane(
                    transform.localToWorldMatrix.transpose * worldSpace.normal,
                    transform.InverseTransformPoint(worldSpace.pointInPlane),
                    worldSpace
                );
            }

            // -------------------------------
            // Check if point is above plane
            // -------------------------------
            public bool IsAbove(Vector3 point) {
                return Vector3.Dot(normal, (point - this.pointInPlane)) > 0;
            }

            // ----------------------------------------
            // Shortest distance from point to plane
            // ----------------------------------------
            public float Distance(Vector3 point) {
                return Math.Abs(
                    (Vector3.Dot(normal,point) + d) / normal.magnitude
                );
            }

            // --------------------------------------------------------
            // Intersection point of edge between p0 and p1 with plane
            // --------------------------------------------------------
            public Vector3 Intersection(Vector3 p0, Vector3 p1) {

                float dist0 = Distance(p0);
                float dist1 = Distance(p1);

                return p0 + (p1 - p0) * dist0 / (dist0 + dist1);

            }

        }

}