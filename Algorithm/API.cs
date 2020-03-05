
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
            public readonly List<Vector3> cutCenters;
            public readonly List<CutObj> results;
            public CutResult (List<Vector3> cutCenters, List<CutObj> results) {
                this.cutCenters = cutCenters;
                this.results = results;
            }
        }

        public class CutObj {

            private readonly MeshPart part;
            private readonly Vector3 pos, scale;
            private readonly Quaternion rot;
            private readonly Vector3 worldNormal;
            private readonly Vector3? vel;
            private readonly Material material;

            private float copyVelocity = 0;
            private float driftVelocity = 0;
            private bool addRenderer = false;
            private bool boxColliderFallback = false;
            private bool addCollider = false;
            private bool addRigidbody = false;
            private bool copyMaterial = false;
            private Color? addColor = null;
            
            public CutObj(MeshPart part, Transform orig, Vector3? vel, Vector3 worldNormal, Material material) {
                this.part = part;
                this.vel = vel;
                this.pos = orig.position;
                this.rot = orig.rotation;
                this.scale = orig.lossyScale;
                this.worldNormal = worldNormal.normalized;
                this.material = material;
            }

            public Vector3 GetDriftDirection() {
                return worldNormal * (part.side ? 1 : -1);
            }

            public CutObj WithRenderer() {
                this.addRenderer = true;
                return this;
            }

            public CutObj WithColor(Color col) {
                this.copyMaterial = false;
                this.addColor = col;
                this.addRenderer = true;
                return this;
            }
            
            public CutObj FallbackToColor(Color col) {
                this.addRenderer = true;
                this.addColor = col;
                return this;
            }

            public CutObj CopyMaterial() {
                this.addColor = null;
                this.addRenderer = true;
                this.copyMaterial = true;
                return this;
            }

            public CutObj WithCollider() {
                this.addCollider = true;
                return this;
            }

            public CutObj FallbackToBoxCollider() {
                this.boxColliderFallback = true;
                return this;
            }

            public CutObj WithRigidbody() {
                this.addRigidbody = true;
                return this;
            }

            public CutObj CopyVelocity(float factor) {
                this.addRigidbody = true;
                this.copyVelocity = factor;
                return this;
            }
            public CutObj WithDriftVelocity(float vel) {
                this.addRigidbody = true;
                this.driftVelocity = vel;
                return this;
            }

            public GameObject Create() {
                GameObject obj = new GameObject();
                this.part.AddMeshTo(obj);
                if (this.addRigidbody) {
                    Rigidbody rb = obj.AddComponent<Rigidbody>();
                    if (this.vel is Vector3 vel) rb.velocity = vel * copyVelocity;
                    rb.velocity += GetDriftDirection() * this.driftVelocity;
                }
                if (this.addCollider) {
                    MeshCollider mc = null;
                    try {
                        mc = obj.AddComponent<MeshCollider>();
                        mc.convex = true;
                    } catch (System.Exception e) {
                        if (boxColliderFallback) {
                            if (mc != null) MonoBehaviour.Destroy(mc);
                            obj.AddComponent<BoxCollider>();
                        } else throw e;
                    }
                }
                    
                if (this.addRenderer) {
                    MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
                    if (this.copyMaterial && this.material != null)
                        renderer.material = this.material;
                    else if (this.addColor is Color color)
                        renderer.material.color = color;
                }

                obj.transform.position = this.pos;
                obj.transform.rotation = this.rot;
                SetGlobalScale(obj.transform,this.scale);

                return obj;

            }

        }

        public static CutResult PerformCut(
            GameObject target,
            CuttingPlane plane,
            CutParams param
        ) {

            CuttingPlane cutting_plane = plane.ToLocalSpace(target.transform);

            Mesh mesh = target.GetComponent<MeshFilter>().mesh;
            MeshPart pos = new MeshPart(true), neg = new MeshPart(false);
            RingGenerator rings = new RingGenerator();

            Vector2[] uvs = mesh.uv;

            if (uvs.Length > 0 && uvs.Length != mesh.vertices.Length)
                throw OperationException.Internal("UV/Vertex length mismatch");

            // divide mesh in half by vertices
            int i = 0;
            foreach (Vector3 vertex in mesh.vertices) {
                if (cutting_plane.IsAbove(vertex)) {
                    pos.indexMap.Add(i,pos.vertices.Count);
                    pos.vertices.Add(vertex);
                    if (uvs.Length > 0) pos.uvs.Add(uvs[i]);
                } else {
                    neg.indexMap.Add(i,neg.vertices.Count);
                    neg.vertices.Add(vertex);
                    if (uvs.Length > 0) neg.uvs.Add(uvs[i]);
                }
                i++;
            }

            bool addUVs = uvs.Length > 0;

            // if either vertex list is empty the knife plane didn't collide
            if (pos.vertices.Count == 0 || neg.vertices.Count == 0)
                return null;

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
                cutObjs.AddRange(PolySep(pos).ConvertAll(p=>new CutObj(p,target.transform,vel,worldNormal,mat)));
                cutObjs.AddRange(PolySep(neg).ConvertAll(p=>new CutObj(p,target.transform,vel,worldNormal,mat)));
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

    }

}