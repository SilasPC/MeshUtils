using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassCollisions : MonoBehaviour
{

    public KnifeExample PassTo;

    public void OnCollisionEnter(Collision col) {
        PassTo.PassCollision(col);
    }

}
