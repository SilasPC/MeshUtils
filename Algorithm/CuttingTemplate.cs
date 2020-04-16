
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshUtils {

    class CuttingTemplate {

        public bool isClosed { get; protected set; } = false;

        public static CuttingTemplate InLocalSpace(Vector3 normal, Vector3 pointInPlane, Transform transform) {
            Vector3 world_normal = transform.TransformDirection(normal),
                    world_point = transform.TransformPoint(pointInPlane);
            return new CuttingTemplate(normal, pointInPlane,world_normal,world_point);
        }

        public static CuttingTemplate InWorldSpace(Vector3 normal, Vector3 pointInPlane) {
            return new CuttingTemplate(normal, pointInPlane, normal, pointInPlane);
        }

        public CuttingTemplate ToWorldSpace() {
            return new CuttingTemplate(
                worldNormal, world_pointInPlane,
                worldNormal, world_pointInPlane
            );
            // points tmp
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

        public CuttingTemplate SetClosed() {
            this.isClosed = true;
            return this;
        }

        public readonly List<Vector3> points = new List<Vector3>();
        public readonly MUPlane plane;
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

        override public string ToString() {
            return this.plane.ToString();
        }

        // ----------------------------------------------------------
        // Checks if a point is on the positive side of the template
        // ----------------------------------------------------------
        public bool IsAbove(Vector3 v) {
            Vector3 pv = plane.Project(v);
            float dMin;
            int i0;
            Vector3 dif;
            float lastLineDist;
            if (isClosed) {
                dif = VectorUtil.VectorToEdge(pv,points.Last(),points.First());
                lastLineDist = VectorUtil.DistanceToLine(pv,points.Last(),points.First());
                i0 = points.Count - 1;
                dMin = dif.magnitude;
            } else {
                dif = VectorUtil.VectorToEdge(pv,points[0],points[1]);
                lastLineDist = VectorUtil.DistanceToLine(pv,points[0],points[1]);
                i0 = 0;
                dMin = dif.magnitude;
            }
            for (int i = 0; i < points.Count - 1; i++) {
                float lineDist = VectorUtil.DistanceToLine(pv,points[i],points[i+1]);
                Vector3 vec = VectorUtil.VectorToEdge(pv,points[i],points[i+1]);
                if (vec.magnitude == dMin) {
                    if (lineDist > lastLineDist) {
                        i0 = i;
                        dMin = vec.magnitude;
                        dif = vec;
                    }
                } else if (vec.magnitude < dMin) {
                    i0 = i;
                    dMin = vec.magnitude;
                    dif = vec;
                }
                lastLineDist = lineDist;
            }
            int i1 = i0 + 1 == points.Count ? 0 : i0 + 1;
            return Vector3.Dot(
                Vector3.Cross(
                    dif,
                    points[i1]-points[i0]
                ),
                normal
            ) > 0;
        }

        public bool AddPoint(Vector3 p, float maxAngle = 0, float minDist = 0.03f) {
            p = plane.Project(p);
            if (maxAngle > 0 && points.Count > 1) {
                Vector3 dif = points[points.Count-1] - points[points.Count-2];
                if (Vector3.Angle(dif,p-points[points.Count-1]) < maxAngle) return false;
            }
            if (points.Count > 0) {
                Vector3 last = points[points.Count-1];
                if ((last-p).magnitude < minDist) return false;
            }
            points.Add(p);
            return true;
        }

        public void Draw() {
            for (int i = 0; i < points.Count - 1; i++) {
                Debug.DrawRay(points[i]-normal,normal * 2,Color.red,10,true);
                Debug.DrawRay(points[i]+normal,points[i+1]-points[i],Color.red,10,true);
                Debug.DrawRay(points[i],points[i+1]-points[i],Color.red,10,true);
                Debug.DrawRay(points[i]-normal,points[i+1]-points[i],Color.red,10,true);
            }
            if (isClosed) {
                Debug.DrawRay(points.Last()-normal,normal * 2,Color.blue,10,true);
                Debug.DrawRay(points.Last()+normal,points[0]-points.Last(),Color.blue,10,true);
                Debug.DrawRay(points.Last(),points[0]-points.Last(),Color.blue,10,true);
                Debug.DrawRay(points.Last()-normal,points[0]-points.Last(),Color.blue,10,true);
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