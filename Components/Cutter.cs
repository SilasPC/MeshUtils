
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MeshUtils;
using static MeshUtils.API;
using static MeshUtils.VectorUtil;

namespace MeshUtils {

    public class Cutter : MonoBehaviour {

        [Tooltip("Direction from base of edge to tip of edge.")]
        public Vector3 EdgeDirection = Vector3.up;
        [Tooltip("Direction edge should cut into objects.")]
        public Vector3 CutDirection = Vector3.forward;

        [Tooltip("Omnidirectional cutting. Think lightsabers. Cutting direction depends on collision direction.")]
        public bool _OmnidirectionalMode = false;

        [MyBox.ConditionalField("_OmnidirectionalMode",true)]
        [Range(0,180)]
        [Tooltip("Maximum angle (in degrees) from cutting direction to tolerate.")]
        public float MaxAngle = 20;
        [MyBox.PositiveValueOnly]
        [Tooltip("Minimum relative velocity required to attempt cut.")]
        public float MinimumVelocity = 2;
        [Tooltip("If true, direction vectors are interpreted as normals. This means the directions will be squezed along with the transform.")]
        public bool directionsAreNormals = false;
        [Tooltip("If true, uses first contact point as basis for cutting plane. Otherwise uses object center.")]
        public bool UseContactPoint = true;
        
        public void OnCollisionEnter(Collision col) {

            Cuttable cuttable;
            if (!col.gameObject.TryGetComponent<Cuttable>(out cuttable)) return;

            Vector3 cutDir = directionsAreNormals
                ? TransformNormal(CutDirection,transform)
                : transform.TransformDirection(CutDirection);

            float relVel = Vector3.Project(col.relativeVelocity,cutDir).magnitude;

            if (MinimumVelocity > relVel) return;

            Vector3 dir = _OmnidirectionalMode
                ? -col.relativeVelocity
                : cutDir;

            Vector3 edge = directionsAreNormals
                ? TransformNormal(EdgeDirection,transform)
                : transform.TransformDirection(EdgeDirection);

            Vector3 angleProjection = Vector3.ProjectOnPlane(-col.relativeVelocity,edge);

            if (Vector3.Angle(angleProjection,cutDir) > MaxAngle) return;

            Vector3 normal = Vector3.Cross(dir,edge).normalized;

            Vector3 pointInPlane = UseContactPoint
                ? col.GetContact(0).point
                : transform.position;

            CuttingPlane plane = CuttingPlane.InWorldSpace(normal,pointInPlane);
            CutParams param = new CutParams(
                cuttable.CheckForHoles,
                cuttable.PolySeperate,
                true,
                true,
                cuttable.CloseOpenSurfaces,
                cuttable.AllowOpenSurfaces,
                Vector3.zero, float.PositiveInfinity, 0,
                cuttable.InnerTextureCoordinate
            );

            CutResult result = PerformCut(col.gameObject,plane,param);
            if (result != null) {
                foreach (CutObj res in result.results) {
                    res
                        .UseDefaults()
                        .WithDriftVelocity(cuttable.DriftVelocity)
                        .WithRingWidth(cuttable.HighlightWidth)
                        .WithRingColor(cuttable.HighLightColor)
                        .WithColor(new Color(1,0.1f,0.1f))
                        .Instantiate();
                }
            }

        }

    }

}