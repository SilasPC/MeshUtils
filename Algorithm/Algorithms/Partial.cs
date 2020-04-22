
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static MeshUtils.Util;
using static MeshUtils.API;
using static MeshUtils.MeshingUtil;
using static MeshUtils.TriangleGen;

namespace MeshUtils {

    static class PartialAlgorithm {

        public static CutResult Run(
            GameObject target,
            CuttingPlane plane,
            CutParams param
        ) {

            CuttingPlane cutting_plane = plane.ToLocalSpace(target.transform);
            Mesh mesh = target.GetComponent<MeshFilter>().mesh;

            MeshPart part = new MeshPart(false);

            Vector2[] uvs = mesh.uv;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            //
            // First we find the rings that are eligable for cutting
            //

            RingGenerator rings = new RingGenerator();
            
            int i;
            for (i = 0; i < triangles.Length; i += 3) {

                // find orignal indices
                int i_a = triangles[i],
                    i_b = triangles[i+1],
                    i_c = triangles[i+2];
                
                // find original verticies
                Vector3 a = vertices[i_a],
                        b = vertices[i_b],
                        c = vertices[i_c];

                // seperation check
                bool aAbove = cutting_plane.IsAbove(a),
                    bAbove = cutting_plane.IsAbove(b),
                    cAbove = cutting_plane.IsAbove(c);

                if (aAbove == bAbove && aAbove == cAbove) continue;

                if (aAbove == bAbove) {
                    // a, b, c
                    GenIntersection(
                        cutting_plane,
                        a, b, c,
                        rings
                    );
                } else if (aAbove == cAbove) {
                    // c, a, b
                    GenIntersection(
                        cutting_plane,
                        c, a, b,
                        rings
                    );
                } else if (bAbove == cAbove) {
                    // b, c, a
                    GenIntersection(
                        cutting_plane,
                        b, c, a,
                        rings
                    );
                }

            }

            Vector3 point = target.transform.InverseTransformPoint(param.originPoint);

            List<Ring> resulting_rings = new List<Ring>();
            foreach (Ring ring in rings.GetRings(param.selfConnectRings,param.ignorePartialRings)) {
                Vector3 vec = ring.FurthestVectorToRingPerimeter(point);
                vec = target.transform.TransformVector(vec);
                // Debug.DrawRay(param.originPoint,vec,Color.blue,10);
                float mag = vec.magnitude;
                Debug.Log(mag);
                if (mag < param.maxCutDistance) resulting_rings.Add(ring);
            }

            if (resulting_rings.Count == 0) return null;

            // Debug.Log(resulting_rings.Count);

            HashSet<Vector3> allow_cut = new HashSet<Vector3>();
            
            foreach (Ring ring in resulting_rings)
            foreach (Vector3 v in ring.verts)
                allow_cut.Add(v);

            //
            // Start of cutting
            //

            bool addUVs = uvs.Length > 0 && param.innerTextureCoord != null;

            // transfer vertices into MeshPart
            i = 0;
            foreach (Vector3 vertex in vertices) {
                part.indexMap.Add(i,part.vertices.Count);
                part.vertices.Add(vertex);
                if (addUVs) part.uvs.Add(uvs[i]);
                i++;
            }

            // process triangles
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

                if (aAbove == bAbove && aAbove == cAbove) {
                    // triangle on one side of plane
                    part.indices.Add(part.indexMap[i_a]);
                    part.indices.Add(part.indexMap[i_b]);
                    part.indices.Add(part.indexMap[i_c]);
                } else {
                    // triangle crosses plane
                    if (aAbove == bAbove) {
                        // a, b, c
                        GenPartialTriangles(
                            cutting_plane,
                            part,
                            a, b, c, txa, txb, txc, i_a, i_b, i_c,
                            allow_cut,
                            addUVs
                        );
                    } else if (aAbove == cAbove) {
                        // c, a, b
                        GenPartialTriangles(
                            cutting_plane,
                            part,
                            c, a, b, txc, txa, txb, i_c, i_a, i_b,
                            allow_cut,
                            addUVs
                        );
                    } else if (bAbove == cAbove) {
                        // b, c, a
                        GenPartialTriangles(
                            cutting_plane,
                            part,
                            b, c, a, txb, txc, txa, i_b, i_c, i_a,
                            allow_cut,
                            addUVs
                        );
                    }
                }
            }

            List<Ring> analysis = param.hiearchyAnalysis ? Hierarchy.Analyse(resulting_rings,cutting_plane) : resulting_rings;

            List<MeshPart> parts = part.PartialPolySeperate(cutting_plane,allow_cut);

            if (parts.Count < 2 && !param.allowSingleResult) return null;

            // generate seperation meshing
            if (parts.Count > 0)
            foreach (var ring in analysis)
            foreach (var resPart in parts)
                GenerateRingMesh(ring,resPart,cutting_plane.normal,param.innerTextureCoord);

            return new CutResult(target,parts,cutting_plane.ToWorldSpace().normal,resulting_rings,true);

        }

    }
    
}
