
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

using MeshUtils;
using static MeshUtils.API;
using static MeshUtils.VectorUtil;

namespace MeshUtils {

    public class Grenade : MonoBehaviour {

        public SteamVR_Action_Boolean onDetachAction;

        public GameObject explosionPrefab;
        
        bool triggered = false;
        int ttl = 180;

        void Start() {
            onDetachAction.AddOnStateUpListener(OnDetach, SteamVR_Input_Sources.Any);
        }

        void OnDetach(SteamVR_Action_Boolean action, SteamVR_Input_Sources src) {
            if (GetComponent<Valve.VR.InteractionSystem.Interactable>().attachedToHand != null)
                triggered = true;
        }

        void Update() {
            if (triggered) ttl--;
            if (ttl == 0) StartCoroutine(Explode());
        }

        IEnumerator Explode() {
            GameObject expl = Instantiate(explosionPrefab);
            expl.transform.position = transform.position;
            expl.transform.localScale = new Vector3(0.58f, 0.58f, 0.58f);
            List<Rigidbody> rbs = new List<Rigidbody>();
            foreach (Collider col in Physics.OverlapSphere(transform.position, 3)) {
                if (col.gameObject.tag != "Shootable" && col.gameObject.tag != "Explodable") continue;
                foreach (GameObject obj in IterativeCut(col.gameObject, 2))
                    rbs.Add(obj.GetComponent<Rigidbody>());
            }
            yield return null;
            foreach (Collider col in Physics.OverlapSphere(transform.position, 4)) {
                Rigidbody rb;
                if (col.TryGetComponent<Rigidbody>(out rb))
                    rb.AddExplosionForce(40, transform.position, 4, 0.5f);
            }   
            Destroy(expl, 1.8f);
            onDetachAction.RemoveOnStateUpListener(OnDetach, SteamVR_Input_Sources.Any);
            Destroy(gameObject);
        }

        List<GameObject> IterativeCut(GameObject obj, int count) {
            CutParams param = new CutParams(true,false,true,false,false,false,Vector3.zero,float.PositiveInfinity,0,Vector3.zero);
            try {
                Vector3 pos = obj.transform.position;
                Collider col;
                if (obj.TryGetComponent<Collider>(out col))
                    pos = col.bounds.center;
                CutResult res = API.PerformCut(obj,CuttingPlane.RandomInWorldSpace(obj.transform.position),param);
                if (res == null) return new List<GameObject>();
                if (count > 1) {
                    List<GameObject> ret = new List<GameObject>();
                    foreach (CutObj robj in res.results)
                        ret.AddRange(
                            IterativeCut(
                                robj
                                    .CopyParent()
                                    .CopyMaterial()
                                    .CopyVelocity()
                                    .CopyDensity()
                                    .WithCollider()
                                    .WithDriftVelocity(0.2f)
                                    .Instantiate(),
                                count - 1
                            )
                        );
                    return ret;
                } else return res.results.ConvertAll(
                    robj =>
                        robj
                            .CopyParent()
                            .CopyMaterial()
                            .WithCollider()
                            .WithRenderer()
                            .CopyVelocity()
                            .CopyDensity()
                            .WithDriftVelocity(0.2f)
                            .Instantiate()
                );
            } catch (MeshUtilsException e) {
                Debug.LogWarning(e);
                obj.AddComponent<Rigidbody>();
                obj.AddComponent<MeshRenderer>();
                obj.AddComponent<MeshCollider>();
                return new List<GameObject>() {obj};
            }
        }

    }

}