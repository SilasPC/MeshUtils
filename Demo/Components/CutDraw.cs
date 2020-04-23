
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MeshUtils;
using static MeshUtils.API;
using static MeshUtils.VectorUtil;

namespace MeshUtils {

    public class CutDraw : MonoBehaviour {

        private CuttingTemplate template;
        
        private Vector3 localCol;

        private GameObject canvas = new GameObject();

        private DateTime lastDraw = DateTime.Now;

        void OnCollisionEnter(Collision col) {

            if (col.gameObject.tag != "CutCanvas") return;

            if ((DateTime.Now-lastDraw).TotalSeconds < 1) return;

            canvas = col.gameObject;

            canvas.GetComponent<Collider>().isTrigger = true;

            var con = col.GetContact(0);

            localCol = transform.InverseTransformPoint(con.point);

            template = CuttingTemplate.InWorldSpace(Perturb(con.normal,0.015f),con.point);
            
        }

        void Update() {

            if (template == null) return;
            template.AddPoint(transform.TransformPoint(localCol),0,0.03f);

        }

        void OnTriggerExit(Collider col) {

            if (canvas == col.gameObject)
                DoDraw();

        }
        void DoDraw() {
            
            Debug.Log(template.points.Count);

            template.Inflate(0.01f);
            template.DrawDebug();

            try {
                CutResult res = API.tmp(canvas,template);

                if (res != null) {
                    res.DestroyObject();
                    foreach (CutObj obj in res.NegativeResults)
                        obj
                            .CopyParent()
                            .CopyMaterial()
                            .WithCollider()
                            .Instantiate()
                            .tag = "CutCanvas";
                }
            } catch (MeshUtilsException e) {
                Debug.LogException(e);
                canvas.GetComponent<Collider>().isTrigger = false;
            }

            lastDraw = DateTime.Now;
            canvas = null;
            template = null;
        }

    }

}