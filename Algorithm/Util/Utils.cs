
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static MeshUtils.Debugging;

namespace MeshUtils {

    class OperationException : Exception {
        private OperationException(string msg) : base(msg) {}
        public static OperationException ZeroNormal() {
            return new OperationException("The supplied normal vector was of zero length");
        }
        public static OperationException ZeroAreaTriangle() {
            return MalformedMesh("Zero area triangle");
        }
        public static OperationException MalformedMesh(string msg) {
            return new OperationException("The supplied object contains a malformed mesh ("+msg+")");
        }
        public static OperationException Internal(string msg) {
            return new OperationException("Internal error: " + msg);
        }
    }

    class MeshPart {
        public readonly List<Vector3> vertices = new List<Vector3>();
        public readonly List<int> indices = new List<int>();
        public readonly List<Vector2> uvs = new List<Vector2>();
        
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
            mesh.vertices = vertices.ToArray();
            mesh.triangles = indices.ToArray();
            if (uvs.Count > 0 && uvs.Count != vertices.Count)
                throw OperationException.Internal("UV/Vertex length mismatch ("+uvs.Count+" uvs, "+vertices.Count+" verts)");
            if (uvs.Count == vertices.Count) mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
        }

        public List<MeshPart> PolySeperate() {

            List<List<int>> list = new List<List<int>>();

            if (uvs.Count > 0 && uvs.Count != vertices.Count)
                throw OperationException.Internal("UV/Vertex length mismatch ("+uvs.Count+" uvs, "+vertices.Count+" verts)");

            // create index groups
            for (int i = 0; i < indices.Count; i += 3) {
                int i0 = indices[i], i1 = indices[i+1], i2 = indices[i+2];
                Vector3 v0 = vertices[i0], v1 = vertices[i1], v2 = vertices[i2];
                AddIndices(list,vertices,v0,v1,v2,i0,i1,i2,false,false,false);
            }

            return list.ConvertAll(indices=>{

                MeshPart part = new MeshPart(side);

                foreach (int ind in indices) {
                    if (part.indexMap.ContainsKey(ind)) part.indices.Add(part.indexMap[ind]);
                    else {
                        part.indexMap.Add(ind,part.vertices.Count);
                        part.indices.Add(part.vertices.Count);
                        part.vertices.Add(vertices[ind]);
                        if (uvs.Count > 0) part.uvs.Add(uvs[ind]);
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

            List<List<int>> list = new List<List<int>>();

            if (uvs.Count > 0 && uvs.Count != vertices.Count)
                throw OperationException.Internal("UV/Vertex length mismatch ("+uvs.Count+" uvs, "+vertices.Count+" verts)");

            // create index groups
            for (int i = 0; i < indices.Count; i += 3) {
                int i0 = indices[i], i1 = indices[i+1], i2 = indices[i+2];
                Vector3 v0 = vertices[i0], v1 = vertices[i1], v2 = vertices[i2];
                bool ci0 = allow_cut.Contains(v0),
                    ci1 = allow_cut.Contains(v1),
                    ci2 = allow_cut.Contains(v2);
                AddIndices(list,vertices,v0,v1,v2,i0,i1,i2,ci0,ci1,ci2);
                //AddIndices(list,vertices,v0,v1,v2,i0,i1,i2,true,true,true);
            }

            return list.ConvertAll(indices=>{

                MeshPart part = new MeshPart(false);

                int doSwap = 0, doStay = 0; // temporary solution (?)

                foreach (int ind in indices) {
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

                return part;

            });

        }

        static void AddIndices(
            List<List<int>> list,
            List<Vector3> verts,
            Vector3 v0, Vector3 v1, Vector3 v2,
            int i0, int i1, int i2,
            bool ci0, bool ci1, bool ci2
        ) {

            List<int> l0 = null, l1 = null, l2 = null;

            // find the set that each vertex belongs to
            foreach (List<int> set in list) {
                foreach (int i in set){
                    Vector3 v = verts[i];
                    if (
                        (ci0 && i == i0 && l0 == null) ||
                        (!ci0 && v == v0 && l0 == null)
                    ) l0 = set;
                    if (
                        (ci1 && i == i1 && l1 == null) ||
                        (!ci1 && v == v1 && l1 == null)
                    ) l1 = set;
                    if (
                        (ci2 && i == i2 && l2 == null) ||
                        (!ci2 && v == v2 && l2 == null)
                    ) l2 = set;
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
                throw OperationException.ZeroAreaTriangle();
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