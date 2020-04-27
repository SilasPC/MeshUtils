using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassCollisions : MonoBehaviour
{

    public GameObject[] objects;

    public void OnCollisionEnter(Collision col) {
        Collider thisCollider = col.GetContact(0).thisCollider;
        foreach (GameObject obj in objects) {
            if (obj == null) continue;
            if (thisCollider == obj.GetComponent<Collider>()) {
                obj.SendMessage("OnCollisionEnter",col);
                return;
            }
        }
    }

}
