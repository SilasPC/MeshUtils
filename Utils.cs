
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshCutter {

    static class Util {

        // Credits: https://www.geeksforgeeks.org/how-to-check-if-a-given-point-lies-inside-a-polygon/
        /*static class Vec2D {

            // Given three colinear points p, q, r,  
            // the function checks if point q lies 
            // on line segment 'pr'
            static bool OnSegment(Vector2 p, Vector2 q, Vector2 r)  
            { 
                if (q.x <= Math.Max(p.x, r.x) && 
                    q.x >= Math.Min(p.x, r.x) && 
                    q.y <= Math.Max(p.y, r.y) && 
                    q.y >= Math.Min(p.y, r.y)) 
                { 
                    return true; 
                } 
                return false; 
            } 
        
            // To find Orientation of ordered triplet (p, q, r). 
            // The function returns following values 
            // 0 --> p, q and r are colinear 
            // 1 --> Clockwise 
            // 2 --> Counterclockwise 
            static int Orientation(Vector2 p, Vector2 q, Vector2 r)  
            { 
                int val = (q.y - p.y) * (r.x - q.x) -  
                        (q.x - p.x) * (r.y - q.y); 
        
                if (val == 0)  
                { 
                    return 0; // colinear 
                } 
                return (val > 0) ? 1 : 2; // clock or counterclock wise 
            } 
        
            // The function that returns true if  
            // line segment 'p1q1' and 'p2q2' intersect. 
            public static bool DoIntersect(Vector2 p1, Vector2 q1,  
                                    Vector2 p2, Vector2 q2)  
            { 
                // Find the four orientations needed for  
                // general and special cases 
                int o1 = Orientation(p1, q1, p2); 
                int o2 = Orientation(p1, q1, q2); 
                int o3 = Orientation(p2, q2, p1); 
                int o4 = Orientation(p2, q2, q1); 
        
                // General case 
                if (o1 != o2 && o3 != o4) 
                { 
                    return true; 
                } 
        
                // Special Cases 
                // p1, q1 and p2 are colinear and 
                // p2 lies on segment p1q1 
                if (o1 == 0 && OnSegment(p1, p2, q1))  
                { 
                    return true; 
                } 
        
                // p1, q1 and p2 are colinear and 
                // q2 lies on segment p1q1 
                if (o2 == 0 && OnSegment(p1, q2, q1))  
                { 
                    return true; 
                } 
        
                // p2, q2 and p1 are colinear and 
                // p1 lies on segment p2q2 
                if (o3 == 0 && OnSegment(p2, p1, q2)) 
                { 
                    return true; 
                } 
        
                // p2, q2 and q1 are colinear and 
                // q1 lies on segment p2q2 
                if (o4 == 0 && OnSegment(p2, q1, q2)) 
                { 
                    return true; 
                } 
        
                // Doesn't fall in any of the above cases 
                return false;  
            } 
        }*/

        // ------------------------------------------------------
        // Create a unit vector perpendicular to a given vector
        // Exact direction is obviously unknown
        // ------------------------------------------------------
        public static Vector3 UnitPerpendicular(Vector3 v) {
            if (v.x != 0) return new Vector3(1,1,-(v.y+v.z)/v.x).normalized;
            if (v.y != 0) return new Vector3(1,1,-(v.x+v.z)/v.y).normalized;
            if (v.z != 0) return new Vector3(1,1,-(v.x+v.y)/v.z).normalized;
            // fail silently
            return new Vector3(0,0,0);
        }

        // ------------------------------------
        // Closest indices between two rings
        // ------------------------------------
        public static float RingDist(List<Vector3> r0, List<Vector3> r1, ref int i0, ref int i1) {
            float mag = float.PositiveInfinity;
            for (int i = 0; i < r0.Count; i++) {
                for (int j = 0; j < r1.Count; j++) {
                    float nMag = (r0[i]-r1[j]).magnitude;
                    if (nMag < mag) {
                        i0 = i;
                        i1 = j;
                        mag = nMag;
                    }
                }
            }
            return mag;
        }

        // --------------------------------------------------------
        // Debugging util to make a GeoGebra compatible point list
        // --------------------------------------------------------
        public static void DebugRing(List<Vector3> ring) {
            List<String> strings = new List<String>();
            foreach (var v in ring) strings.Add("("+v.x.ToString().Replace(',','.')+","+v.y.ToString().Replace(',','.')+","+v.z.ToString().Replace(',','.')+")");
            Debug.Log("{"+String.Join(",",strings)+"}");
        }

        // -----------------------------------------------------------
        // Debugging util to make a GeoGebra compatible 2d point list
        // -----------------------------------------------------------
        public static void DebugRing2D(List<Vector2> ring) {
            List<String> strings = new List<String>();
            foreach (var v in ring) strings.Add("("+v.x.ToString().Replace(',','.')+","+v.y.ToString().Replace(',','.')+")");
            Debug.Log("{"+String.Join(",",strings)+"}");
        }

        // --------------------------------------------------------
        // Debugging util to make a GeoGebra compatible ring list
        // --------------------------------------------------------
        public static void DebugRings(List<List<Vector3>> rings) {
            List<String> topStrings = new List<String>();
            foreach (var ring in rings) {
                List<String> strings = new List<String>();
                foreach (var v in ring) strings.Add("("+v.x.ToString().Replace(',','.')+","+v.y.ToString().Replace(',','.')+","+v.z.ToString().Replace(',','.')+")");
                topStrings.Add("{"+String.Join(",",strings)+"}");
            }
            Debug.Log("{"+String.Join(",",topStrings)+"}");
        }

        // --------------------------------------------------
        // Remove excess points that lie in a straight line
        // Note: broken (probably rounding errors)
        // --------------------------------------------------
        /*public static void SimplifyRing(List<Vector3> ring) {
            int i = 0;
            while (i < ring.Count && ring.Count > 3) {
                if (
                    Vector3.Cross(
                        ring[i]-ring[(i+2)%ring.Count],
                        ring[i]-ring[(i+1)%ring.Count]
                    ) == Vector3.zero
                ) ring.RemoveAt((i+1)%ring.Count);
                else i++;
            }
        }*/

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
        private static bool CheckIsInside(Vector3 v0, Vector3 v1, Vector3 v) {
            float x = v0.sqrMagnitude, y = Vector3.Dot(v0,v1), z = v1.sqrMagnitude;
            float invDet = 1 / (x*z-y*y);
            float m00 = invDet * (z*v0.x-y*v1.x), m01 = invDet * (z*v0.y-y*v1.y), m02 = invDet * (z*v0.z-y*v1.z),
                  m10 = invDet * (-y*v0.x+x*v1.x), m11 = invDet * (-y*v0.y+x*v1.y), m12 = invDet * (-y*v0.z+x*v1.z);
            float s = v.x * m00 + v.y * m01 + v.z * m02;
            float t = v.x * m10 + v.y * m11 + v.z * m12;
            return s >= 0 && s <= 1 && t >= 0 && t <= 1 && s + t >= 0 && s + t <= 1;
        }

        // ------------------------------------
        // Check if float is in a given range
        // ------------------------------------
        public static bool inRange(float min, float max, float val) {
            return val >= min && val <= max;
        }

        // -----------------------------------------------------------
        // Generate triangles for an intersecting triangle a,b,c
        // It is assumed that a and b are on the positive half plane,
        // and c is on the negative half plane.
        // -----------------------------------------------------------
        public static void GenTriangles(
            CuttingPlane plane,
            API.MeshPart pos, API.MeshPart neg,
            Vector3 a, Vector3 b,Vector3 c,
            int i_a, int i_b, int i_c,
            RingGenerator rings
        ) {

            // find intersection vertices
            Vector3 e = plane.Intersection(a, c);
            Vector3 d = plane.Intersection(b, c);

            // if e == d, the three vertices lie in a line,
            //   and thus do not make up a triangle
            if (e == d) return;

            // new indices
            int pi0 = pos.vertices.Count, ni0 = neg.vertices.Count;

            // add connected pair in ring generator

            // find proper direction for ring
            Vector3 tri_nor = Vector3.Cross(c-a,c-b);
            bool dir = Vector3.Dot(e-d,Vector3.Cross(plane.normal,tri_nor)) > 0;

            rings.AddConnected(dir?e:d,dir?d:e);

            // add new vertices
            pos.vertices.Add(d);
            pos.vertices.Add(e);
            neg.vertices.Add(d);
            neg.vertices.Add(e);

            // generate triangles for sides ...

            // add a,d,e to positive indicies
            pos.indices.Add(pos.indexMap[i_a]);
            pos.indices.Add(pi0);
            pos.indices.Add(pi0+1);
            // add a,b,d to positive indicies
            pos.indices.Add(pos.indexMap[i_a]);
            pos.indices.Add(pos.indexMap[i_b]);
            pos.indices.Add(pi0);
            // add e,d,c to negative indicies
            neg.indices.Add(ni0+1);
            neg.indices.Add(ni0);
            neg.indices.Add(neg.indexMap[i_c]);

        }

        /*public static void AddTriangleIndices(List<int> triangles, Dictionary<int,int> map, int i_a, int i_b, int i_c) {
            triangles.Add(map[i_a]);
            triangles.Add(map[i_b]);
            triangles.Add(map[i_c]);
        }*/

        // -----------------------------------------------------
        // Generate triangle mesh within possibly concave ring
        // -----------------------------------------------------
        public static void GenerateRingMesh(
            List<Vector3> ring, API.MeshPart part, Vector3 normal
        ) {

            // List<List<Vector3>> reduceHist = new List<List<Vector3>>();
            // reduceHist.Add(ring);

            int indStart = part.vertices.Count;
            part.vertices.AddRange(ring);

            List<Tuple<Vector3,int>> set = ring.ConvertAll(v=>new Tuple<Vector3,int>(v,indStart++));

            int i = -1 , lsc = set.Count;
            bool didInf = false;
            while (set.Count > 1) {
                i++;
                if (i >= set.Count) {
                    if (lsc == set.Count) {
                        if (didInf) {
                            // Debug.LogError("ear clipping failed");
                            // DebugRings(reduceHist);
                            // Debug.Log("normal "+(normal*1000));
                            // DebugRing(set.ConvertAll(s=>s.Item1));
                            return;
                        }
                        didInf = true;
                    } else didInf = false;
                    lsc = set.Count;
                }
                i %= set.Count;

                Tuple<Vector3,int> vi0 = set[i],
                                   vi1 = set[(i+1)%set.Count],
                                   vi2 = set[(i+2)%set.Count];
                Vector3 d1 = vi1.Item1 - vi0.Item1; // i0 -> i1
                Vector3 d2 = vi2.Item1 - vi0.Item1; // i0 -> i2

                // we assume points do not lie in a line (see SimplifyRing)
                // if they do however, it's just a waste of vertices

                // check convex ear
                float conv = Vector3.Dot(Vector3.Cross(d1,d2),normal);
                if (conv > 0) continue;

                // check that there are no other vertices inside this triangle
                if (set.Exists(v => {
                    // note: we must compare the vectors, otherwise coinciding vectors do not
                    // register as being the same
                    if (v.Item1 == vi0.Item1 || v.Item1 == vi1.Item1 || v.Item1 == vi2.Item1) return false;
                    return CheckIsInside(d1,d2,v.Item1-vi0.Item1);
                })) continue;

                // generate indices
                if (part.side) {
                    part.indices.Add(vi0.Item2);
                    part.indices.Add(vi1.Item2);
                    part.indices.Add(vi2.Item2);
                } else {
                    part.indices.Add(vi0.Item2);
                    part.indices.Add(vi2.Item2);
                    part.indices.Add(vi1.Item2);
                }

                // eliminate vertex
                set.RemoveAt((++i)%set.Count);
                i++;

                // reduceHist.Add(set.ConvertAll(s=>s.Item1));

            }

        }

    }

}