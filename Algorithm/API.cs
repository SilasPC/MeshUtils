
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MeshUtils.Util;

namespace MeshUtils {

    static class API {

        public class CutParamsBuilder {

            private bool hiearchyAnalysis = false;
            
            private Vector2? innerTextureCoord = null;

            public CutParamsBuilder UseConcaveChecks() {
                hiearchyAnalysis = true;
                return this;
            }

            public CutParamsBuilder WithInnerTexture(Vector2 uv) {
                innerTextureCoord = uv;
            }

            public static implicit operator CutParams(CutParamsBuilder builder) => new CutParams(
                hiearchyAnalysis,
                false,
                false,
                false,
                Vector3.zero,
                float.PositiveInfinity,
                0,
                innerTextureCoord
            );

        }

        public struct CutParams {
            public readonly bool hiearchyAnalysis;
            public readonly bool allowSingleResult;
            public readonly bool selfConnectRings;
            public readonly bool ignorePartialRings;
            public readonly float seperationDistance;
            public readonly float maxCutDistance;
            public readonly Vector3 originPoint;
            public readonly Vector2? innerTextureCoord;
            public CutParams (
                bool hiearchyAnalysis,
                bool allowSingleResult,
                bool selfConnectRings,
                bool ignorePartialRings,
                Vector3 originPoint,
                float maxCutDistance,
                float gap,
                Vector2? innerTextureCoord
            ) {
                this.hiearchyAnalysis = hiearchyAnalysis;
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
            
            private readonly GameObject originalObject;
            
            public Vector3 Position { get; private set; }
            public Vector3 Scale { get; private set; }
            public Quaternion Rotation { get; private set; }
            public Vector3? Velocity { get; private set; }
            public Material Material { get; private set; }
            public float? Density { get; private set; }

            public readonly Transform parentTransform;
            public readonly Vector3 worldNormal;
            public readonly IReadOnlyCollection<Ring> rings;

            public List<CutObj> PositiveResults { get {
                return new List<CutObj>(_results.Where(r=>r.IsPositive()));
            }}
            public List<CutObj> NegativeResults { get {
                return new List<CutObj>(_results.Where(r=>r.IsNegative()));
            }}
            
            private readonly List<CutObj> _results;
            public IReadOnlyCollection<CutObj> Results { get => _results; }
            public CutResult (
                GameObject original,
                List<MeshPart> results,
                Vector3 worldNormal,
                List<Ring> rings,
                bool isPolySeperated
            ) {
                _results = results.ConvertAll(p=>new CutObj(p,this,isPolySeperated));
                originalObject = original;
                this.worldNormal = worldNormal;
                this.rings = rings;
                parentTransform = original.transform.parent;
                UpdateMeta();
            }

            public CutResult (
                GameObject original,
                Vector3 worldNormal,
                List<Ring> rings,
                params MeshPart[] results
            ) {
                _results = new List<CutObj>(from result in results select new CutObj(result,this,false));
                originalObject = original;
                this.worldNormal = worldNormal;
                this.rings = rings;
                parentTransform = original.transform.parent;
                UpdateMeta();
            }

            public CutResult UpdateMeta() {

                Position = originalObject.transform.position;
                Scale = originalObject.transform.lossyScale;
                Rotation = originalObject.transform.rotation;
                
                Rigidbody rb;
                if (originalObject.TryGetComponent<Rigidbody>(out rb)) {
                    Velocity = rb.velocity;
                    float oldMass = rb.mass;
                    rb.SetDensity(1);
                    Density = oldMass / rb.mass;
                    rb.mass = oldMass;
                } else {
                    Velocity = null;
                    Density = null;
                }

                Renderer renderer;
                if (originalObject.TryGetComponent<Renderer>(out renderer)) 
                    Material = renderer.material;
                else
                    Material = null;

                return this;

            }

            public bool PolySeperate() {
                bool success = false;
                success |= PolySeperatePositive();
                success |= PolySeperateNegative();
                return success;
            }
            public bool PolySeperatePositive() {
                bool success = false;
                PositiveResults.ForEach(c => success |= c.PolySeperate());
                return success;
            }
            public bool PolySeperateNegative() {
                bool success = false;
                NegativeResults.ForEach(c => success |= c.PolySeperate());
                return success;
            }

            public void DestroyObject() => MonoBehaviour.Destroy(originalObject);

            public List<T> ConvertAll<T>(Converter<CutObj,T> f) => _results.ConvertAll(f);

            public abstract class FriendlyCutObj {

                protected readonly CutResult cutResult;
                protected readonly MeshPart part;

                private bool isPolySeperated;

                public FriendlyCutObj(MeshPart part, CutResult result, bool isPolySeperated) {
                    this.part = part;
                    this.cutResult = result;
                    this.isPolySeperated = isPolySeperated;
                }

                public bool PolySeperate() {
                    if (isPolySeperated) return false;
                    List<MeshPart> newParts = part.PolySeperate();
                    if (newParts.Count > 1) {
                        newParts.ForEach(p => cutResult._results.Add(new CutObj(p, cutResult, true)));
                        cutResult._results.Remove((CutObj)this);
                        return true;
                    }
                    return false;
                }

            }

        }

