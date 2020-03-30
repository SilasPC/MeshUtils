
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

            bool addUVs = uvs.Length > 0;

            if (addUVs && uvs.Length != mesh.vertices.Length)
                throw OperationException.Internal("UV/Vertex length mismatch");

            // removed indices
            HashSet<int> removed = new HashSet<int>();

            // divide mesh in half by vertices
            int i = -1;
            foreach (Vector3 vertex in mesh.vertices) {
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
            for (i = 0; i < mesh.triangles.Length; i += 3) {

                // find orignal indices
                int i_a = mesh.triangles[i],
                    i_b = mesh.triangles[i+1],
                    i_c = mesh.triangles[i+2];

                // find if these were removed in cut
                bool r_a = removed.Contains(i_a),
                     r_b = removed.Contains(i_b),
                     r_c = removed.Contains(i_c);

                // if all are removed, ignore triangle
                if (r_a && r_b && r_c) continue;

                // find original verticies
                Vector3 a = mesh.vertices[i_a],
                        b = mesh.vertices[i_b],
                        c = mesh.vertices[i_c];

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
            
            var pos_analysis = Hierarchy.Analyse(pos_rings.GetRings(), cutting_plane);
            var neg_analysis = Hierarchy.Analyse(neg_rings.GetRings(), cutting_plane);

            // generate seperation meshing
            foreach (var ring in pos_analysis.rings) {
                GenerateRingMesh(ring,pos,cutting_plane.normal,addUVs); 
            }
            foreach (var ring in neg_analysis.rings) {
                GenerateRingMesh(ring,neg,cutting_plane.normal,addUVs);
            }

            List<CutObj> cutObjs = new List<CutObj>();

            Vector3? vel = null;
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
                if (pos.vertices.Count > 0)
                    cutObjs.AddRange(pos.PolySeperate().ConvertAll(p=>new CutObj(p,target.transform,vel,worldNormal,mat,pos_rings.GetRings())));
                if (neg.vertices.Count > 0)
                    cutObjs.AddRange(neg.PolySeperate().ConvertAll(p=>new CutObj(p,target.transform,vel,worldNormal,mat,neg_rings.GetRings())));
            } else {
                if (pos.vertices.Count > 0)
                    cutObjs.Add(new CutObj(pos,target.transform,vel,worldNormal,mat,pos_rings.GetRings()));
                if (neg.vertices.Count > 0)
                    cutObjs.Add(new CutObj(neg,target.transform,vel,worldNormal,mat,neg_rings.GetRings()));
            }

            if (cutObjs.Count < 2 && !param.allowSingleResult) return null;

            List<Vector3> centers = pos_analysis.siblingCenters.ConvertAll(v=>target.transform.TransformPoint(v));
            centers.AddRange(neg_analysis.siblingCenters.ConvertAll(v=>target.transform.TransformPoint(v)));

            CutResult result = new CutResult(
                centers,
                cutObjs
            );

            // destroy original object
            if (param.destroyOriginal)
                MonoBehaviour.Destroy(target);

            return result;

        }

    }

}