
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MeshUtils;
using static MeshUtils.API;
using static MeshUtils.VectorUtil;

namespace MeshUtils {

    public class Axe : MonoBehaviour {

        public Vector3 cutDirection, edgeDirection;
        public float maxAngle = 15;

        public bool pointy = true;

        public void OnCollisionEnter(Collision col) {

            /*if (col.relativeVelocity.magnitude < 2.5f) return;

            Vector3 cutDir = transform.TransformDirection(cutDirection);

            Vector3 edge = transform.TransformDirection(edgeDirection);

            Vector3 angleProjection = Vector3.ProjectOnPlane(gameObject.GetComponentInParent<Rigidbody>().velocity,edge);

            if (Vector3.Angle(angleProjection,cutDir) > maxAngle) return;
*/
            Chopable chopable;
            TreeChopablePart tcc;
            if (col.gameObject.TryGetComponent(out chopable)) Chop(col.gameObject,chopable);
            else if (col.gameObject.TryGetComponent(out tcc)) Chop(col.gameObject,tcc);
            
        }

        void Chop(GameObject obj, TreeChopablePart tcc) {
            
            if ((DateTime.Now-lastChop).TotalSeconds < 2) return;

            lastChop = DateTime.Now;

            var res = API.tmp(obj,GetTemplate());
            if (res != null) {
                res.DestroyObject();
                res.PolySeparatePositive();
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
                    above.GetComponent<MeshCollider>().convex = true;
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

            Vector3 r = transform.forward * 0.07f,
                    f = transform.right * -0.2f,
                    p = transform.position;

            template.AddPoints(
                p + r - 5 * f,
                p + r + 1.2f * f
            );
            if (!pointy) {
                template.AddPoints(
                    p + 0.3f * r + 1.8f * f,
                    p - 0.3f * r + 1.8f * f
                );
            } else template.AddPoint(p + 1.8f * f);
            template.AddPoints(
                p - r + 1.2f * f,
                p - r - 5 * f
            );

            return template;

        }

    }

}