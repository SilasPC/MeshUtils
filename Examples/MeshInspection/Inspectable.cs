using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MeshUtils;
using static MeshUtils.API;

class Obj {
    private readonly GameObject obj;
    private readonly Vector3 originalPosition;
    private readonly Vector3 relativePosition;
    private bool collapsed = true;
    public List<Obj> children;
    public Obj (GameObject obj, Vector3 relativePosition) {
        this.obj = obj;
        this.relativePosition = relativePosition;
        this.originalPosition = obj.transform.localPosition;
    }
    public Obj Find(GameObject obj) {
        if (this.obj == obj) return this;
        foreach (var child in children) {
            var res = child.Find(obj);
            if (res != null) return res;
        }
        return null;
    }
    public IEnumerator ToggleRoutine() {
        if (collapsed) return ExpandRoutine();
        return CollapseRoutine();
    }
    public IEnumerator CollapseRoutine() {
        if (children == null) yield break;
        float t = 1;
        do {
            if (t < 0) t = 0;
            float smoothT = Mathf.SmoothStep(0,1,t);
            LerpPosition(smoothT);
            yield return null;
        } while ((t -= 0.05f) >= 0);
        SetShown(true);
        foreach (var child in children)
            child.SetShown(false);
        collapsed = true;
    }
    public IEnumerator ExpandRoutine() {
        SetShown(false);
        if (children == null) Split(CuttingPlane.InLocalSpace(UnityEngine.Random.insideUnitSphere.normalized,Vector3.zero,obj.transform));
        foreach (var child in children)
            child.SetShown(true);
        float t = 0;
        do {
            if (t > 1) t = 1;
            float smoothT = Mathf.SmoothStep(0,1,t);
            LerpPosition(smoothT);
            yield return null;
        } while ((t += 0.05f) <= 1);
        collapsed = false;
    }
    public void Split(CuttingPlane plane) {
        if (this.children != null) return;
        CutParams param = new CutParams(true,false);
        CutResult result = PerformCut(obj,plane,param);
        if (result == null) return;
        children = new List<Obj>();
        foreach (var res in result.results) {
            GameObject newObj = res
                .CopyMaterial()
                .WithCollider()
                .Create();
            newObj.transform.SetParent(obj.transform);
            children.Add(new Obj(newObj,res.GetDriftDirection() * 0.1f));
        }
    }
    private void LerpPosition(float t) {
        obj.transform.localPosition = originalPosition + t * relativePosition;
        if (children != null)
        foreach (var child in children)
            child.LerpPosition(t);
    }
    private void SetShown(bool shown) {
        obj.GetComponent<Renderer>().enabled = shown;
        if (children != null)
        foreach (var child in children)
            child.SetShown(shown);
    }

}

public class Inspectable : MonoBehaviour
{

    private Obj obj;

    void Start() {
        obj = new Obj(gameObject,Vector3.zero);
    }
    
    int t = 0;

    void Update() {
        t = ++t % 120;
        if (t == 0) {
            StartCoroutine(obj.ToggleRoutine());
        }
    }
    
}
