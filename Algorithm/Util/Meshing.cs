
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshUtils {

    static class MeshingUtil {

        // -----------------------------------------------------
        // Generate triangle mesh within possibly concave ring
        // -----------------------------------------------------
        public static void GenerateRingMesh(
            Ring ring, MeshPart part, Vector3 normal, Vector2? innerUV, bool addNormals = false
        ) {GenerateRingMesh(ring.verts,part,normal,innerUV,addNormals);}
        public static void GenerateRingMesh(
            List<Vector3> ring, MeshPart part, Vector3 normal, Vector2? innerUV, bool addNormals = false
        ) {GenerateRingMesh(ring,part,normal,part.side,innerUV,addNormals);}
        public static void GenerateRingMesh(
            List<Vector3> ring, MeshPart part, Vector3 normal, bool side, Vector2? innerUV, bool addNormals = false
        ) {

            bool addUVs = innerUV != null;

            // List<List<Vector3>> reduceHist = new List<List<Vector3>>();
            // reduceHist.Add(ring);

            int indStart = part.vertices.Count;
            part.vertices.AddRange(ring);
            if (addUVs || addNormals)
            foreach (var _ in ring) {
                if (addUVs) part.uvs.Add(innerUV.Value);
                if (addNormals) part.normals.Add(side ? -normal : normal);
            }

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
                            throw MeshUtilsException.Internal("Ear clipping failed");
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
                float conv = Vector3.Dot(Vector3.Cross(d1.normalized,d2.normalized),normal);
                if (conv > 0) continue;

                // check that there are no other vertices inside this triangle
                if (set.Exists(v => {
                    // note: we must compare the vectors, otherwise coinciding vectors do not
                    // register as being the same
                    if (v.Item1 == vi0.Item1 || v.Item1 == vi1.Item1 || v.Item1 == vi2.Item1) return false;
                    return VectorUtil.CheckIsInside(d1,d2,v.Item1-vi0.Item1);
                })) continue;

                // generate indices
                if (side) {
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