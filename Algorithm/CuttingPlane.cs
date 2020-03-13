
using System;
using UnityEngine;

namespace MeshUtils {

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
                this.worldSpace = worldSpace;
                this.pointInPlane = pointInPlane;
                this.normal = normal;
                this.d = -Vector3.Dot(pointInPlane,normal);
            }

            public CuttingPlane ToWorldSpace() {
                if (worldSpace == null) return this;
                return worldSpace;
            }

            public CuttingPlane ToLocalSpace(Transform transform) {
                CuttingPlane worldSpace = ToWorldSpace();
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
            public Tuple<Vector3,Vector2> Intersection(Vector3 p0, Vector3 p1, Vector2 uv0, Vector2 uv1, float shift) {

                float dist0 = Distance(p0);
                float dist1 = Distance(p1);

                float factor = (dist0 + shift) / (dist0 + dist1);

                return new Tuple<Vector3,Vector2>(
                    p0 + (p1 - p0) * factor,
                    uv0 + (uv1 - uv0) * factor
                );

            }

            // ----------------------------------------
            // Intersection point of edge without UVs
            // ----------------------------------------
            public Vector3 Intersection(Vector3 p0, Vector3 p1, float shift) {

                float dist0 = Distance(p0);
                float dist1 = Distance(p1);

                float factor = (dist0 + shift) / (dist0 + dist1);

                return p0 + (p1 - p0) * factor;
                
            }

        }

}