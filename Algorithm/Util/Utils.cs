
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static MeshUtils.Debugging;

namespace MeshUtils {

    class MeshUtilsException : Exception {
        private MeshUtilsException(string msg) : base(msg) {}
        public static MeshUtilsException ZeroNormal() {
            return new MeshUtilsException("The supplied normal vector was of zero length");
        }
        public static MeshUtilsException ZeroAreaTriangle() {
            return MalformedMesh("Zero area triangle");
        }
        public static MeshUtilsException MalformedMesh(string msg) {
            return new MeshUtilsException("The supplied object contains a malformed mesh ("+msg+")");
        }
        public static MeshUtilsException Internal(string msg) {
            return new MeshUtilsException("Internal error: " + msg);
        }
    }

    class MeshPart {
        public readonly List<Vector3> vertices = new List<Vector3>();
        public readonly List<int> indices = new List<int>();
        public readonly List<Vector2> uvs = new List<Vector2>();
        public readonly List<Vector3> normals = new List<Vector3>();
        
        // Map from original indices to new indices
        public readonly Dictionary<int,int> indexMap = new Dictionary<int, int>();
        private bool _side;
        public bool side {
            get {return _side;}
        }
        public MeshPart (bool side) {
            this._side = side;
        }

        public void SwapSide() {
            this._side = !this._side;
        }

        public void AddMeshTo(GameObject obj) {
            var mesh = obj.AddComponent<MeshFilter>().mesh;
            AssertMatchingCounts();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = indices.ToArray();
            if (uvs.Count == vertices.Count) mesh.uv = uvs.ToArray();
            if (normals.Count == vertices.Count) mesh.normals = normals.ToArray();
            else mesh.RecalculateNormals(); // allow control of these ?
            mesh.RecalculateTangents();
        }

        private void AssertMatchingCounts() {
            if (uvs.Count > 0 && uvs.Count != vertices.Count)
                throw MeshUtilsException.Internal("UV/vertex count mismatch ("+uvs.Count+" uvs, "+vertices.Count+" verts)");
            if (normals.Count > 0 && normals.Count != vertices.Count)
                throw MeshUtilsException.Internal("Normal/vertex count mismatch ("+normals.Count+" normals, "+vertices.Count+" verts)");
        }

        public List<MeshPart> PolySeperate() {

            List<Tuple<HashSet<int>, HashSet<Vector3>, List<int>>> list = new List<Tuple<HashSet<int>, HashSet<Vector3>, List<int>>>();
            
            AssertMatchingCounts();

            // create index groups
            for (int i = 0; i < indices.Count; i += 3) {
                int i0 = indices[i], i1 = indices[i+1], i2 = indices[i+2];
                Vector3 v0 = vertices[i0], v1 = vertices[i1], v2 = vertices[i2];
                AddIndices(list,vertices,v0,v1,v2,i0,i1,i2,false,false,false);
            }

            return list.ConvertAll(set=>{

                MeshPart part = new MeshPart(side);

                foreach (int ind in set.Item3) {
                    if (part.indexMap.ContainsKey(ind)) part.indices.Add(part.indexMap[ind]);
                    else {
                        part.indexMap.Add(ind,part.vertices.Count);
                        part.indices.Add(part.vertices.Count);
                        part.vertices.Add(vertices[ind]);
                        if (uvs.Count > 0) part.uvs.Add(uvs[ind]);
                        if (normals.Count > 0) part.normals.Add(normals[ind]);
                    }
                }

                return part;

            });

        }

        public List<MeshPart> PartialPolySeperate(
            CuttingPlane plane,
            HashSet<Vector3> allow_cut
        ) {

            //Debug.Log("allow_cut");
            //foreach (Vector3 v in allow_cut) Debug.Log(VecStr(v));
            //Debug.Log("missing");

            List<Tuple<HashSet<int>, HashSet<Vector3>, List<int>>> list = new List<Tuple<HashSet<int>, HashSet<Vector3>, List<int>>>();

            AssertMatchingCounts();

            // create index groups
            for (int i = 0; i < indices.Count; i += 3) {
                int i0 = indices[i], i1 = indices[i+1], i2 = indices[i+2];
                Vector3 v0 = vertices[i0], v1 = vertices[i1], v2 = vertices[i2];
                bool ci0 = allow_cut.Contains(v0),
                    ci1 = allow_cut.Contains(v1),
                    ci2 = allow_cut.Contains(v2);
                AddIndices(list,vertices,v0,v1,v2,i0,i1,i2,ci0,ci1,ci2);
            }

            return list.ConvertAll(set=>{

                MeshPart part = new MeshPart(false);

                int doSwap = 0, doStay = 0; // temporary solution (?)

                foreach (int ind in set.Item3) {
                    Vector3 point = vertices[ind];
                    if (!allow_cut.Contains(point)) {
                        if (plane.IsAbove(point)) doSwap++;
                        else doStay++;
                    };
                    if (part.indexMap.ContainsKey(ind)) part.indices.Add(part.indexMap[ind]);
                    else {
                        part.indexMap.Add(ind,part.vertices.Count);
                        part.indices.Add(part.vertices.Count);
                        part.vertices.Add(point);
                        if (uvs.Count > 0) part.uvs.Add(uvs[ind]);
                    }
                }

                if (doSwap > doStay) part.SwapSide();

                part.AssertMatchingCounts();

                return part;

            });

        }

