
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static MeshUtils.API;
using MeshUtils;

public class KnifeExample : MonoBehaviour {

	[Tooltip("A fadeable material, if fading out cut results is desired.")]
	public Material FadeMaterial;

	[Tooltip("The speed with which to fade cut results, if fade material is given")]
	public int FadeSpeed = 2;

	[Tooltip("A particle system prefab to spawn at cuts.")]
	public GameObject ParticlePrefab;

	public Vector3 EdgeDirection = Vector3.up, CutDirection = Vector3.forward;

	[Tooltip("Maximum angle (in degrees) from cutting direction to tolerate.")]
	public float MaxAngle = 20;

	[Tooltip("Minimum relative velocity required to attempt cut.")]
	public float MinimumVelocity = 2;

	[Tooltip("If true, the cutting direction is aligned to the relative velocity between objects.\nPrimarily useful for omnidirectional cutting with maxAngle >= 180.")]
	public bool AlignToVelocity = false;

	[Tooltip("Minimum velocity is evaluted after projection in cut direction")]
	public bool ProjectMinimumVelocity = true;

	[Tooltip("If true, direction vectors are interpreted as normals. This means the directions will be squezed along with the transform.")]
	public bool directionsAreNormals = false;

	[Tooltip("If true, uses first contact point as basis for cutting plane. Otherwise uses knife center.")]
	public bool UseContactPoint = true;

	[Tooltip("Further seperate disconnected parts of resulting meshes.")]
	public bool PolySeperation = true;

	[Tooltip("Distance between to newly generated surfaces")]
	public float Gap;

	public void PassCollision(Collision col) {
		if (col.GetContact(0).thisCollider == GetComponent<Collider>())
			OnCollisionEnter(col);
	}

	public void OnCollisionEnter(Collision col) {

		if (col.gameObject.tag != "Cuttable") return;

		Vector3 cutDir = directionsAreNormals
			? Util.TransformNormal(CutDirection,transform)
			: transform.TransformDirection(CutDirection);

		float relVel = ProjectMinimumVelocity
			? Vector3.Project(col.relativeVelocity,cutDir).magnitude
			: col.relativeVelocity.magnitude;

		if (MinimumVelocity > relVel) return;

		Vector3 dir = AlignToVelocity
			? -col.relativeVelocity
			: cutDir;

		Vector3 edge = directionsAreNormals
			? Util.TransformNormal(EdgeDirection,transform)
			: transform.TransformDirection(EdgeDirection);

		Vector3 angleProjection = Vector3.ProjectOnPlane(-col.relativeVelocity,edge);

		if (Vector3.Angle(angleProjection,cutDir) > MaxAngle) return;

		Vector3 normal = Vector3.Cross(dir,edge).normalized;

		Vector3 pointInPlane = UseContactPoint
			? col.GetContact(0).point
			: transform.position;

		CuttingPlane plane = CuttingPlane.InWorldSpace(normal,pointInPlane);
		CutParams param = new CutParams(PolySeperation, true, Gap);

		CutResult result = PerformCut(col.gameObject,plane,param);

		if (result != null) {
			if (ParticlePrefab) {
				foreach (Vector3 pos in result.cutCenters) {
					GameObject part = Instantiate(ParticlePrefab);
					part.transform.SetPositionAndRotation(
						pos,
						Quaternion.FromToRotation(
							Vector3.up,
							normal
						)
					);
				}
			}

			foreach (CutObj res in result.results) {
				GameObject obj = res
					.CopyParent()
					.CopyMaterial()
					.FallbackToColor(new Color(0,0.7f,0.3f))
					.WithCollider()
					.FallbackToBoxCollider()
					.CopyVelocity(FadeMaterial == null ? 1 : 0.1f)
					.WithDriftVelocity(0.1f)
					.Create();
				if (FadeMaterial != null && FadeSpeed > 0) {
					obj.GetComponent<Rigidbody>().useGravity = false;
					Destroy(obj.GetComponent<Collider>());
					StartCoroutine(FadeRoutine(obj));
					var oldMat = obj.GetComponent<MeshRenderer>().material;
					var mat = obj.GetComponent<MeshRenderer>().material = FadeMaterial;
					if (oldMat) mat.color = oldMat.color;
					else mat.color = new Color(0,0.7f,0.3f);
					Physics.IgnoreCollision(obj.GetComponent<Collider>(),GetComponent<Collider>());
				}
			}
		} else Debug.Log("failure");

	}

	private IEnumerator FadeRoutine(GameObject obj) {
		int alpha = 255;
		while (alpha > 0) {
       		var mat = obj.GetComponent<MeshRenderer>().material;
			Color col = mat.color;
			col.a = (float) alpha / 255f;
			mat.color = col;
			alpha -= FadeSpeed;
			yield return null;
		}
		Destroy(obj);
		yield break;
	}

}
