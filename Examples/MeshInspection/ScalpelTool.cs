using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static MeshUtils.API;
using MeshUtils;

public class ScalpelTool : MonoBehaviour
{
    
    void OnTriggerEnter(Collider col) {
        Inspectable i;
        if (!col.gameObject.TryGetComponent<Inspectable>(out i)) return;
        CuttingPlane plane = CuttingPlane.InLocalSpace(
            UnityEngine.Random.insideUnitSphere.normalized,
            Vector3.zero,
            transform
        );
        i.Split(col.gameObject,plane);
    }

}