        public class CutObj : CutResult.FriendlyCutObj {

            private float copyVelocity = 0;
            private float driftVelocity = 0;
            private float seperationDistance = 0;
            private bool addRenderer = false;
            private bool boxColliderFallback = false;
            private bool addCollider = false;
            private bool setConvex = true;
            private bool addRigidbody = false;
            private bool setKinematic = false;
            private bool copyDensity = false;
            private bool copyMaterial = false;
            private bool copyParent = false;
            private float ringWidth = 0;
            private Color ringColor = Color.white;
            private Color? addColor = null;
            
            public CutObj(MeshPart part, CutResult res, bool isPolySeperated) : base(part, res, isPolySeperated) {}

            public bool IsPositive() => part.side;
            public bool IsNegative() => !part.side;

            public Vector3 GetLocalDriftDirection() {
                return cutResult.parentTransform != null
                    ? cutResult.parentTransform.InverseTransformDirection(GetDriftDirection())
                    : GetDriftDirection();
            }

            public Vector3 GetDriftDirection() => cutResult.worldNormal * (part.side ? 1 : -1);
            
            public CutObj UseDefaults() {
                CopyParent();
                CopyMaterial();
                WithCollider();
                WithRingWidth(0.01f);
                WithRingColor(Color.white);
                FallbackToBoxCollider();
                CopyVelocity();
                CopyDensity();
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

            public CutObj WithCollider(bool concave = false) {
                this.addCollider = true;
                this.setConvex = !concave;
                if (concave) this.setKinematic = true;
                return this;
            }

            public CutObj FallbackToBoxCollider() {
                this.boxColliderFallback = true;
                return this;
            }

            public CutObj WithRigidbody(bool kinematic = false) {
                this.addRigidbody = true;
                if (!this.setConvex) {
                    this.setKinematic = true;
                } else this.setKinematic = kinematic;
                return this;
            }

            public CutObj CopyDensity() {
                this.addRigidbody = true;
                this.copyDensity = true;
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

            public CutObj WithSeperationDistance(float dist) {
                this.seperationDistance = dist;
                return this;
            }

            public GameObject Instantiate() {
                GameObject obj = new GameObject();
                part.AddMeshTo(obj);

                obj.transform.position = cutResult.Position + GetDriftDirection() * Math.Max(0f, seperationDistance / 2f);
                obj.transform.rotation = cutResult.Rotation;
                SetGlobalScale(obj.transform,cutResult.Scale);
                if (copyParent) obj.transform.SetParent(cutResult.parentTransform);

                if (addCollider) {
                    MeshCollider mc = null;
                    try {
                        mc = obj.AddComponent<MeshCollider>();
                        if (setConvex) mc.convex = true;
                    } catch (System.Exception e) {
                        if (boxColliderFallback) {
                            if (mc != null) MonoBehaviour.Destroy(mc);
                            obj.AddComponent<BoxCollider>();
                        } else throw e;
                    }
                }
                if (addRigidbody) {
                    Rigidbody rb = obj.AddComponent<Rigidbody>();
                    if (setKinematic) rb.isKinematic = true;
                    if (cutResult.Velocity is Vector3 vel) rb.velocity = vel * copyVelocity;
                    rb.velocity += GetDriftDirection() * driftVelocity;
                    if (copyDensity && cutResult.Density is float density) {
                        rb.SetDensity(density);
                        rb.mass = rb.mass; // update mass in component view
                    }
                }
                if (addRenderer) {
                    MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
                    if (copyMaterial && cutResult.Material is Material mat)
                        renderer.material = mat;
                    else if (addColor is Color color)
                        renderer.material.color = color;
                    if (ringWidth > 0) {
                        foreach (Ring ring in cutResult.rings) {
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

                return obj;

            }

        }

        public static CutResult tmp(
            GameObject target,
            CuttingTemplate template
        ) {
            DateTime start = DateTime.Now;
            var res = NonPlanarAlgorithm.Run(target,template.ToLocalSpace(target.transform));
            Debug.Log((DateTime.Now-start).TotalMilliseconds+" elapsed");
            return res;
        }

        public static CutResult PerformCut(
            GameObject target,
            CuttingPlane plane,
            CutParams param
        ) {
            //DateTime start = DateTime.Now;
            CutResult res;
            if (param.maxCutDistance != float.PositiveInfinity) {
                if (param.seperationDistance > 0) throw new Exception("no gap and max cut");
                res = PartialAlgorithm.Run(target,plane,param);
            }
            else if (param.seperationDistance <= 0) res = BasicAlgorithm.Run(target,plane,param);
            else res = GapAlgorithm.Run(target,plane,param);
            //Debug.Log((DateTime.Now-start).TotalMilliseconds+" elapsed");
            return res;
        }

    }

}