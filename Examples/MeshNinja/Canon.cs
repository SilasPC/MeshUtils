using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Canon : MonoBehaviour
{
    
    public GameObject prefab;

    void Start() {
        StartShooting();
    }

    public void StartShooting() {
        StartCoroutine(ShootRoutine());
    }

    private IEnumerator ShootRoutine() {
        while (true) {
            var obj = Instantiate(prefab);
            obj.SetActive(true);
            Physics.IgnoreCollision(GetComponent<Collider>(),obj.GetComponent<Collider>());
            obj.transform.position = transform.position + transform.up * 2;
            obj.GetComponent<Rigidbody>().velocity = transform.up * 10;
            obj.AddComponent<MeshProjectile>();
            yield return new WaitForSeconds(2);
        }
    }

}
