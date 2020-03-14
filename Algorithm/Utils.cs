
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
        public static OperationException MalformedMesh() {
            return new OperationException("The supplied object contains a malformed mesh");
        }
        public static OperationException Internal(string msg) {
            return new OperationException("Internal error: " + msg);
        }
    }

    static class Util {

        // ------------------------------------
        // Check if float is in a given range
        // ------------------------------------
        public static bool inRange(float min, float max, float val) {
            return val >= min && val <= max;
        }

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
                throw OperationException.MalformedMesh();
            }

            // find proper direction for ring
            Vector3 tri_nor = Vector3.Cross(c-a,c-b);
            bool dir = Vector3.Dot(e-d,Vector3.Cross(plane.normal,tri_nor)) > 0;

            // add connected pair in ring generator
            rings.AddConnected(dir?e:d,dir?d:e);
        }

        // -----------------------------------------------------------
        // Generate triangles for an intersecting triangle a,b,c
        // It is assumed that a and b are in the positive half space,
        // and c is in the negative half space.
        // -----------------------------------------------------------
        public static void GenTriangles(
            CuttingPlane plane,
            MeshPart pos, MeshPart neg,
            Vector3 a, Vector3 b,Vector3 c,
            Vector2 txa, Vector2 txb, Vector2 txc,
            int i_a, int i_b, int i_c,
            RingGenerator rings,
            bool addUVs
        ) {

            // find intersection vertices / uvs
            var es = plane.Intersection(a, c, txa, txc, 0);
            var ds = plane.Intersection(b, c, txb, txc, 0);

            Vector3 e = es.Item1, d = ds.Item1;
            Vector2 txe = es.Item2, txd = ds.Item2;

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

            // add new vertices and uvs
            pos.vertices.Add(d);
            pos.vertices.Add(e);
            neg.vertices.Add(d);
            neg.vertices.Add(e);
            if (addUVs) {
                pos.uvs.Add(txd);
                pos.uvs.Add(txe);
                neg.uvs.Add(txd);
                neg.uvs.Add(txe);
            }

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

        public static void GenPartialTriangles(
            CuttingPlane plane,
            MeshPart part,
            Vector3 a, Vector3 b,Vector3 c,
            Vector2 txa, Vector2 txb, Vector2 txc,
            int i_a, int i_b, int i_c,
            HashSet<Vector3> allow_cut,
            bool addUVs
        ) {

            // find intersection vertices / uvs
            var es = plane.Intersection(a, c, txa, txc, 0);
            var ds = plane.Intersection(b, c, txb, txc, 0);

            Vector3 e = es.Item1, d = ds.Item1;
            Vector2 txe = es.Item2, txd = ds.Item2;

            // if e == d, the three vertices lie in a line,
            //   and thus do not make up a triangle
            if (e == d) {
                return;
                // not sure if this is nescessary
                throw OperationException.MalformedMesh();
            }

            if (
                !allow_cut.Contains(e) &&
                !allow_cut.Contains(d)
            ) {
                // triangle must not be cut
                part.indices.Add(part.indexMap[i_a]);
                part.indices.Add(part.indexMap[i_b]);
                part.indices.Add(part.indexMap[i_c]);
                return;
            }

            // new indices
            int i0 = part.vertices.Count, i1 = part.vertices.Count + 2;

            // add new vertices and uvs
            part.vertices.Add(d);
            part.vertices.Add(e);
            part.vertices.Add(d);
            part.vertices.Add(e);
            if (addUVs) {
                part.uvs.Add(txd);
                part.uvs.Add(txe);
                part.uvs.Add(txd);
                part.uvs.Add(txe);
            }

            // generate triangles ...

            // add a,d,e
            part.indices.Add(part.indexMap[i_a]);
            part.indices.Add(i0);
            part.indices.Add(i0+1);
            // add a,b,d
            part.indices.Add(part.indexMap[i_a]);
            part.indices.Add(part.indexMap[i_b]);
            part.indices.Add(i0);
            // add e,d,c
            part.indices.Add(i1+1);
            part.indices.Add(i1);
            part.indices.Add(part.indexMap[i_c]);

        }

        public static void GenTwoTriangles(
            CuttingPlane plane,
            MeshPart pos,
            Vector3 a, Vector3 b,Vector3 c,
            Vector2 txa, Vector2 txb, Vector2 txc,
            int i_a, int i_b, int i_c,
            RingGenerator rings,
            bool addUVs,
            float shift
        ) {

            // find intersection vertices / uvs
            var es = plane.Intersection(a, c, txa, txc, shift);
            var ds = plane.Intersection(b, c, txb, txc, shift);

            Vector3 e = es.Item1, d = ds.Item1;
            Vector2 txe = es.Item2, txd = ds.Item2;

            // if e == d, the three vertices lie in a line,
            //   and thus do not make up a triangle
            if (e == d) {
                return;
                // not sure if this is nescessary
                throw OperationException.MalformedMesh();
            }

            // new indices
            int i0 = pos.vertices.Count;

            // add connected pair in ring generator

            // find proper direction for ring
            Vector3 tri_nor = Vector3.Cross(c-a,c-b);
            bool dir = Vector3.Dot(e-d,Vector3.Cross(plane.normal,tri_nor)) > 0;

            rings.AddConnected(dir?e:d,dir?d:e);

            // add new vertices and uvs
            pos.vertices.Add(d);
            pos.vertices.Add(e);
            if (addUVs) {
                pos.uvs.Add(txd);
                pos.uvs.Add(txe);
            }

            // generate triangles for sides ...

            // add a,d,e to positive indicies
            pos.indices.Add(pos.indexMap[i_a]);
            pos.indices.Add(i0);
            pos.indices.Add(i0+1);
            // add a,b,d to positive indicies
            pos.indices.Add(pos.indexMap[i_a]);
            pos.indices.Add(pos.indexMap[i_b]);
            pos.indices.Add(i0);

        }

        // ------------------------------------------------------------
        // Generate single triangle for an intersecting triangle a,b,c
        // It is assumed that a is on the positive half plane
        // ------------------------------------------------------------
        public static void GenTriangle(
            CuttingPlane plane,
            MeshPart pos,
            Vector3 a, Vector3 b,Vector3 c,
            Vector2 txa, Vector2 txb, Vector2 txc,
            int i_a, int i_b, int i_c,
            RingGenerator rings,
            bool addUVs,
            float shift
        ) {

            // find intersection vertices / uvs
            var es = plane.Intersection(c, a, txa, txc, shift);
            var ds = plane.Intersection(b, a, txb, txc, shift);

            Vector3 e = es.Item1, d = ds.Item1;
            Vector2 txe = es.Item2, txd = ds.Item2;

            // if e == d, the three vertices lie in a line,
            //   and thus do not make up a triangle
            if (e == d) {
                return;
                // not sure if this is nescessary
                throw OperationException.MalformedMesh();
            }

            // new indices
            int i0 = pos.vertices.Count;

            // add connected pair in ring generator

            // find proper direction for ring
            Vector3 tri_nor = Vector3.Cross(c-a,c-b);
            bool dir = Vector3.Dot(e-d,Vector3.Cross(plane.normal,tri_nor)) > 0;

            rings.AddConnected(dir?e:d,dir?d:e);

            // add new vertices and uvs
            pos.vertices.Add(d);
            pos.vertices.Add(e);
            if (addUVs) {
                pos.uvs.Add(txd);
                pos.uvs.Add(txe);
            }

            // add a,d,e to positive indicies
            pos.indices.Add(pos.indexMap[i_a]);
            pos.indices.Add(i0);
            pos.indices.Add(i0+1);

        }

        // -----------------------------------------------------
        // Generate triangle mesh within possibly concave ring
        // -----------------------------------------------------
        public static void GenerateRingMesh(
            Ring ring, MeshPart part, Vector3 normal, bool addUVs
        ) {

            // List<List<Vector3>> reduceHist = new List<List<Vector3>>();
            // reduceHist.Add(ring);

            int indStart = part.vertices.Count;
            part.vertices.AddRange(ring.verts);
            if (addUVs) foreach (var _ in ring.verts) part.uvs.Add(Vector2.zero);

            List<Tuple<Vector3,int>> set = ring.verts.ConvertAll(v=>new Tuple<Vector3,int>(v,indStart++));

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
                    return VectorUtil.CheckIsInside(d1,d2,v.Item1-vi0.Item1);
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
                    throw OperationException.Internal("UV/Vertex length mismatch");
                if (uvs.Count == vertices.Count) mesh.uv = uvs.ToArray();
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
            }

            public List<MeshPart> PolySeperate() {

                List<List<int>> list = new List<List<int>>();

                if (uvs.Count > 0 && uvs.Count != vertices.Count)
                    throw OperationException.Internal("UV/Vertex length mismatch");

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
                    throw OperationException.Internal("UV/Vertex length mismatch");

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

                    bool doSwap = false; // temporary solution

                    foreach (int ind in indices) {
                        Vector3 point = vertices[ind];
                        if (!doSwap&&!allow_cut.Contains(point)&&plane.IsAbove(point)) doSwap = true;
                        if (part.indexMap.ContainsKey(ind)) part.indices.Add(part.indexMap[ind]);
                        else {
                            part.indexMap.Add(ind,part.vertices.Count);
                            part.indices.Add(part.vertices.Count);
                            part.vertices.Add(point);
                            if (uvs.Count > 0) part.uvs.Add(uvs[ind]);
                        }
                    }

                    if (doSwap) part.SwapSide();

                    return part;

                });

            }

        }
        
        public static void AddIndices(
            List<List<int>> list,
            List<Vector3> verts,
            Vector3 v0, Vector3 v1, Vector3 v2,
            int i0, int i1, int i2,
            bool ci0, bool ci1, bool ci2
        ) {

            List<int> l0 = null, l1 = null, l2 = null;

            /*
            if (!ci0&&inRange(1.1f,2.9f,v0.y)) Debug.Log(i0+":"+VecStr(v0));
            if (!ci1&&inRange(1.1f,2.9f,v1.y)) Debug.Log(i1+":"+VecStr(v1));
            if (!ci2&&inRange(1.1f,2.9f,v2.y)) Debug.Log(i2+":"+VecStr(v2));
            */

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