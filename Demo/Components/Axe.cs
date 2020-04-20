
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MeshUtils;
using static MeshUtils.API;
using static MeshUtils.VectorUtil;

namespace MeshUtils {

    public class Axe : MonoBehaviour {

        public bool pointy = true;

        public void OnCollisionEnter(Collision col) {

            if (col.relativeVelocity.magnitude < 2.5f) return;

            Chopable chopable;
            TreeChopablePart tcc;
            if (col.gameObject.TryGetComponent(out chopable)) Chop(col.gameObject,chopable);
            else if (col.gameObject.TryGetComponent(out tcc)) Chop(col.gameObject,tcc);
            
        }

        void Chop(GameObject obj, TreeChopablePart tcc) {
            
            if ((DateTime.Now-lastChop).TotalSeconds < 2) return;

            lastChop = DateTime.Now;

            var res = API.tmp(obj,GetTemplate(),true);
            if (res != null) {
                res.DestroyObject();
                List<GameObject> robjs = res.PositiveResults.ConvertAll(
                    rm => rm
                        .CopyParent()
                        .CopyMaterial()
                        .WithCollider(true)
                        .Instantiate()
                );
                if (robjs.Count > 1) {
                    tcc.abovePart.GetComponent<Rigidbody>().isKinematic = false;
                    GameObject above = null;
                    float maxY = float.NegativeInfinity;
                    foreach (GameObject robj in robjs) {
                        float y = robj.GetComponent<Collider>().bounds.center.y;
                        if (y > maxY) {
                            above = robj;
                            maxY = y;
                        }
                    }
                    above.transform.SetParent(tcc.abovePart.transform);
                } else foreach (GameObject robj in robjs) tcc.CopyTo(robj);
            } else Debug.Log("fail");

        }

        DateTime lastChop = DateTime.Now;
        void Chop(GameObject obj, Chopable cc) {

            if ((DateTime.Now-lastChop).TotalSeconds < 2) return;

            lastChop = DateTime.Now;

            var res = API.tmp(obj,GetTemplate());
            if (res != null) {
                res.DestroyObject();
                foreach (var rm in res.PositiveResults) {
                    GameObject robj = rm
                        .CopyParent()
                        .CopyMaterial()
                        .WithCollider()
                        .Instantiate();
                    robj.GetComponent<MeshCollider>().convex = false;
                    cc.CopyTo(robj);
                }
            } else Debug.Log("fail");
        }

        CuttingTemplate GetTemplate() {

            CuttingTemplate template = CuttingTemplate.InLocalSpace(Vector3.up,Vector3.zero,transform).ToWorldSpace();

            Vector3 r = transform.forward * 0.1f,
                    f = transform.right * -0.2f,
                    p = transform.position;

            template.AddPoints(
                p + r - 5 * f,
                p + r + 1.2f * f
            );
            if (!pointy) {
                template.AddPoints(
                    p + 0.3f * r + 2.2f * f,
                    p - 0.3f * r + 2.2f * f
                );
            } else template.AddPoint(p + 2 * f);
            template.AddPoints(
                p - r + 1.2f * f,
                p - r - 5 * f
            );

            return template;

        }

    }

}