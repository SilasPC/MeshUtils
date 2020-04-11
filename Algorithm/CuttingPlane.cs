
using System;
using UnityEngine;

namespace MeshUtils {

    public class MUPlane {

        protected readonly float d; 
        public readonly Vector3 pointInPlane, normal;

        public MUPlane(Vector3 normal, Vector3 pointInPlane) {
                if (normal == Vector3.zero) throw MeshUtilsException.Internal("Zero normal");
                this.pointInPlane = pointInPlane;
                this.normal = normal.normalized;
                this.d = -Vector3.Dot(pointInPlane,this.normal);
        }

        override public string ToString() {
            bool sy = normal.y >= 0, sz = normal.z >= 0, sd = d >= 0;
            return ("Plane: "+normal.x+"x"+(sy?"+":"")+normal.y+"y"+(sz?"+":"")+normal.z+"z"+(sd?"+":"")+d+"=0").Replace(",",".");
        }

        // -------------------------------
        // Check if point is above plane
        // -------------------------------
        public bool IsAbove(Vector3 point) {
            return Vector3.Dot(normal, (point - this.pointInPlane)) > 0;
        }

        public float SignedDistance(Vector3 point) {
            return Vector3.Dot(normal,point) + d;
        }

        // ----------------------------------------
        // Shortest distance from point to plane
        // ----------------------------------------
        public float Distance(Vector3 point) {
            return Math.Abs(
                (Vector3.Dot(normal,point) + d) / normal.magnitude
            );
        }

        // ---------------------------
        // Intersection point of edge
        // ---------------------------
        public Vector3 Intersection(Vector3 p0, Vector3 p1) {

            // to avoid rounding errors, always make sure float calculations are done in this order
            if (IsAbove(p0)) {
                Vector3 tmp = p0;
                p0 = p1;
                p1 = tmp;
            } else if (!IsAbove(p1)) Debug.LogException(MeshUtilsException.Internal("both below"));

            if (IsAbove(p0)) Debug.LogException(MeshUtilsException.Internal("both above"));

            float dist0 = Distance(p0);
            float dist1 = Distance(p1);

            float factor = dist0 / (dist0 + dist1);

            return p0 + (p1 - p0) * factor;
            
        }

        public Vector3 Project(Vector3 v) {
            float t = - (Vector3.Dot(v,normal) + d) / (normal.sqrMagnitude);
            return v + t * normal;
        }

        public Vector3 DirectionalProject(Vector3 v, Vector3 dir) {
            float t = -(Vector3.Dot(v,normal)+d)/Vector3.Dot(dir,normal);
            if (float.IsInfinity(t) || float.IsNaN(t))
                throw MeshUtilsException.Internal("Directional projection failed: t="+t+" v="+v+" n="+normal+" d="+d+" dir="+dir);
            return v + dir * t;
        }
        
    }

    // ---------------------------------------------
    // Used to define a plane to seperate meshes
    // ---------------------------------------------
    public class CuttingPlane : MUPlane {

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

        private CuttingPlane(Vector3 normal, Vector3 pointInPlane, CuttingPlane worldSpace) : base(normal,pointInPlane) {
            this.worldSpace = worldSpace;
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

        // --------------------------------------------------------
        // Intersection point of edge between p0 and p1 with plane
        // --------------------------------------------------------
        public Tuple<Vector3,Vector2> Intersection(Vector3 p0, Vector3 p1, Vector2 uv0, Vector2 uv1, float shift) {

            // to avoid rounding errors, always make sure float calculations are done in this order
            if (IsAbove(p0)) {
                Vector3 tmp = p0;
                p0 = p1;
                p1 = tmp;
                Vector2 uvtmp = uv0;
                uv0 = uv1;
                uv1 = uvtmp;
                shift *= -1; // is this rounding-safe ?
            }

            float dist0 = Distance(p0);
            float dist1 = Distance(p1);

            float factor = (dist0 + shift) / (dist0 + dist1);

            Vector3 res = p0 + (p1 - p0) * factor;

            //Debug.Log(Debugging.VecStr(p0));
            //Debug.Log(Debugging.VecStr(p1));
            //Debug.Log(Debugging.VecStr(res));

            return new Tuple<Vector3,Vector2>(
                res,
                uv0 + (uv1 - uv0) * factor
            );

        }

    }

}