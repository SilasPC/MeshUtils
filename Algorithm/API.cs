
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MeshUtils.Util;

namespace MeshUtils {

    static class API {

        public struct CutParams {
            public readonly bool hiearchyAnalysis;
            public readonly bool polySeperation;
            public readonly bool destroyOriginal;
            public readonly bool allowSingleResult;
            public readonly bool selfConnectRings;
            public readonly bool ignorePartialRings;
            public readonly float seperationDistance;
            public readonly float maxCutDistance;
            public readonly Vector3 originPoint;
            public readonly Vector2 innerTextureCoord;
            public CutParams (
                bool hiearchyAnalysis,
                bool polySeperation,
                bool destroyOriginal,
                bool allowSingleResult,
                bool selfConnectRings,
                bool ignorePartialRings,
                Vector3 originPoint,
                float maxCutDistance,
                float gap,
                Vector2 innerTextureCoord
            ) {
                this.hiearchyAnalysis = hiearchyAnalysis;
                this.polySeperation = polySeperation;
                this.destroyOriginal = destroyOriginal;
                this.allowSingleResult = allowSingleResult;
                this.selfConnectRings = selfConnectRings;
                this.ignorePartialRings = ignorePartialRings;
                this.originPoint = originPoint;
                this.maxCutDistance = maxCutDistance;
                this.seperationDistance = gap / 2;
                this.innerTextureCoord = innerTextureCoord;
            }
        }

        public class CutResult {
            public readonly List<CutObj> results;
            public CutResult (List<CutObj> results) {
                this.results = results;
            }
        }

        public class CutObj {

            private readonly Transform parent;
            private readonly MeshPart part;
            private readonly Vector3 pos, scale;
            private readonly Quaternion rot;
            private readonly Vector3 worldNormal;
            private readonly Vector3? vel;
            private readonly Material material;
            private readonly List<Ring> rings;
            //private readonly float? density;

            private float copyVelocity = 0;
            private float driftVelocity = 0;
            private float seperationDistance = 0;
            private bool addRenderer = false;
            private bool boxColliderFallback = false;
            private bool addCollider = false;
            private bool addRigidbody = false;
            //private bool copyDensity = false;
            private bool copyMaterial = false;
            private bool copyParent = false;
            private float ringWidth = 0;
            private Color ringColor = Color.white;
            private Color? addColor = null;
            
            public CutObj(MeshPart part, Transform orig, Vector3? vel, Vector3 worldNormal, Material material, List<Ring> rings) {
                this.part = part;
                this.vel = vel;
                this.pos = orig.position;
                this.rot = orig.rotation;
                this.scale = orig.lossyScale;
                this.worldNormal = worldNormal.normalized;
                this.material = material;
                this.parent = orig.parent;
                this.rings = rings;
                //this.density = density;
            }

            public bool IsPositive() {return part.side;}

            public Vector3 GetLocalDriftDirection() {
                return parent != null
                    ? parent.InverseTransformDirection(GetDriftDirection())
                    : GetDriftDirection();
            }

            public Vector3 GetDriftDirection() {
                return worldNormal * (part.side ? 1 : -1);
            }
            
            public CutObj UseDefaults() {
                CopyParent();
                CopyMaterial();
                WithCollider();
                WithRingWidth(0.01f);
                WithRingColor(Color.white);
                FallbackToBoxCollider();
                CopyVelocity();
                WithDriftVelocity(0.1f);
                WithSeperationDistance(0.02f);
                return this;
            }

            public CutObj WithRingWidth(float width) {
                this.ringWidth = width;
                return this;
            }

            public CutObj WithRingColor(Color col) {
                this.ringColor = col;
                return this;
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

            public CutObj CopyParent() {
                this.copyParent = true;
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

            /*public CutObj CopyDensity() {
                this.addRigidbody = true;
                this.copyDensity = true;
                return this;
            }*/

            public CutObj CopyVelocity(float factor = 1) {
                this.addRigidbody = true;
                this.copyVelocity = factor;
                return this;
            }
            public CutObj WithDriftVelocity(float vel) {
                this.addRigidbody = true;
                this.driftVelocity = vel;
                return this;
            }

            public CutObj WithSeperationDistance(float dist) {
                this.seperationDistance = dist;
                return this;
            }

            public GameObject Instantiate() {
                GameObject obj = new GameObject();
                this.part.AddMeshTo(obj);
                if (this.addRigidbody) {
                    Rigidbody rb = obj.AddComponent<Rigidbody>();
                    if (this.vel is Vector3 vel) rb.velocity = vel * copyVelocity;
                    rb.velocity += GetDriftDirection() * this.driftVelocity;
                    /*if (this.copyDensity && this.density is float density) {
                        rb.SetDensity(density);
                    }*/
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
                    if (ringWidth > 0) {
                        foreach (Ring ring in rings) {
                            GameObject lineObj = new GameObject();
                            lineObj.transform.SetParent(obj.transform);
                            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
                            lr.positionCount = ring.verts.Count;
                            lr.numCornerVertices = 2;
                            lr.SetPositions(ring.verts.ToArray());
                            lr.loop = true;
                            lr.widthMultiplier = ringWidth;
                            lr.material = new Material(Shader.Find("Sprites/Default"));
                            lr.material.color = ringColor;
                            lr.startColor = ringColor;
                            lr.endColor = ringColor;
                            lr.useWorldSpace = false;
                        }
                    }
                }

                obj.transform.position = this.pos + GetDriftDirection() * Math.Max(0f, seperationDistance / 2f);
                obj.transform.rotation = this.rot;
                SetGlobalScale(obj.transform,this.scale);
                if (this.copyParent) obj.transform.SetParent(this.parent);

                return obj;

            }

        }

        public static CutResult tmp(
            GameObject target,
            CuttingTemplate template
        ) {
            return NonPlanarAlgorithm.Run(target,template.ToLocalSpace(target.transform));
        }

        public static CutResult PerformCut(
            GameObject target,
            CuttingPlane plane,
            CutParams param
        ) {
            DateTime start = DateTime.Now;
            CutResult res;
            if (param.maxCutDistance != float.PositiveInfinity) {
                if (param.seperationDistance > 0) throw new Exception("no gap and max cut");
                res = PartialAlgorithm.Run(target,plane,param);
            }
            if (param.seperationDistance <= 0) res = BasicAlgorithm.Run(target,plane,param);
            else res = GapAlgorithm.Run(target,plane,param);
            Debug.Log((DateTime.Now-start).TotalMilliseconds+" elapsed");
            return res;
        }

    }

}