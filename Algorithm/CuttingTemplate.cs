
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
            if (points.Count > 0) {
                Vector3 last = points[points.Count-1];
                if ((last-p).magnitude < 0.03f) return;
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

        public void PrepareTmp() {
            Vector3 dif = points[0] - points[1];
            points.Insert(0,points[0]+5*dif);
            dif = points[points.Count-1] - points[points.Count-2];
            points.Add(points[points.Count-1]+5*dif);
        }

    }

}