        static void AddIndices(
            List<Tuple<HashSet<int>,HashSet<Vector3>,List<int>>> list,
            List<Vector3> verts,
            Vector3 v0, Vector3 v1, Vector3 v2,
            int i0, int i1, int i2,
            bool ci0, bool ci1, bool ci2
        ) {

            Tuple<HashSet<int>,HashSet<Vector3>,List<int>> l0 = null, l1 = null, l2 = null;

            // find the set that each vertex belongs to
            foreach (Tuple<HashSet<int>,HashSet<Vector3>,List<int>> set in list) {
                if (l0 == null && (ci0 ? set.Item1.Contains(i0) : set.Item2.Contains(v0)))
                    l0 = set;
                if (l1 == null && (ci1 ? set.Item1.Contains(i1) : set.Item2.Contains(v1)))
                    l1 = set;
                if (l2 == null && (ci2 ? set.Item1.Contains(i2) : set.Item2.Contains(v2)))
                    l2 = set;
                if (l0 != null && l1 != null && l2 != null) break;
            }

            // if no sets were found, make a new set
            if (l0 == null && l1 == null && l2 == null) {
                list.Add(new Tuple<HashSet<int>, HashSet<Vector3>, List<int>>(
                    new HashSet<int>() {i0,i1,i2},
                    new HashSet<Vector3>() {v0,v1,v2},
                    new List<int>() {i0,i1,i2}
                ));
                return;
            }

            // merge l0 into l1 (unless we get ABA, then ignore l0)
            if (l0 != null) {
                if (l1 == null) l1 = l0;
                else if (l0 != l1 && l0 != l2) {
                    list.Remove(l0);
                    l1.Item1.UnionWith(l0.Item1);
                    l1.Item2.UnionWith(l0.Item2);
                    l1.Item3.AddRange(l0.Item3);
                }
            }

            // merge l1 into l2
            if (l1 != null) {
                if (l2 == null) l2 = l1;
                else if (l1 != l2) {
                    list.Remove(l1);
                    l2.Item1.UnionWith(l1.Item1);
                    l2.Item2.UnionWith(l1.Item2);
                    l2.Item3.AddRange(l1.Item3);
                }
            }

            // add to l2
            l2.Item1.Add(i0);
            l2.Item1.Add(i1);
            l2.Item1.Add(i2);
            l2.Item2.Add(v0);
            l2.Item2.Add(v1);
            l2.Item2.Add(v2);
            l2.Item3.Add(i0);
            l2.Item3.Add(i1);
            l2.Item3.Add(i2);

        }

    }

    static class Util {

        public static void GenIntersection(
            CuttingPlane plane,
            Vector3 a, Vector3 b,Vector3 c,
            RingGenerator rings
        ) {
            // find intersection vertices
            Vector3 e = plane.Intersection(a, c, Vector2.zero, Vector2.zero, 0).Item1,
                    d = plane.Intersection(b, c, Vector2.zero, Vector2.zero, 0).Item1;

            // if e == d, the three vertices lie in a line,
            //   and thus do not make up a triangle
            if (e == d) {
                return;
                // not sure if this is nescessary
                throw MeshUtilsException.ZeroAreaTriangle();
            }

            // find proper direction for ring
            Vector3 tri_nor = Vector3.Cross(c-a,c-b);
            bool dir = Vector3.Dot(e-d,Vector3.Cross(plane.normal,tri_nor)) > 0;

            // add connected pair in ring generator
            rings.AddConnected(dir?e:d,dir?d:e);
        }

        public static void SetGlobalScale(Transform transform, Vector3 scale) {
            transform.localScale = Vector3.one;
            transform.localScale = new Vector3(
                scale.x/transform.lossyScale.x,
                scale.y/transform.lossyScale.y,
                scale.z/transform.lossyScale.z
            );
        }

    }

}