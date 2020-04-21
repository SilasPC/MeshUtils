
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
            
            //DateTime start = DateTime.Now;

            Vector2[] uvs = mesh.uv;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int[] triangles = mesh.triangles;

            bool addUVs = uvs.Length > 0,
                addNormals = normals.Length > 0;

            // divide mesh in half by vertices
            int i = 0;
            foreach (Vector3 vertex in vertices) {
                if (cutting_plane.IsAbove(vertex)) {
                    pos.indexMap.Add(i,pos.vertices.Count);
                    pos.vertices.Add(vertex);
                    if (addUVs) pos.uvs.Add(uvs[i]);
                    if (addNormals) pos.normals.Add(normals[i]);
                } else {
                    neg.indexMap.Add(i,neg.vertices.Count);
                    neg.vertices.Add(vertex);
                    if (addUVs) neg.uvs.Add(uvs[i]);
                    if (addNormals) neg.normals.Add(normals[i]);
                }
                i++;
            }
            
            //Debug.Log((DateTime.Now-start).TotalMilliseconds+" elapsed (1)");
            //start = DateTime.Now;

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

                // find original uvs
                Vector2 txa, txb, txc;
                if (addUVs) {
                    txa = uvs[i_a];
                    txb = uvs[i_b];
                    txc = uvs[i_c];
                } else txa = txb = txc = Vector3.zero;

                // find original normals
                Vector3 na, nb, nc;
                if (addNormals) {
                    na = normals[i_a];
                    nb = normals[i_b];
                    nc = normals[i_c];
                } else na = nb = nc = Vector3.zero;

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
                            a, b, c, txa, txb, txc, na, nb, nc, i_a, i_b, i_c,
                            rings,
                            addUVs,
                            addNormals
                        );
                    } else if (aAbove == cAbove) {
                        // c, a, b
                        GenTriangles(
                            cutting_plane,
                            aAbove ? pos : neg,
                            !aAbove ? pos : neg,
                            c, a, b, txc, txa, txb, na, nb, nc, i_c, i_a, i_b,
                            rings,
                            addUVs,
                            addNormals
                        );

                    } else if (bAbove == cAbove) {
                        // b, c, a (use bAbove)
                        GenTriangles(
                            cutting_plane,
                            bAbove ? pos : neg,
                            !bAbove ? pos : neg,
                            b, c, a, txb, txc, txa, na, nb, nc, i_b, i_c, i_a,
                            rings,
                            addUVs,
                            addNormals
                        );
                    }
                }
            }

            //Debug.Log((DateTime.Now-start).TotalMilliseconds+" elapsed (2)");
            //start = DateTime.Now;

            List<Ring> ringOut = rings.GetRings(param.selfConnectRings,param.ignorePartialRings);
            
            List<Ring> analysis = param.hiearchyAnalysis ? Hierarchy.Analyse(ringOut, cutting_plane) : ringOut;

            // generate seperation meshing
            foreach (var ring in analysis) {
                GenerateRingMesh(ring,pos,cutting_plane.normal,addUVs,param.innerTextureCoord,addNormals);
                GenerateRingMesh(ring,neg,cutting_plane.normal,addUVs,param.innerTextureCoord,addNormals); 
            }

            //Debug.Log((DateTime.Now-start).TotalMilliseconds+" elapsed (3)");
            //start = DateTime.Now;

            return new CutResult(target,cutting_plane.ToWorldSpace().normal,ringOut,pos,neg);

        }

    }

}