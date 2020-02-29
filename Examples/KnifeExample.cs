
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static MeshUtils.API;
using MeshUtils;

public class KnifeExample : MonoBehaviour {

	[Tooltip("A fadeable material.")]
	public Material fadeMaterial;

	[Tooltip("A particle system prefab to spawn at cuts")]
	public GameObject particlePrefab;

	public Vector3 edgeDirection = Vector3.up, cutDirection = Vector3.forward;

	[Tooltip("Maximum angle (in degrees) from cutting direction to tolerate")]
	public float maxAngle = 20;

	[Tooltip("Minimum relative velocity required to attempt cut")]
	public float minimumVelocity = 2;

	[Tooltip("If true, the cutting direction is aligned to the relative velocity between objects.\nPrimarily useful for omnidirectional cutting with maxAngle >= 180")]
	public bool alignToVelocity = false;

	[Tooltip("If true, direction vectors are interpreted as normals. This means the directions will be squezed along with the transform.")]
	public bool directionsAreNormals = false;

	[Tooltip("If true, uses first contact point as basis for cutting plane. Otherwise uses knife center.")]
	public bool useContactPoint = true;

	public void OnCollisionEnter(Collision col) {

		if (col.gameObject.tag != "Cuttable") return;
		if (minimumVelocity > col.relativeVelocity.magnitude) return;

		Vector3 cutDir = directionsAreNormals
			? Util.TransformNormal(cutDirection,transform)
			: transform.TransformDirection(cutDirection);

		if (Vector3.Angle(-col.relativeVelocity,cutDir) > maxAngle) return;

		Vector3 dir = alignToVelocity
			? -col.relativeVelocity
			: cutDir;

		Vector3 edge = directionsAreNormals
			? Util.TransformNormal(edgeDirection,transform)
			: transform.TransformDirection(edgeDirection);
		
		Vector3 normal = Vector3.Cross(dir,edge).normalized;

		Vector3 pointInPlane = useContactPoint
			? col.GetContact(0).point
			: transform.position;

		CuttingPlane plane = CuttingPlane
			.InWorldSpace(normal,pointInPlane);
		CutParams param = new CutParams(true, true);

		CutResult result = PerformCut(col.gameObject,plane,param);

		if (result != null) {
			Debug.Log("success");
			
			if (particlePrefab) {
				foreach (Vector3 pos in result.cutCenters) {
					GameObject part = Instantiate(particlePrefab);
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
					.WithColor(new Color(0,0.7f,0.3f))
					.WithCollider()
					.CopyVelocity()
					.Create();
				if (fadeMaterial != null) {
					obj.GetComponent<Rigidbody>().useGravity = false;
					obj.AddComponent<FadeAndDestroy>();
					var mat = obj.GetComponent<MeshRenderer>().material = fadeMaterial;
					mat.color = new Color(0,0.7f,0.3f);
					Physics.IgnoreCollision(obj.GetComponent<Collider>(),GetComponent<Collider>());
				}
			}
		} else Debug.Log("failure");

	}

}
