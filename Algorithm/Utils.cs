
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshUtils {

    class OperationException : Exception {
        private OperationException(string msg) : base(msg) {}
        public static OperationException ZeroNormal() {
            return new OperationException("The supplied normal vector was of zero length");
        }
        public static OperationException MalformedMesh() {
            return new OperationException("The supplied object contains a malformed mesh");
        }
        public static OperationException Internal(string msg) {
            return new OperationException("Internal error: " + msg);
        }
    }

    static class Util {

        // ------------------------------------------------------
        // Create a unit vector perpendicular to a given vector
        // Exact direction is obviously unknown
        // ------------------------------------------------------
        public static Vector3 UnitPerpendicular(Vector3 v) {
            if (v.x != 0) return new Vector3(1,1,-(v.y+v.z)/v.x).normalized;
            if (v.y != 0) return new Vector3(1,1,-(v.x+v.z)/v.y).normalized;
            if (v.z != 0) return new Vector3(1,1,-(v.x+v.y)/v.z).normalized;
            throw OperationException.ZeroNormal();
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

        // ------------------------------
        // Debugging util to log a list
        // ------------------------------
        public static string LogList(List<int> list) {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (int i in list) sb.Append(i.ToString()+" ");
            return sb.ToString();
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
        private static bool CheckIsInside(Vector3 v0, Vector3 v1, Vector3 v) {
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
            MeshPart pos, MeshPart neg,
            Vector3 a, Vector3 b,Vector3 c,
            int i_a, int i_b, int i_c,
            RingGenerator rings
        ) {

            // find intersection vertices
            Vector3 e = plane.Intersection(a, c);
            Vector3 d = plane.Intersection(b, c);

            // if e == d, the three vertices lie in a line,
            //   and thus do not make up a triangle
            if (e == d) {
                return;
                // not sure if this is nescessary
                throw OperationException.MalformedMesh();
            }

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

        // ---------------------------------------------------------------
        // Determine shortest distance to ring perimeter
        // Formula for point/line distance: d=|(p-x1)x(p-x2)|/|x2-x1|
        // i0 and i1 are indices for vertexes around closest line in ring
        // ---------------------------------------------------------------
        public static float DistanceToRingPerimeter(List<Vector3> ring, Vector3 v, out int i0, out int i1) {
            int prevIndex = ring.Count - 1;
            i0 = prevIndex;
            i1 = 0;
            float dMin = float.PositiveInfinity;
            for (int i = 0; i < ring.Count; i++) {
                Vector3 x1 = ring[prevIndex], x2 = ring[i];
                float d1 = (x1-v).magnitude, d2 = (x2-v).magnitude;
                float d0 = Vector3.Cross(v-x1,v-x2).magnitude/(x2-x1).magnitude;
                float d = Math.Min(Math.Min(d1,d2),d0);
                if (d < dMin) {
                    dMin = d;
                    i0 = prevIndex;
                    i1 = i;
                }
                prevIndex = i;
            }
            return dMin;
        }

        // -------------------------------------
        // Check if a point lies within a ring
        // -------------------------------------
        public static bool CheckPointInsideRing(List<Vector3> ring, Vector3 v, Vector3 normal) {
            int i0, i1;
            DistanceToRingPerimeter(ring,v,out i0, out i1);
            Vector3 side = ring[i0] - ring[i1];
            Vector3 toSide = ring[i0] - v;
            return
                Vector3.Dot(
                    Vector3.Cross(side,toSide),
                    normal
                ) < 0;
        }

        // -----------------------------------------------------
        // Generate triangle mesh within possibly concave ring
        // -----------------------------------------------------
        public static void GenerateRingMesh(
            List<Vector3> ring, MeshPart part, Vector3 normal
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
                            throw OperationException.Internal("Ear clipping failed");
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

        public class MeshPart {
            public readonly List<Vector3> vertices = new List<Vector3>();
            public readonly List<int> indices = new List<int>();
            
            // Map from original indices to new indices
            public readonly Dictionary<int,int> indexMap = new Dictionary<int, int>();
            public readonly bool side;
            public MeshPart (bool side) {
                this.side = side;
            }
            public void AddMeshTo(GameObject obj) {
                var mesh = obj.AddComponent<MeshFilter>().mesh;
                mesh.vertices = vertices.ToArray();
                mesh.triangles = indices.ToArray();
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
            }

        }

        public static List<MeshPart> PolySep(MeshPart mesh) {

            List<List<int>> list = new List<List<int>>();

            // create index groups
            for (int i = 0; i < mesh.indices.Count; i += 3) {
                int i0 = mesh.indices[i], i1 = mesh.indices[i+1], i2 = mesh.indices[i+2];
                Vector3 v0 = mesh.vertices[i0], v1 = mesh.vertices[i1], v2 = mesh.vertices[i2];
                AddIndices(list,mesh.vertices,v0,v1,v2,i0,i1,i2);
            }

            //MeshPart part = new MeshPart(mesh.side);
            //part.vertices.AddRange(mesh.vertices);
            //part.indices.AddRange(list[0]);
            //return new List<MeshPart>() {part};
            // create new meshes
            return list.ConvertAll(indices=>{

                MeshPart part = new MeshPart(mesh.side);

                foreach (int ind in indices) {
                    if (part.indexMap.ContainsKey(ind)) part.indices.Add(part.indexMap[ind]);
                    else {
                        // Debug.Log(part.vertices.Count+" => "+ind);
                        part.indexMap.Add(ind,part.vertices.Count);
                        part.indices.Add(part.vertices.Count);
                        part.vertices.Add(mesh.vertices[ind]);
                    }
                }

                return part;

            });

        }
        
        public static void AddIndices(
            List<List<int>> list,
            List<Vector3> verts,
            Vector3 v0, Vector3 v1, Vector3 v2,
            int i0, int i1, int i2
        ) {

            List<int> l0 = null, l1 = null, l2 = null;

            // find the set that each vertex belongs to
            foreach (List<int> set in list) {
                foreach (int i in set){
                    Vector3 v = verts[i];
                    if (v == v0 && l0 == null)
                        l0 = set;
                    if (v == v1 && l1 == null)
                        l1 = set;
                    if (v == v2 && l2 == null)
                        l2 = set;
                }
                if (l0 != null && l1 != null && l2 != null) break;
            }

            // if no sets were found, make a new set
            if (l0 == null && l1 == null && l2 == null) {
                list.Add(new List<int>() {i0,i1,i2});
                return;
            }

            // merge l0 into l1 (unless we get ABA, then ignore l0)
            if (l0 != null) {
                if (l1 == null) l1 = l0;
                else if (l0 != l1 && l0 != l2) {
                    list.Remove(l0);
                    l1.AddRange(l0);
                }
            }

            // merge l1 into l2
            if (l1 != null) {
                if (l2 == null) l2 = l1;
                else if (l1 != l2) {
                    list.Remove(l1);
                    l2.AddRange(l1);
                }
            }

            // add to l2
            l2.Add(i0);
            l2.Add(i1);
            l2.Add(i2);

        }

    }

}