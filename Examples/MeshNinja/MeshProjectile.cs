using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshProjectile : MonoBehaviour
{
    
    public void OnTriggerEnter() {
        Destroy(gameObject);
    }

}
