
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshUtils {

    static class VectorUtil {

        // ------------------------------------------------------
        // Create a unit vector perpendicular to a given vector
        // Exact direction is obviously unknown
        // ------------------------------------------------------
        public static Vector3 UnitPerpendicular(Vector3 v) {
            if (v.x != 0) return new Vector3(1,1,-(v.y+v.z)/v.x).normalized;
            if (v.y != 0) return new Vector3(1,1,-(v.x+v.z)/v.y).normalized;
            if (v.z != 0) return new Vector3(1,1,-(v.x+v.y)/v.z).normalized;
            throw MeshUtilsException.ZeroNormal();
        }
        
        // ------------------------------------------------------------------------------------
        // Use pseudo inverse matrix to decompose vector to barycentric coordinates
        // If 's' and 't' fall in the range [0,1] and s+t <= 1, then v is in the triangle
        //
        // We want to solve p = p0 + s * (p1 - p0) + t * (p2 - p0)
        // Taking p0 as the origin, this gives
        //   v = s * v0 + t * v1
        //
        // This is equivalent to v = M * P, where P is the vector {s,t}
        // We want to find the pseudo inverse of M, such that
        //   M^-1 * v = P
        //
        // The left pseudo inverse of M is given as (M^T * M)^-1 * M^T
        // (M^T * M) is simply found using dot products of v0 and v1,
        //   and it's inverse is pretty easy (2x2 matrix)
        // 
        // Multiplied by M^T and then v gives P, which encode the barycentric coordinates
        // ------------------------------------------------------------------------------------
        public static bool CheckIsInside(Vector3 v0, Vector3 v1, Vector3 v) {
            float x = v0.sqrMagnitude, y = Vector3.Dot(v0,v1), z = v1.sqrMagnitude;
            float invDet = 1 / (x*z-y*y);
            float m00 = invDet * (z*v0.x-y*v1.x), m01 = invDet * (z*v0.y-y*v1.y), m02 = invDet * (z*v0.z-y*v1.z),
                  m10 = invDet * (-y*v0.x+x*v1.x), m11 = invDet * (-y*v0.y+x*v1.y), m12 = invDet * (-y*v0.z+x*v1.z);
            float s = v.x * m00 + v.y * m01 + v.z * m02;
            float t = v.x * m10 + v.y * m11 + v.z * m12;
            return s >= 0 && s <= 1 && t >= 0 && t <= 1 && s + t >= 0 && s + t <= 1;
        }

        // ------------------
        // Transform normals
        // ------------------
        public static Vector3 TransformNormal(Vector3 v, Transform t) {
            return t.worldToLocalMatrix.transpose * v;
        }
        public static Vector3 InverseTransformNormal(Vector3 v, Transform t) {
            return t.localToWorldMatrix.transpose * v;
        }

        // -------------------------------------------------------------
        // Calculate the shortest distance between a point and a line
        //   given by two points
        // Formula for point/line distance: d=|(p-x1)x(p-x2)|/|x2-x1|
        // -------------------------------------------------------------
        public static float DistanceToLine(Vector3 p, Vector3 e0, Vector3 e1) {
            return Vector3.Cross(p-e0,p-e1).magnitude/(e1-e0).magnitude;
        }

        // -------------------------------------------------------------
        // Calculate the shortest distance between a point and an edge
        //   given by two points
        // Formula for point/line distance: d=|(p-x1)x(p-x2)|/|x2-x1|
        // -------------------------------------------------------------
        public static float DistanceToEdge(Vector3 p, Vector3 e0, Vector3 e1) {
            return VectorToEdge(p,e0,e1).magnitude;
        }

        // ----------------------------------------------
        // Calculate shortest vector from point to edge
        // ----------------------------------------------
        public static Vector3 VectorToEdge(Vector3 p, Vector3 e0, Vector3 e1) {
            bool ae0 = Vector3.Dot((e1-e0).normalized, p - e0) > 0,
                ae1 = Vector3.Dot((e0-e1).normalized, p - e1) > 0;
            if (ae0 && ae1) return e0 + Vector3.Project(p-e0,e1-e0) - p;
            if (ae0) return e1 - p;
            return e0 - p;   
        }

        // --------------------------------------------
        // Calculate closest point on edge to a point
        // --------------------------------------------
        public static Vector3 ClosestPointOnEdge(Vector3 p, Vector3 e0, Vector3 e1) {
            return VectorToEdge(p,e0,e1) + p;
        }

    }

}