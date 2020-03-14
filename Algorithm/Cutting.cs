
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MeshUtils.Util;
using static MeshUtils.API;

namespace MeshUtils {

    static class Algorithms {

        public static CutResult PartialPlanarCut(
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
                Debug.Log(ring.FurthestDistanceToRingPerimeter(point));
                if (ring.FurthestDistanceToRingPerimeter(point) < param.maxCutDistance) resulting_rings.Add(ring);
            }

            if (resulting_rings.Count == 0) return null;

            Debug.Log(resulting_rings.Count);

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
            List<CutObj> cutObjs = parts.ConvertAll(p=>new CutObj(p,target.transform,vel,worldNormal,mat));

            CutResult result = new CutResult(
                analysis.siblingCenters.ConvertAll(v=>target.transform.TransformPoint(v)),
                cutObjs
            );

            // destroy original object
            if (param.destroyOriginal)
                MonoBehaviour.Destroy(target);

            return result;

        }

        public static CutResult PlanarCutWithoutGap(
            GameObject target,
            CuttingPlane plane,
            CutParams param
        ) {

            CuttingPlane cutting_plane = plane.ToLocalSpace(target.transform);

            Mesh mesh = target.GetComponent<MeshFilter>().mesh;
            MeshPart pos = new MeshPart(true), neg = new MeshPart(false);
            RingGenerator rings = new RingGenerator();

            Vector2[] uvs = mesh.uv;

            bool addUVs = uvs.Length > 0;

            if (addUVs && uvs.Length != mesh.vertices.Length)
                throw OperationException.Internal("UV/Vertex length mismatch");

            // divide mesh in half by vertices
            int i = 0;
            foreach (Vector3 vertex in mesh.vertices) {
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
            
            var analysis = Hierarchy.Analyse(rings.GetRings(), cutting_plane);

            // generate seperation meshing
            foreach (var ring in analysis.rings) {
                GenerateRingMesh(ring,pos,cutting_plane.normal,addUVs);
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
                cutObjs.AddRange(pos.PolySeperate().ConvertAll(p=>new CutObj(p,target.transform,vel,worldNormal,mat)));
                cutObjs.AddRange(neg.PolySeperate().ConvertAll(p=>new CutObj(p,target.transform,vel,worldNormal,mat)));
            } else {
                cutObjs.Add(new CutObj(pos,target.transform,vel,worldNormal,mat));
                cutObjs.Add(new CutObj(neg,target.transform,vel,worldNormal,mat));
            }

            CutResult result = new CutResult(
                analysis.siblingCenters.ConvertAll(v=>target.transform.TransformPoint(v)),
                cutObjs
            );

            // destroy original object
            if (param.destroyOriginal)
                MonoBehaviour.Destroy(target);

            return result;

        }

        public static CutResult PlanarCutWithGap(
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
                    cutObjs.AddRange(pos.PolySeperate().ConvertAll(p=>new CutObj(p,target.transform,vel,worldNormal,mat)));
                if (neg.vertices.Count > 0)
                    cutObjs.AddRange(neg.PolySeperate().ConvertAll(p=>new CutObj(p,target.transform,vel,worldNormal,mat)));
            } else {
                if (pos.vertices.Count > 0)
                    cutObjs.Add(new CutObj(pos,target.transform,vel,worldNormal,mat));
                if (neg.vertices.Count > 0)
                    cutObjs.Add(new CutObj(neg,target.transform,vel,worldNormal,mat));
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