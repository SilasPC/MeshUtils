
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshUtils {

    public class Chopable : MonoBehaviour {

        [Tooltip("For a less well-formed mesh, this will attempt to merge points in intersection that are almost exactly identical.")]
        public bool tryApproximation = false;
        
        [Tooltip("Attempt to separate disconnected parts of resulting meshes. Note: not terribly fast for big objects.")]
        public bool polySeparate = false;

        [Tooltip("Number of consecutive chops allowed. Zero for unlimited.")]
        public uint maxCutCount = 1;

        public bool CopyTo(GameObject obj) {
            if (maxCutCount == 1) return false;
            Chopable c = obj.AddComponent<Chopable>();
            c.tryApproximation = tryApproximation;
            c.polySeparate = polySeparate;
            c.maxCutCount = maxCutCount == 0 ? 0 : maxCutCount - 1;
            return true;
        }
        
    }

}