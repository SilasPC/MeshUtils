
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static MeshUtils.Util;
using static MeshUtils.API;
using static MeshUtils.MeshingUtil;
using static MeshUtils.TriangleGen;

namespace MeshUtils {

    static class BasicAlgorithm {

        public static CutResult Run(
            GameObject target,
            CuttingPlane plane,
            CutParams param
        ) {

            CuttingPlane cutting_plane = plane.ToLocalSpace(target.transform);

            Mesh mesh = target.GetComponent<MeshFilter>().mesh;
            MeshPart pos = new MeshPart(true), neg = new MeshPart(false);
            RingGenerator rings = new RingGenerator();

            Vector2[] uvs = mesh.uv;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            bool addUVs = uvs.Length > 0;

            if (addUVs && uvs.Length != vertices.Length)
                throw OperationException.Internal("UV/Vertex length mismatch");

            // divide mesh in half by vertices
            int i = 0;
            foreach (Vector3 vertex in vertices) {
                if (cutting_plane.IsAbove(vertex)) {
                    pos.indexMap.Add(i,pos.vertices.Count);
                    pos.vertices.Add(vertex);
                    if (addUVs) pos.uvs.Add(uvs[i]);
                } else {
                    neg.indexMap.Add(i,neg.vertices.Count);
                    neg.vertices.Add(vertex);
                    if (addUVs) neg.uvs.Add(uvs[i]);
                }
                i++;
            }

            // if either vertex list is empty the knife plane didn't collide
            if (pos.vertices.Count == 0 || neg.vertices.Count == 0)
                if (!param.allowSingleResult) return null;

            // put triangles in correct mesh
            for (i = 0; i < triangles.Length; i += 3) {

                // find orignal indices
                int i_a = triangles[i],
                    i_b = triangles[i+1],
                    i_c = triangles[i+2];
                
                // find original verticies
                Vector3 a = vertices[i_a],
                        b = vertices[i_b],
                        c = vertices[i_c];

                Vector2 txa, txb, txc;

                // find original uvs
                if (uvs.Length > 0) {
                    txa = uvs[i_a];
                    txb = uvs[i_b];
                    txc = uvs[i_c];
                } else txa = txb = txc = Vector2.zero;

                // seperation check
                bool aAbove = cutting_plane.IsAbove(a),
                    bAbove = cutting_plane.IsAbove(b),
                    cAbove = cutting_plane.IsAbove(c);

                if (aAbove && bAbove && cAbove) {
                    // triangle above plane
                    pos.indices.Add(pos.indexMap[i_a]);
                    pos.indices.Add(pos.indexMap[i_b]);
                    pos.indices.Add(pos.indexMap[i_c]);
                } else if (!aAbove && !bAbove && !cAbove) {
                    // triangle below plane
                    neg.indices.Add(neg.indexMap[i_a]);
                    neg.indices.Add(neg.indexMap[i_b]);
                    neg.indices.Add(neg.indexMap[i_c]);
                } else {
                    // triangle crosses plane
                    // call Util.GenTriangles
                    if (aAbove == bAbove) {
                        // a, b, c
                        GenTriangles(
                            cutting_plane,
                            aAbove ? pos : neg,
                            !aAbove ? pos : neg,
                            a, b, c, txa, txb, txc, i_a, i_b, i_c,
                            rings,
                            addUVs
                        );
                    } else if (aAbove == cAbove) {
                        // c, a, b
                        GenTriangles(
                            cutting_plane,
                            aAbove ? pos : neg,
                            !aAbove ? pos : neg,
                            c, a, b, txc, txa, txb, i_c, i_a, i_b,
                            rings,
                            addUVs
                        );

                    } else if (bAbove == cAbove) {
                        // b, c, a (use bAbove)
                        GenTriangles(
                            cutting_plane,
                            bAbove ? pos : neg,
                            !bAbove ? pos : neg,
                            b, c, a, txb, txc, txa, i_b, i_c, i_a,
                            rings,
                            addUVs
                        );
                    }
                }
            }

            List<Ring> ringOut = rings.GetRings(param.selfConnectRings,param.ignorePartialRings);
            
            List<Ring> analysis = param.hiearchyAnalysis ? Hierarchy.Analyse(ringOut, cutting_plane) : ringOut;

            // generate seperation meshing
            foreach (var ring in analysis) {
                GenerateRingMesh(ring,pos,cutting_plane.normal,addUVs);
                GenerateRingMesh(ring,neg,cutting_plane.normal,addUVs); 
            }

           List<CutObj> cutObjs = new List<CutObj>();

            Vector3? vel = null;
            float? density = null;
            Rigidbody rb;
            if (target.TryGetComponent<Rigidbody>(out rb)) {
                vel = rb.velocity;
            }

            Material mat = null;
            Renderer renderer;
            if (target.TryGetComponent<Renderer>(out renderer)) mat = renderer.material;

            Vector3 worldNormal = cutting_plane.ToWorldSpace().normal;


            // create new objects
            if (param.polySeperation) {
                cutObjs.AddRange(pos.PolySeperate().ConvertAll(p=>new CutObj(p,target.transform,vel,worldNormal,mat,ringOut)));
                cutObjs.AddRange(neg.PolySeperate().ConvertAll(p=>new CutObj(p,target.transform,vel,worldNormal,mat,ringOut)));
            } else {
                cutObjs.Add(new CutObj(pos,target.transform,vel,worldNormal,mat,ringOut));
                cutObjs.Add(new CutObj(neg,target.transform,vel,worldNormal,mat,ringOut));
            }

            CutResult result = new CutResult(cutObjs);

            // destroy original object
            if (param.destroyOriginal)
                MonoBehaviour.Destroy(target);

            return result;

        }

    }

}