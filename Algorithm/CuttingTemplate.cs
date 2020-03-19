
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshUtils {

    class CuttingTemplate {

        public static CuttingTemplate InLocalSpace(Vector3 normal, Vector3 pointInPlane, Transform transform) {
            Vector3 world_normal = transform.TransformDirection(normal),
                    world_point = transform.TransformPoint(pointInPlane);
            return new CuttingTemplate(normal, pointInPlane,world_normal,world_point);
        }

        public static CuttingTemplate InWorldSpace(Vector3 normal, Vector3 pointInPlane) {
            return new CuttingTemplate(normal, pointInPlane, normal, pointInPlane);
        }

        public CuttingTemplate ToLocalSpace(Transform transform) {
            var t = new CuttingTemplate(
                transform.InverseTransformDirection(worldNormal),
                transform.InverseTransformPoint(world_pointInPlane),
                worldNormal,
                world_pointInPlane
            );
            foreach (Vector3 p in points)
                t.AddPoint(transform.InverseTransformPoint(p)); // tmp. p is assumed to be in world space
            return t;
        }

        public readonly List<Vector3> points = new List<Vector3>();
        private readonly MUPlane plane;
        public readonly Vector3 pointInPlane, normal;
        private readonly Vector3 world_pointInPlane, worldNormal;

        private CuttingTemplate(
            Vector3 normal, Vector3 pointInPlane,
            Vector3 worldNormal, Vector3 world_pointInPlane
        ) {
            this.worldNormal = worldNormal;
            this.world_pointInPlane = world_pointInPlane;
            this.pointInPlane = pointInPlane;
            this.normal = normal.normalized;
            this.plane = new MUPlane(this.normal,pointInPlane);
        }

        // ----------------------------------------------------------
        // Checks if a point is on the positive side of the template
        // ----------------------------------------------------------
        public bool IsAbove(Vector3 v) {
            Vector3 pv = plane.Project(v);
            float dMin = float.PositiveInfinity;
            int i0 = 0;
            for (int i = 0; i < points.Count - 1; i++) {
                float d = VectorUtil.DistanceToEdge(pv,points[i],points[i+1]);
                if (d < dMin) {
                    i0 = i;
                    dMin = d;
                }
            }
            return Vector3.Dot(
                Vector3.Cross(
                    points[i0]-pv,
                    points[i0+1]-points[i0]
                ),
                normal
            ) > 0;
        }

        public void AddPoint(Vector3 p) {
            p = plane.Project(p);
            if (points.Count > 1) {
                Vector3 dif = points[points.Count-1] - points[points.Count-2];
                if (Vector3.Angle(dif,p-points[points.Count-1]) < 0) return;
            }
            points.Add(p);
        }

        public void Draw() {
            foreach(Vector3 p in points)
                Debug.DrawRay(p-normal,normal * 2,Color.red,1,true);
            for (int i = 0; i < points.Count - 1; i++) {
                Debug.DrawRay(points[i]+normal,points[i+1]-points[i],Color.red,1,true);
                Debug.DrawRay(points[i],points[i+1]-points[i],Color.red,1,true);
                Debug.DrawRay(points[i]-normal,points[i+1]-points[i],Color.red,1,true);
            }
        }

        public void ProcessTriangle(
            Vector3 a, Vector3 b, Vector3 c,
            RingGenerator ring
        ) {
            Vector3 tri_nor = -Vector3.Cross(c-a,c-b);
            MUPlane tri_plane = new MUPlane(tri_nor,a); // rounding errors are inbound here
            MUPlane template_plane = new MUPlane(normal,pointInPlane);
            Vector3 pa = template_plane.Project(a),
                    pb = template_plane.Project(b),
                    pc = template_plane.Project(c);
            Vector3 ab_nor = Vector3.Cross(tri_nor,pb-pa),
                    bc_nor = Vector3.Cross(tri_nor,pc-pb),
                    ca_nor = Vector3.Cross(tri_nor,pa-pc);
            MUPlane ab = new MUPlane(ab_nor,pa),
                    bc = new MUPlane(bc_nor,pb),
                    ca = new MUPlane(ca_nor,pc);
            bool oaab = ab.IsAbove(points[0]),
                oabc = bc.IsAbove(points[0]),
                oaca = ca.IsAbove(points[0]);
            bool oldInside = !(oaab||oabc||oaca);
            for (int i = 1; i < points.Count; i++) {
                bool aab = ab.IsAbove(points[i]),
                    abc = bc.IsAbove(points[i]),
                    aca = ca.IsAbove(points[i]);
                bool inside = !(aab||abc||aca);
                // Debug.Log(aab+" "+abc+" "+aca);
                if (inside != oldInside) {
                    // add edge pair
                    MUPlane plane;
                    if (inside) {
                        if (oaab) plane = ab;
                        else if (oabc) plane = bc;
                        else if (oaca) plane = ca;
                        else throw OperationException.Internal("Point on neither side of triangle");
                        Vector3 iv = plane.Intersection(points[i-1],points[i]);
                        ring.AddConnected(iv,points[i]);
                    } else {
                        //Debug.Log(a+" "+b+" "+c+" "+template_plane);
                        //Debug.Log(pa+" "+pb+" "+pc+" "+points[i-1]+" "+points[i]);
                        if (aab) plane = ab;
                        else if (abc) plane = bc;
                        else if (aca) plane = ca;
                        else throw OperationException.Internal("Point on neither side of triangle");
                        Vector3 iv = plane.Intersection(points[i-1],points[i]);
                        ring.AddConnected(points[i-1],iv);
                    }
                } else if (oldInside) {
                    // add inner pair
                    ring.AddConnected(points[i-1],points[i]);
                } else {
                    // add outer pair
                    if ( // test to check if edge does not cross triangle
                        (aab && oaab) ||
                        (abc && oabc) ||
                        (aca && oaca)
                    ) continue;
                    // crosses triangle...
                }
                oldInside = inside;
                oaab = aab;
                oabc = abc;
                oaca = aca;
            }
            //Debug.Log(a+" "+b+" "+c);
            //ring.MyDebugLog();
        }

    }

}