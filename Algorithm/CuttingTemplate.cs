
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshUtils {

    public class CuttingTemplate {

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
        private readonly float d; 
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
            this.d = -Vector3.Dot(pointInPlane,normal);
        }

        // ----------------------------------------------------------
        // Checks if a point is on the positive side of the template
        // ----------------------------------------------------------
        public bool IsAbove(Vector3 v) {
            float dMin = float.PositiveInfinity;
            int i0 = 0;
            for (int i = 0; i < points.Count - 1; i++) {
                float d = VectorUtil.DistanceToEdge(v,points[i],points[i+1]);
                if (d < dMin) {
                    i0 = i;
                    dMin = d;
                }
            }
            return Vector3.Dot(
                Vector3.Cross(
                    points[i0]-v,
                    points[i0+1]-points[i0]
                ),
                normal
            ) > 0;
        }

        public void AddPoint(Vector3 p) {
            p = VectorUtil.ProjectIntoPlane(p,normal,d);
            if (points.Count > 1) {
                Vector3 dif = points[points.Count-1] - points[points.Count-2];
                if (Vector3.Angle(dif,p-points[points.Count-1]) < 20) return;
            }
            points.Add(p);
        }

        public void Draw() {
            foreach(Vector3 p in points)
                Debug.DrawRay(p-normal,normal * 2,Color.red,60,true);
            for (int i = 0; i < points.Count - 1; i++) {
                Debug.DrawRay(points[i]+normal,points[i+1]-points[i],Color.red,60,true);
                Debug.DrawRay(points[i],points[i+1]-points[i],Color.red,60,true);
                Debug.DrawRay(points[i]-normal,points[i+1]-points[i],Color.red,60,true);
            }
        }

    }

}