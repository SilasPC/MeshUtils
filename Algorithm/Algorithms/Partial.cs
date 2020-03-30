
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MeshUtils.Util;
using static MeshUtils.API;

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

            //
            // First we find the rings that are eligable for cutting
            //

            RingGenerator rings = new RingGenerator();
            
            int i;
            for (i = 0; i < mesh.triangles.Length; i += 3) {

                // find orignal indices
                int i_a = mesh.triangles[i],
                    i_b = mesh.triangles[i+1],
                    i_c = mesh.triangles[i+2];
                
                // find original verticies
                Vector3 a = mesh.vertices[i_a],
                        b = mesh.vertices[i_b],
                        c = mesh.vertices[i_c];

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
            foreach (Ring ring in rings.GetRings()) {
                Vector3 vec = ring.FurthestVectorToRingPerimeter(point);
                vec = target.transform.TransformVector(vec);
                // Debug.DrawRay(param.originPoint,vec,Color.blue,10);
                float mag = vec.magnitude;
                // Debug.Log(mag);
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

            Vector2[] uvs = mesh.uv;

            if (uvs.Length > 0 && uvs.Length != mesh.vertices.Length)
                throw OperationException.Internal("UV/Vertex length mismatch");

            bool addUVs = uvs.Length > 0;

            // divide mesh in half by vertices
            i = 0;
            foreach (Vector3 vertex in mesh.vertices) {
                part.indexMap.Add(i,part.vertices.Count);
                part.vertices.Add(vertex);
                if (addUVs) part.uvs.Add(uvs[i]);
                i++;
            }

            // put triangles in correct mesh
            for (i = 0; i < mesh.triangles.Length; i += 3) {

                // find orignal indices
                int i_a = mesh.triangles[i],
                    i_b = mesh.triangles[i+1],
                    i_c = mesh.triangles[i+2];
                
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

            var analysis = Hierarchy.Analyse(resulting_rings,cutting_plane);

            List<MeshPart> parts;
            if (param.polySeperation) parts = part.PartialPolySeperate(cutting_plane,allow_cut);
            else parts = new List<MeshPart>() {part};

            if (parts.Count < 2 && !param.allowSingleResult) return null;

            // generate seperation meshing
            if (parts.Count > 1)
            foreach (var ring in analysis.rings)
            foreach (var resPart in parts)
                GenerateRingMesh(ring,resPart,cutting_plane.normal,addUVs);

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
            List<CutObj> cutObjs = parts.ConvertAll(p=>new CutObj(p,target.transform,vel,worldNormal,mat,resulting_rings));

            CutResult result = new CutResult(
                analysis.siblingCenters.ConvertAll(v=>target.transform.TransformPoint(v)),
                cutObjs
            );

            // destroy original object
            if (param.destroyOriginal)
                MonoBehaviour.Destroy(target);

            return result;

        }

    }
    
}
