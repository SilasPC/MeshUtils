
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MeshUtils.Util;

namespace MeshUtils {

    static class API {

        public struct CutParams {
            public bool polySeperation;
            public bool destroyOriginal;
            public CutParams (
                bool polySeperation,
                bool destroyOriginal
            ) {
                this.polySeperation = polySeperation;
                this.destroyOriginal = destroyOriginal;
            }
        }

        public class CutResult {

            private readonly MeshPart part;
            private readonly Vector3 pos, scl;
            private readonly Quaternion rot;
            
            private bool addRenderer = false;
            private bool addCollider = false;
            private bool addRigidbody = false;
            private Color? addColor = null;
            
            public CutResult(MeshPart part, Transform orig) {
                this.part = part;
                this.pos = orig.position;
                this.rot = orig.rotation;
                this.scl = orig.localScale;
            }

            public CutResult WithRenderer() {
                this.addRenderer = true;
                return this;
            }

            public CutResult WithColor(Color col) {
                this.addColor = col;
                this.addRenderer = true;
                return this;
            }

            public CutResult WithCollider() {
                this.addCollider = true;
                return this;
            }

            public CutResult WithRigidbody() {
                this.addRigidbody = true;
                return this;
            }

            public GameObject Create() {
                GameObject obj = new GameObject();
                this.part.AddMeshTo(obj);
                if (this.addRigidbody)
                    obj.AddComponent<Rigidbody>();
                if (this.addCollider)
                    obj.AddComponent<MeshCollider>().convex = true;
                if (this.addRenderer) {
                    MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
                    if (this.addColor is Color color)
                        renderer.material.color = color;
                }

                obj.transform.position = this.pos;
                obj.transform.rotation = this.rot;
                obj.transform.localScale = this.scl;

                return obj;

            }

        }

        public static bool PerformCut(
            GameObject target,
            CuttingPlane cutting_plane,
            CutParams param,
            out List<CutResult> result
        ) {

            Mesh mesh = target.GetComponent<MeshFilter>().mesh;
            MeshPart pos = new MeshPart(true), neg = new MeshPart(false);
            RingGenerator rings = new RingGenerator();

            // divide mesh in half by vertices
            int i = 0;
            foreach (Vector3 vertex in mesh.vertices) {
                if (cutting_plane.IsAbove(vertex)) {
                    pos.indexMap.Add(i,pos.vertices.Count);
                    pos.vertices.Add(vertex);
                } else {
                    neg.indexMap.Add(i,neg.vertices.Count);
                    neg.vertices.Add(vertex);
                }
                i++;
            }

            // if either vertex list is empty the knife plane didn't collide
            if (pos.vertices.Count == 0 || neg.vertices.Count == 0) {
                result = null;
                return false;
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
                            a, b, c, i_a, i_b, i_c,
                            rings
                        );
                    } else if (aAbove == cAbove) {
                        // c, a, b
                        GenTriangles(
                            cutting_plane,
                            aAbove ? pos : neg,
                            !aAbove ? pos : neg,
                            c, a, b, i_c, i_a, i_b,
                            rings
                        );

                    } else if (bAbove == cAbove) {
                        // b, c, a (use bAbove)
                        GenTriangles(
                            cutting_plane,
                            bAbove ? pos : neg,
                            !bAbove ? pos : neg,
                            b, c, a, i_b, i_c, i_a,
                            rings
                        );
                    }
                }
            }

            var reduced = Hierarchy.Analyse(rings.GetRings(), cutting_plane);

            // generate seperation meshing
            foreach (var ring in reduced) {
                GenerateRingMesh(ring,pos,cutting_plane.normal);
                GenerateRingMesh(ring,neg,cutting_plane.normal); 
            }

            result = new List<CutResult>();

            // create new objects
            if (param.polySeperation) {
                result.AddRange(PolySep(pos).ConvertAll(p=>new CutResult(p,target.transform)));
                result.AddRange(PolySep(neg).ConvertAll(p=>new CutResult(p,target.transform)));
            } else {
                result.Add(new CutResult(pos,target.transform));
                result.Add(new CutResult(neg,target.transform));
            }

            // destroy original object
            if (param.destroyOriginal) MonoBehaviour.Destroy(target);

            return true;
        }

    }

}