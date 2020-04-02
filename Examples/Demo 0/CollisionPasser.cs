using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionPasser : MonoBehaviour
{

    public CuttingCollider PassTo;

    public void OnCollisionEnter(Collision col) {
        PassTo.PassCollision(col);
    }

}
