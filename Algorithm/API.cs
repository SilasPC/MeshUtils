
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MeshUtils.Util;

namespace MeshUtils {

    static class API {

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

            private float copyVelocity = 0;
            private float driftVelocity = 0;
            private bool addRenderer = false;
            private bool boxColliderFallback = false;
            private bool addCollider = false;
            private bool addRigidbody = false;
            private bool copyMaterial = false;
            private bool copyParent = false;
            private Color? addColor = null;
            
            public CutObj(MeshPart part, Transform orig, Vector3? vel, Vector3 worldNormal, Material material) {
                this.part = part;
                this.vel = vel;
                this.pos = orig.position;
                this.rot = orig.rotation;
                this.scale = orig.lossyScale;
                this.worldNormal = worldNormal.normalized;
                this.material = material;
                this.parent = orig.parent;
            }

            public Vector3 GetLocalDriftDirection() {
                return parent != null
                    ? parent.InverseTransformDirection(GetDriftDirection())
                    : GetDriftDirection();
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
                if (this.copyParent) obj.transform.SetParent(this.parent);

                return obj;

            }

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