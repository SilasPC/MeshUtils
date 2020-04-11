
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MeshUtils;
using static MeshUtils.API;
using static MeshUtils.VectorUtil;

namespace MeshUtils {

    public class Axe : MonoBehaviour {

        DateTime lastChop = DateTime.Now;

        public void OnCollisionEnter(Collision col) {

            if (col.gameObject.tag != "Cuttable") return;

            if ((DateTime.Now-lastChop).TotalSeconds < 2) return;
            lastChop = DateTime.Now;

            CuttingTemplate template = CuttingTemplate.InLocalSpace(Vector3.up,Vector3.zero,transform).ToWorldSpace();

            Vector3 r = transform.forward * 0.1f,
                    f = transform.right * -0.2f,
                    p = transform.position;

            template.AddPoint(p + r - 5 * f);
            template.AddPoint(p + r + f);
            //template.AddPoint(p + 0.3f * r + 2 * f);
            template.AddPoint(p + 2 * f);
            //template.AddPoint(p - 0.3f * r + 2 * f);
            template.AddPoint(p - r + f);
            template.AddPoint(p - r - 5 * f);

            template.Draw();

            var res = API.tmp(col.gameObject,template);
            if (res != null) {
                foreach (var rm in res.results) {
                    rm.UseDefaults().WithRingWidth(0).Instantiate().tag = "Cuttable";
                }
            } else Debug.Log("fail");
            
        }

    }

}