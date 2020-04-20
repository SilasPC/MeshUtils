
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static MeshUtils.Util;
using static MeshUtils.API;

namespace MeshUtils {

    public class AlgoTest : MonoBehaviour {

        public List<GameObject> objs = new List<GameObject>();

        public bool Run = false;

        void OnValidate() {
            if (Run) Test();
            Run = false;
        }

        void Test() {
            //for (int i = 0; i < 10; i++) {
                foreach (GameObject obj in objs) {
                    Debug.Log(obj);
                    DateTime start = DateTime.Now;
                    CutParams p = new CutParams(false,true,false,false,false,false,Vector3.zero,0.2f,0,Vector2.zero);
                    CuttingPlane pl = CuttingPlane.InWorldSpace(obj.transform.up,obj.transform.position);
                    if (GapAlgorithm.Run(obj,pl,p) == null) throw new Exception("test failed");
                    Debug.Log((DateTime.Now-start).TotalMilliseconds+" elapsed");
                }
            //}
        }

    }

}