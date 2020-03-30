
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MeshUtils.Util;

namespace MeshUtils {

    static class API {

        /*public class CutParamsBuilder {
            private bool polySeperation = false;
            private bool destroyOriginal = false;
            private bool allowSingleResult = false;
            private float seperationDistance;
            private float maxCutDistance;
            private Vector3 originPoint;
            public CutParamsBuilder WithPolySeperation() {
                this.polySeperation = true;
                return this;
            }
            public CutParamsBuilder DoDestroyOriginal() {
                this.destroyOriginal = true;
                return this;
            }
            public CutParamsBuilder AllowSingleResult() {
                this.allowSingleResult = true;
            }
        }*/

        public struct CutParams {
            public readonly bool polySeperation;
            public readonly bool destroyOriginal;
            public readonly bool allowSingleResult;
            public readonly float seperationDistance;
            public readonly float maxCutDistance;
            public readonly Vector3 originPoint;
            public CutParams (
                bool polySeperation,
                bool destroyOriginal,
                bool allowSingleResult,
                Vector3 originPoint,
                float maxCutDistance,
                float gap
            ) {
                this.polySeperation = polySeperation;
                this.destroyOriginal = destroyOriginal;
                this.allowSingleResult = allowSingleResult;
                this.originPoint = originPoint;
                this.maxCutDistance = maxCutDistance;
                this.seperationDistance = gap / 2;
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

            private readonly Transform parent;
            private readonly MeshPart part;
            private readonly Vector3 pos, scale;
            private readonly Quaternion rot;
            private readonly Vector3 worldNormal;
            private readonly Vector3? vel;
            private readonly Material material;
            private readonly List<Ring> rings;

            private float copyVelocity = 0;
            private float driftVelocity = 0;
            private bool addRenderer = false;
            private bool boxColliderFallback = false;
            private bool addCollider = false;
            private bool addRigidbody = false;
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
            }

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
                WithRingWidth(0.02f);
                WithRingColor(Color.red);
                FallbackToBoxCollider();
                CopyVelocity();
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

            public GameObject Instantiate() {
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
                    if (ringWidth > 0) {
                        foreach (Ring ring in rings) {
                            GameObject lineObj = new GameObject();
                            lineObj.transform.SetParent(obj.transform);
                            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
                            lr.positionCount = ring.verts.Count;
                            lr.SetPositions(ring.verts.ToArray());
                            lr.loop = true;
                            lr.widthMultiplier = ringWidth;
                            // lr.Simplify(0.1f);
                            lr.useWorldSpace = false;
                        }
                    }
                }

                obj.transform.position = this.pos;
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
            return Algorithms.NonPlanarCut(target,template.ToLocalSpace(target.transform));
        }

        public static CutResult PerformCut(
            GameObject target,
            CuttingPlane plane,
            CutParams param
        ) {
            if (param.maxCutDistance != float.PositiveInfinity) {
                if (param.seperationDistance > 0) throw new Exception("no gap and max cut");
                return Algorithms.PartialPlanarCut(target,plane,param);
            }
            if (param.seperationDistance <= 0) return Algorithms.PlanarCutWithoutGap(target,plane,param);
            return Algorithms.PlanarCutWithGap(target,plane,param);
        }

    }

}