
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MeshUtils;
using static MeshUtils.API;
using static MeshUtils.VectorUtil;

namespace MeshUtils {

    public class HoleCutTest : MonoBehaviour {

        DateTime lastTime = DateTime.Now;

        public void OnCollisionEnter(Collision col) {

            if (col.gameObject.tag != "Cuttable") return;

            if ((DateTime.Now-lastTime).TotalSeconds < 2) return;
            lastTime = DateTime.Now;

            var con = col.GetContact(0);
            
            Vector3 n = (con.normal + VectorUtil.UnitPerpendicular(con.normal) * 0.07f).normalized;

            CuttingTemplate template = CuttingTemplate.InWorldSpace(n,con.point).SetClosed();

            Vector3 a = VectorUtil.UnitPerpendicular(n) * 0.15f;
            Vector3 b = Vector3.Cross(a,n).normalized * 0.15f;

            Debug.DrawRay(con.point,con.normal,Color.green,10);
            Debug.DrawRay(con.point,a,Color.blue,10);
            Debug.DrawRay(con.point,b,Color.black,10);

            template.AddPoint(con.point+a+b);
            template.AddPoint(con.point+a-b);
            template.AddPoint(con.point-a-b);
            template.AddPoint(con.point-a+b);

            template.Draw();

            var res = API.tmp(col.gameObject,template);
            if (res != null) {
                foreach (var rm in res.results) {
                    if (rm.IsPositive()) continue;
                    GameObject obj = rm
                        .CopyParent()
                        .CopyMaterial()
                        .WithRigidbody()
                        .WithCollider()
                        .Instantiate();
                    obj.tag = "Cuttable";
                    obj.GetComponent<Rigidbody>().isKinematic = true;
                    obj.GetComponent<MeshCollider>().convex = false;
                }
            } else Debug.Log("fail");
            
        }

    }

}