
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static MeshUtils.API;

namespace MeshUtils {

	public class KnifeExample : MonoBehaviour {

		public void OnCollisionEnter(Collision col) {

			if (col.gameObject.tag != "Cuttable") return;

			CuttingPlane plane = CuttingPlane
				.InLocalSpace(Vector3.up,Vector3.zero,transform)
				.ToLocalSpace(col.transform);
			CutParams param = new CutParams(true, true);

			List<CutResult> result;
			if (PerformCut(col.gameObject,plane,param,out result)) {
				Debug.Log("success");
				foreach (CutResult res in result)
					res
						.WithColor(new Color(0,0.7f,0.3f))
						.WithCollider()
						.WithRigidbody()
						.Create();
			} else Debug.Log("failure");

		}

	}

}
