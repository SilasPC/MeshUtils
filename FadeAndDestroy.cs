using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshUtils {
    public class FadeAndDestroy : MonoBehaviour
    {

        int alpha = 255;

        void Update() {
            var mat = GetComponent<MeshRenderer>().material;
            Color col = mat.color;
            col.a = (float) alpha / 255f;
            mat.color = col;
            if (alpha-- <= 0) {
                Destroy(transform.gameObject);
            }
        }
    }
    
}
