using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static MeshUtils.API;
using MeshUtils;

public class BulletExample : MonoBehaviour
{
    [Tooltip("Velocity resulting objects should drift apart with.")]
    public float SplitVelocity = 1;

    [Tooltip("Split at collision. Otherwise splits at object center.")]
    public bool UseContactPoint = false;

    public void OnCollisionEnter(Collision col) {

		if (col.gameObject.tag != "Cuttable") {
            Destroy(gameObject);
            return;
        }

		CuttingPlane plane = CuttingPlane.InLocalSpace(UnityEngine.Random.insideUnitSphere.normalized,Vector3.zero,col.transform);
		CutParams param = new CutParams(true, true);

		CutResult result = PerformCut(col.gameObject,plane,param);

		if (result != null) {
			foreach (CutObj res in result.results) {
            GameObject obj = res
                .CopyMaterial()
                .WithDriftVelocity(SplitVelocity)
                .Create();
		    plane = CuttingPlane
			    .InLocalSpace(UnityEngine.Random.insideUnitSphere.normalized,Vector3.zero,obj.transform);
                CutResult result2 = PerformCut(obj,plane,param);
                if (result != null)
                foreach (CutObj res2 in result2.results)
                    res2
                        .CopyMaterial()
                        .FallbackToColor(new Color(0,0.7f,0.3f))
                        .WithCollider()
                        .FallbackToBoxCollider()
                        .CopyVelocity()
                        .WithDriftVelocity(SplitVelocity)
                        .Create();
			}
		}

        Destroy(gameObject);

	}

}
