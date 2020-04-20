
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static MeshUtils.Util;
using static MeshUtils.API;
using static MeshUtils.MeshingUtil;
using static MeshUtils.TriangleGen;

namespace MeshUtils {

    static class GapAlgorithm {

        public static CutResult Run(
            GameObject target,
            CuttingPlane plane,
            CutParams param
        ) {

            CuttingPlane cutting_plane = plane.ToLocalSpace(target.transform);

            Mesh mesh = target.GetComponent<MeshFilter>().mesh;
            MeshPart pos = new MeshPart(true), neg = new MeshPart(false);
            RingGenerator pos_rings = new RingGenerator(), neg_rings = new RingGenerator();

            Vector2[] uvs = mesh.uv;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            bool addUVs = uvs.Length > 0;

            // removed indices
            HashSet<int> removed = new HashSet<int>();

            // divide mesh in half by vertices
            int i = -1;
            foreach (Vector3 vertex in vertices) {
                i++;
                float dist = cutting_plane.Distance(vertex);
                if (dist < param.seperationDistance) {
                    removed.Add(i);
                    continue;
                }
                if (cutting_plane.IsAbove(vertex)) {
                    pos.indexMap.Add(i,pos.vertices.Count);
                    pos.vertices.Add(vertex);
                    if (addUVs) pos.uvs.Add(uvs[i]);
                } else {
                    neg.indexMap.Add(i,neg.vertices.Count);
                    neg.vertices.Add(vertex);
                    if (addUVs) neg.uvs.Add(uvs[i]);
                }
            }

            // if either vertex list is empty and no vertices were removed, the knife plane didn't collide
            if (pos.vertices.Count == 0 || neg.vertices.Count == 0)
                if (!param.allowSingleResult) return null;

            // put triangles in correct mesh
            for (i = 0; i < triangles.Length; i += 3) {

                // find orignal indices
                int i_a = triangles[i],
                    i_b = triangles[i+1],
                    i_c = triangles[i+2];

                // find if these were removed in cut
                bool r_a = removed.Contains(i_a),
                     r_b = removed.Contains(i_b),
                     r_c = removed.Contains(i_c);

                // if all are removed, ignore triangle
                if (r_a && r_b && r_c) continue;

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

                if (!r_a&&!r_b&&!r_c) {
                    // all available
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
                        if (aAbove == bAbove) {
                            // a, b, c
                            GenTwoTriangles(
                                cutting_plane,
                                aAbove ? pos : neg,
                                a, b, c, txa, txb, txc, i_a, i_b, i_c,
                                aAbove ? pos_rings : neg_rings,
                                addUVs,
                                -param.seperationDistance
                            );
                            GenTriangle(
                                cutting_plane,
                                cAbove ? pos : neg,
                                c, a, b, txc, txa, txb, i_c, i_a, i_b,
                                cAbove ? pos_rings : neg_rings,
                                addUVs,
                                param.seperationDistance
                            );
                        } else if (aAbove == cAbove) {
                            // c, a, b
                            GenTwoTriangles(
                                cutting_plane,
                                aAbove ? pos : neg,
                                c, a, b, txc, txa, txb, i_c, i_a, i_b,
                                aAbove ? pos_rings : neg_rings,
                                addUVs,
                                -param.seperationDistance
                            );
                            GenTriangle(
                                cutting_plane,
                                bAbove ? pos : neg,
                                b, c, a, txb, txc, txa, i_b, i_c, i_a,
                                bAbove ? pos_rings : neg_rings,
                                addUVs,
                                param.seperationDistance
                            );

                        } else if (bAbove == cAbove) {
                            // b, c, a
                            GenTwoTriangles(
                                cutting_plane,
                                bAbove ? pos : neg,
                                b, c, a, txb, txc, txa, i_b, i_c, i_a,
                                bAbove ? pos_rings : neg_rings,
                                addUVs,
                                -param.seperationDistance
                            );
                            GenTriangle(
                                cutting_plane,
                                aAbove ? pos : neg,
                                a, b, c, txa, txb, txc, i_a, i_b, i_c,
                                aAbove ? pos_rings : neg_rings,
                                addUVs,
                                param.seperationDistance
                            );
                        }
                    }
                } else if (!r_a&&!r_b) {
                    // a and b available
                    if (aAbove != bAbove) {
                        GenTriangle(
                            cutting_plane,
                            aAbove ? pos : neg,
                            a, b, c, txa, txb, txc, i_a, i_b, i_c,
                            aAbove ? pos_rings : neg_rings,
                            addUVs,
                            param.seperationDistance
                        );
                        GenTriangle(
                            cutting_plane,
                            bAbove ? pos : neg,
                            b, c, a, txb, txc, txa, i_b, i_c, i_a,
                            bAbove ? pos_rings : neg_rings,
                            addUVs,
                            param.seperationDistance
                        );
                    } else {
                        GenTwoTriangles(
                            cutting_plane,
                            aAbove ? pos : neg,
                            a, b, c, txa, txb, txc, i_a, i_b, i_c,
                            aAbove ? pos_rings : neg_rings,
                            addUVs,
                            -param.seperationDistance
                        );
                    }
                } else if (!r_a&&!r_c) {
                    // a and c available
                    if (aAbove != cAbove) {
                        GenTriangle(
                            cutting_plane,
                            aAbove ? pos : neg,
                            a, b, c, txa, txb, txc, i_a, i_b, i_c,
                            aAbove ? pos_rings : neg_rings,
                            addUVs,
                            param.seperationDistance
                        );
                        GenTriangle(
                            cutting_plane,
                            cAbove ? pos : neg,
                            c, a, b, txc, txa, txb, i_c, i_a, i_b,
                            cAbove ? pos_rings : neg_rings,
                            addUVs,
                            param.seperationDistance
                        );
                    } else {
                        GenTwoTriangles(
                            cutting_plane,
                            aAbove ? pos : neg,
                            c, a, b, txc, txa, txb, i_c, i_a, i_b,
                            aAbove ? pos_rings : neg_rings,
                            addUVs,
                            -param.seperationDistance
                        );
                    }
                } else if (!r_b&&!r_c) {
                    // b and c available
                    if (bAbove != cAbove) {
                        GenTriangle(
                            cutting_plane,
                            bAbove ? pos : neg,
                            b, c, a, txb, txc, txa, i_b, i_c, i_a,
                            bAbove ? pos_rings : neg_rings,
                            addUVs,
                            param.seperationDistance
                        );
                        GenTriangle(
                            cutting_plane,
                            cAbove ? pos : neg,
                            c, a, b, txc, txa, txb, i_c, i_a, i_b,
                            cAbove ? pos_rings : neg_rings,
                            addUVs,
                            param.seperationDistance
                        );
                    } else {
                        GenTwoTriangles(
                            cutting_plane,
                            bAbove ? pos : neg,
                            b, c, a, txb, txc, txa, i_b, i_c, i_a,
                            bAbove ? pos_rings : neg_rings,
                            addUVs,
                            -param.seperationDistance
                        );
                    }
                } else if (!r_a) {
                    // a available
                    GenTriangle(
                        cutting_plane,
                        aAbove ? pos : neg,
                        a, b, c, txa, txb, txc, i_a, i_b, i_c,
                        aAbove ? pos_rings : neg_rings,
                        addUVs,
                        param.seperationDistance
                    );
                } else if (!r_b) {
                    // b available
                    GenTriangle(
                        cutting_plane,
                        bAbove ? pos : neg,
                        b, c, a, txb, txc, txa, i_b, i_c, i_a,
                        bAbove ? pos_rings : neg_rings,
                        addUVs,
                        param.seperationDistance
                    );
                } else {
                    // c available
                    GenTriangle(
                        cutting_plane,
                        cAbove ? pos : neg,
                        c, a, b, txc, txa, txb, i_c, i_a, i_b,
                        cAbove ? pos_rings : neg_rings,
                        addUVs,
                        param.seperationDistance
                    );
                }
                
            }

