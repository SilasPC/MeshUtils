
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

using MeshUtils;
using static MeshUtils.API;
using static MeshUtils.VectorUtil;

namespace MeshUtils {

    public class Pistol : MonoBehaviour {

        public SteamVR_Action_Boolean pullTriggerAction;

        void Start() {
            pullTriggerAction.AddOnStateDownListener(TriggerPulled, SteamVR_Input_Sources.Any);
        }

        void TriggerPulled(SteamVR_Action_Boolean action, SteamVR_Input_Sources src) {
            if (GetComponent<Valve.VR.InteractionSystem.Interactable>().attachedToHand == null) return;
            RaycastHit rayHit;
            Ray ray = new Ray(transform.position + transform.up * 0.025f, -transform.forward);
            if (
                Physics.Raycast(
                    ray,
                    out rayHit,
                    100
                )
            ) {
                // Debug.DrawRay(ray.origin, ray.direction * rayHit.distance, Color.white, 0.25f);
                GameObject obj = rayHit.collider.gameObject;
                if (obj.tag == "Shootable") {
                    IterativeCut(obj,3).ForEach(r=>r.GetComponent<Rigidbody>().velocity += 5 * ray.direction);
                }
            }
        }

        List<GameObject> IterativeCut(GameObject obj, int count) {
            CutParams param = new CutParams(false,false,true,false,false,false,Vector3.zero,float.PositiveInfinity,0,Vector3.zero);
            try {
                CutResult res = API.PerformCut(obj,CuttingPlane.RandomInWorldSpace(obj.transform.position),param);
                res.DestroyObject();
                if (res == null) return new List<GameObject>();
                if (count > 1) {
                    List<GameObject> ret = new List<GameObject>();
                    foreach (CutObj robj in res.Results)
                        ret.AddRange(
                            IterativeCut(
                                robj
                                    .CopyParent()
                                    .CopyMaterial()
                                    .CopyVelocity()
                                    .WithDriftVelocity(0.2f)
                                    .Instantiate(),
                                count - 1
                            )
                        );
                    return ret;
                } else return res.ConvertAll(
                    robj =>
                        robj
                            .CopyParent()
                            .CopyMaterial()
                            .WithCollider()
                            .WithRenderer()
                            .CopyVelocity()
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