
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshUtils {

    public class TreeChopablePart : MonoBehaviour {

        public GameObject abovePart;
        public void CopyTo(GameObject obj) {
            TreeChopablePart c = obj.AddComponent<TreeChopablePart>();
            c.abovePart = abovePart;
            Outline oc = obj.AddComponent<Outline>();
        }
    }

}