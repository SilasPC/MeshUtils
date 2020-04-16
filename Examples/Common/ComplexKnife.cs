
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshUtils {

    public class ComplexKnife : MonoBehaviour {

        public Vector3 EdgeDirection = Vector3.forward;
        public float MaxAngle = 5;

        private LineRenderer line;

        private CuttingTemplate template;
        private Vector3 entranceDir;
        private List<Vector3> hits = new List<Vector3>();

        void OnTriggerEnter() {
            if (template == null) {
                if (line == null) line = gameObject.AddComponent<LineRenderer>();
                line.positionCount = 0;
                line.numCornerVertices = 2;
                line.widthMultiplier = 0.05f;
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.material.color = Color.red;
                line.startColor = Color.red;
                line.endColor = Color.red;
                template = CuttingTemplate.InLocalSpace(EdgeDirection,Vector3.zero,transform).ToWorldSpace();
                entranceDir = transform.TransformDirection(EdgeDirection);
                hits.Clear();
            }
        }

        void OnTriggerStay(Collider col) {
            Vector3 dir = transform.TransformDirection(EdgeDirection);
            if (Vector3.Angle(dir,entranceDir) > MaxAngle) {
                template = null;
            }
            if (template == null) return;
            //Debug.DrawRay(transform.position-dir,0.1f*dir,Color.green,1,true);
            Ray ray = new Ray(transform.position-dir,dir);
            RaycastHit hit;
            if (col.Raycast(ray,out hit,100)) {
                if (template.AddPoint(hit.point)) {
                    hits.Add(hit.point);
                    line.positionCount = hits.Count;
                    line.SetPositions(hits.ToArray());
                }
            }
            /*foreach (Vector3 p in hits) {
                Debug.DrawRay(p,-dir,Color.red,1,true);
            }*/
            //template.Draw();
        }

        void OnTriggerExit(Collider col) {
            template.PrepareTmp();
            template.DrawDebug();
            var res = API.tmp(col.gameObject,template);
            if (res != null) {
                foreach (var r in res.results) {
                    r.UseDefaults().Instantiate();
                }
                Debug.Break();
            } else Debug.Log("fail");
            template = null;
        }

    }

}