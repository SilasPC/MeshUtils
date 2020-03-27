
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshUtils {

    public class ComplexKnife : MonoBehaviour {

        private CuttingTemplate template;

        void OnTriggerEnter() {
            if (template == null) template = CuttingTemplate.InWorldSpace(new Vector3(1,0,0),transform.position);
        }

        void OnTriggerStay() {
            template.AddPoint(transform.position);
            GetComponent<Rigidbody>().velocity += new Vector3(0,0,0.5f);
            template.Draw();
        }

        void OnTriggerExit(Collider col) {
            template.PrepareTmp();
            template.Draw();
            var res = API.tmp(col.gameObject,template);
            if (res != null) {
                foreach (var r in res.results) {
                    r.WithColor(Color.blue).Instantiate();
                }
                Debug.Break();
            } else Debug.Log("fail");
        }

        void Draw(Vector3 p, Vector3 n) {
            StartCoroutine(DrawRoutine(p,n));
        }
        IEnumerator DrawRoutine(Vector3 p, Vector3 n) {
            for (int i = 0; i < 50; i++) {
                Debug.DrawRay(p,n * 10, Color.red);
                yield return null;
            }
            yield break;
        }

    }

}