            List<Ring> pos_ring_res = pos_rings.GetRings(param.selfConnectRings,param.ignorePartialRings);
            List<Ring> neg_ring_res = neg_rings.GetRings(param.selfConnectRings,param.ignorePartialRings);
            
            List<Ring> pos_analysis = param.hiearchyAnalysis ? Hierarchy.Analyse(pos_ring_res, cutting_plane) : pos_ring_res;
            List<Ring> neg_analysis = param.hiearchyAnalysis ? Hierarchy.Analyse(neg_ring_res, cutting_plane) : neg_ring_res;

            // generate seperation meshing
            foreach (var ring in pos_analysis) {
                GenerateRingMesh(ring,pos,cutting_plane.normal,addUVs,param.innerTextureCoord); 
            }
            foreach (var ring in neg_analysis) {
                GenerateRingMesh(ring,neg,cutting_plane.normal,addUVs,param.innerTextureCoord);
            }

            List<MeshPart> resParts = new List<MeshPart>();

            // create new MeshParts
            if (param.polySeperation) {
                if (pos.vertices.Count > 0)
                    resParts.AddRange(pos.PolySeperate());
                if (neg.vertices.Count > 0)
                    resParts.AddRange(neg.PolySeperate());
            } else {
                if (pos.vertices.Count > 0)
                    resParts.Add(pos);
                if (neg.vertices.Count > 0)
                    resParts.Add(neg);
            }

            if (resParts.Count < 2 && !param.allowSingleResult) return null;

            return new CutResult(target,resParts,cutting_plane.ToWorldSpace().normal,new List<Ring>());

        }

    }

}