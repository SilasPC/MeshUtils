
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

            Chopable chopable;
            if (col.gameObject.TryGetComponent<Chopable>(out chopable)) Chop(col.gameObject,chopable);
            
        }

        DateTime lastChop = DateTime.Now;
        void Chop(GameObject obj, Chopable cc) {

            if ((DateTime.Now-lastChop).TotalSeconds < 2) return;

            lastChop = DateTime.Now;

            CuttingTemplate template = CuttingTemplate.InLocalSpace(Vector3.up,Vector3.zero,transform).ToWorldSpace();

            Vector3 r = transform.forward * 0.1f,
                    f = transform.right * -0.2f,
                    p = transform.position;

            template.AddPoint(p + r - 5 * f);
            template.AddPoint(p + r + 1.2f * f);
            if (!pointy) {
                template.AddPoint(p + 0.3f * r + 2.2f * f);
                template.AddPoint(p - 0.3f * r + 2.2f * f);
            } else template.AddPoint(p + 2 * f);
            template.AddPoint(p - r + 1.2f * f);
            template.AddPoint(p - r - 5 * f);

            var res = API.tmp(obj,template);
            if (res != null) {
                foreach (var rm in res.results) {
                    if (!rm.IsPositive()) continue;
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

    }

}