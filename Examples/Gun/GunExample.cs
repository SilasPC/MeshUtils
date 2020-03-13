using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static MeshUtils.API;
using MeshUtils;

public class GunExample : MonoBehaviour
{
    [Tooltip("Velocity resulting objects should drift apart with.")]
    public float SplitVelocity = 1;

    [Tooltip("Velocity added in direction of hit.")]
    public float HitVelocity = 5;

    void Update() {
        if (Random.value > 0.9) Shoot();
    }

    public void Shoot() {
        RaycastHit hit;
        if (Physics.Raycast(transform.position,transform.up,out hit)) {
            Break(hit.transform.gameObject,transform.up);
        }
    }

    public void Break(GameObject obj, Vector3 hitDir) {

		if (obj.tag != "Shootable") return;

		CuttingPlane plane = CuttingPlane.InLocalSpace(UnityEngine.Random.insideUnitSphere.normalized,Vector3.zero,obj.transform);
		CutParams param = new CutParams(false, true, true, Vector3.zero, float.PositiveInfinity, 0), param2 = new CutParams(true, true, true, Vector3.zero, float.PositiveInfinity, 0);

		CutResult result = PerformCut(obj,plane,param);

		if (result != null) {
			foreach (CutObj res in result.results) {
            GameObject resObj = res
                .CopyMaterial()
                .CopyVelocity(1)
                .CopyParent()
                .WithDriftVelocity(SplitVelocity)
                .Create();
		    plane = CuttingPlane
			    .InLocalSpace(UnityEngine.Random.insideUnitSphere.normalized,Vector3.zero,resObj.transform);
                CutResult result2 = PerformCut(resObj,plane,param2);
                if (result != null)
                foreach (CutObj res2 in result2.results)
                    res2
                        .CopyParent()
                        .CopyMaterial()
                        .FallbackToColor(new Color(0,0.7f,0.3f))
                        .WithCollider()
                        .FallbackToBoxCollider()
                        .CopyVelocity(1)
                        .WithDriftVelocity(SplitVelocity)
                        .Create()
                        .GetComponent<Rigidbody>().velocity += hitDir * HitVelocity;
			}
		}

	}

}
