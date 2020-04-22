
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

        private HashSet<GameObject> canvases = new HashSet<GameObject>();
        private int canvasesInTrigger = 0;

        private DateTime lastDraw = DateTime.Now;

        void OnCollisionEnter(Collision col) {

            if (col.gameObject.tag != "CutCanvas") return;

            if ((DateTime.Now-lastDraw).TotalSeconds < 1) return;

            var canvas = col.gameObject;

            canvases.Add(canvas);

            canvasesInTrigger++;

            canvas.GetComponent<Collider>().isTrigger = true;

            var con = col.GetContact(0);

            localCol = transform.InverseTransformPoint(con.point);

            template = CuttingTemplate.InWorldSpace(Perturb(con.normal,0.015f),con.point);
            
        }

        void OnTriggerEnter(Collider col) {
            if (canvases.Contains(col.gameObject))
                canvasesInTrigger++;
        }

        void Update() {

            if (template == null) return;
            template.AddPoint(transform.TransformPoint(localCol));

        }

        void OnTriggerExit(Collider col) {
            
            if (canvases.Contains(col.gameObject))
                canvasesInTrigger--;

            if (canvasesInTrigger == 0)
                DoDraw();

        }
        void DoDraw() {

            template.Inflate(0.02f);
            template.DrawDebug();

            try {
                foreach (GameObject canvas in canvases) {
                    CutResult res = API.tmp(canvas,template);

                    if (res != null) {
                        res.DestroyObject();
                        foreach (CutObj obj in res.NegativeResults)
                            obj
                                .CopyParent()
                                .CopyMaterial()
                                .Instantiate();
                    }
                }
            } catch (MeshUtilsException e) {
                Debug.LogException(e);
            }

            lastDraw = DateTime.Now;
            canvases.Clear();
            template = null;
        }

    }

}