
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MeshUtils;
using static MeshUtils.API;
using static MeshUtils.VectorUtil;

namespace MeshUtils {

    public class Cutter : MonoBehaviour {

        HashSet<Collider> ignoreColliders = new HashSet<Collider>();

        [Tooltip("Direction from base of edge to tip of edge.")]
        public Vector3 edgeDirection = Vector3.up;
        [Tooltip("Direction edge should cut into objects.")]
        public Vector3 cutDirection = Vector3.forward;

        [Tooltip("Omnidirectional cutting. Think lightsabers. Cutting direction depends on collision direction.")]
        public bool omnidirectionalMode = false;

        [Range(0,180)]
        [Tooltip("Maximum angle (in degrees) from cutting direction to tolerate.")]
        public float maxAngle = 20;
        [Tooltip("Minimum relative velocity required to attempt cut.")]
        public float minimumVelocity = 2;
        [Tooltip("If true, direction vectors are interpreted as normals. This means the directions will be squezed along with the transform.")]
        public bool directionsAreNormals = false;
        [Tooltip("If true, uses first contact point as basis for cutting plane. Otherwise uses object center.")]
        public bool useContactPoint = true;

        [Tooltip("Velocity resulting objects should drift apart with.")]
        public float driftVelocity = 0;

        [Tooltip("Distance to seperate resulting objects after cut")]
        public float seperationDistance = 0.02f;

        public void OnCollisionEnter(Collision col) {

            if (ignoreColliders.Contains(col.collider)) {
                ignoreColliders.Remove(col.collider);
                return;
            }

            Cuttable cuttable;
            if (!col.gameObject.TryGetComponent<Cuttable>(out cuttable)) return;

            Vector3 cutDir = directionsAreNormals
                ? TransformNormal(cutDirection,transform)
                : transform.TransformDirection(cutDirection);

            float relVel = (col.relativeVelocity-Vector3.Project(col.relativeVelocity,cutDir)).magnitude;

            // Debug.Log("vel: "+relVel);
            // Debug.DrawRay(col.GetContact(0).point,col.relativeVelocity-Vector3.Project(col.relativeVelocity,cutDir)/relVel,Color.blue,1);

            if (minimumVelocity > relVel) return;

            Vector3 dir = omnidirectionalMode
                ? -col.relativeVelocity
                : cutDir;

            Vector3 edge = directionsAreNormals
                ? TransformNormal(edgeDirection,transform)
                : transform.TransformDirection(edgeDirection);

            Vector3 angleProjection = Vector3.ProjectOnPlane(gameObject.GetComponentInParent<Rigidbody>().velocity,edge);

            // Debug.Log("angle: "+Vector3.Angle(angleProjection,cutDir));

            // if (Vector3.Angle(angleProjection,cutDir) > 70) Debug.Break();

            // Debug.DrawRay(col.GetContact(0).point,angleProjection,Color.red,1);
            // Debug.DrawRay(col.GetContact(0).point,cutDir,Color.green,1);
            // Debug.DrawRay(col.GetContact(0).point,-col.relativeVelocity,Color.blue,1);

            if (Vector3.Angle(angleProjection,cutDir) > maxAngle) return;

            Vector3 normal = Vector3.Cross(dir,edge).normalized;

            Vector3 pointInPlane = useContactPoint
                ? col.GetContact(0).point
                : transform.position;

            CuttingPlane plane = CuttingPlane.InWorldSpace(normal,pointInPlane);
            CutParams param = new CutParams(
                cuttable.checkForHoles,
                cuttable.polySeperate,
                true,
                true,
                cuttable.closeOpenSurfaces,
                cuttable.allowOpenSurfaces,
                Vector3.zero, float.PositiveInfinity, 0,
                cuttable.innerTextureCoordinate
            );

            CutResult result = PerformCut(col.gameObject,plane,param);
            if (result != null) {
                foreach (CutObj res in result.results) {
                    GameObject resObj = res
                        .UseDefaults()
                        .WithDriftVelocity(driftVelocity)
                        .WithSeperationDistance(seperationDistance)
                        .WithRingWidth(cuttable.highlightWidth)
                        .WithRingColor(cuttable.highLightColor)
                        .WithColor(new Color(1,0.1f,0.1f))
                        .Instantiate();
                    cuttable.CopyTo(resObj);
                    ignoreColliders.Add(resObj.GetComponent<Collider>());
                }
            }

        }

    }

}