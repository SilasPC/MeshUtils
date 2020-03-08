using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MeshUtils;
using static MeshUtils.API;

//using Valve.VR.InteractionSystem;

public class Inspectable : MonoBehaviour
{
    
    List<Inspectable> children;
    Vector3 relativePosition = Vector3.zero;
    bool collapsed = true;

    bool allowCutting = true;

    int cooldown = 30;

    void Start() {
        /*Throwable throwable;
        if (!TryGetComponent<Throwable>(out throwable)) return;
        throwable.onDetachFromHand.AddListener(OnDetachFromHand);
        throwable.onPickUp.AddListener(OnPickUp);*/
    }

    /*void OnPickUp() {}

    void OnDetachFromHand() {
        StartCoroutine(MoveRoutine());
    }

    Vector3 vel = Vector3.zero;
    IEnumerator MoveRoutine() {
        if (vel != Vector3.zero) yield break;
        SetAllowCutting(false);
        while (transform.localPosition.magnitude > 0.01f) {
            Debug.Log(vel);
            transform.localPosition = Vector3.SmoothDamp(
                transform.localPosition,
                Vector3.zero,
                ref vel,
                0.4f
            );
            yield return null;
        }
        transform.localPosition = Vector3.zero;
        SetAllowCutting(true);
    }*/

    void Update() {
        if (cooldown > 0) cooldown--;
    }

    public void Split(GameObject obj, CuttingPlane plane) {
        Inspectable toSplit = Find(obj);
        if (toSplit == null) return;
        if (toSplit.cooldown > 0) return;
        if (!allowCutting) return;
        toSplit.Split(plane);
        StartCoroutine(toSplit.ExpandRoutine());
    }

    public Inspectable Find(GameObject obj) {
        if (gameObject == obj) return this;
        if (children != null)
        foreach (var child in children) {
            var res = child.Find(obj);
            if (res != null) return res;
        }
        return null;
    }

    IEnumerator ToggleRoutine() {
        if (collapsed) return ExpandRoutine();
        return CollapseRoutine();
    }

    IEnumerator CollapseRoutine() {
        if (children == null) yield break;
        float t = 1;
        do {
            if (t < 0) t = 0;
            float smoothT = Mathf.SmoothStep(0,1,t);
            LerpPosition(smoothT);
            yield return null;
        } while ((t -= 0.05f) >= 0);
        SetShown(true);
        SetChildrenShown(false);
        collapsed = true;
    }

    IEnumerator ExpandRoutine() {
        SetShown(false);
        if (children == null) Split(
            CuttingPlane.InLocalSpace(
                UnityEngine.Random.insideUnitSphere.normalized,
                Vector3.zero,
                transform
            )
        );
        SetChildrenShown(true);
        float t = 0;
        do {
            if (t > 1) t = 1;
            float smoothT = Mathf.SmoothStep(0,1,t);
            LerpPosition(smoothT);
            yield return null;
        } while ((t += 0.05f) <= 1);
        collapsed = false;
    }
    
    void Split(CuttingPlane plane) {
        DestroyChildren();
        CutParams param = new CutParams(true,false);
        CutResult result = PerformCut(gameObject,plane,param);
        if (result == null) return;
        children = new List<Inspectable>();
        foreach (var res in result.results) {
            GameObject newObj = res
                .CopyMaterial()
                .WithCollider()
                .Create();
            newObj.transform.SetParent(transform);
            Inspectable newInspectable = newObj.AddComponent<Inspectable>();
            newInspectable.relativePosition = res.GetLocalDriftDirection() * 0.1f;
            children.Add(newInspectable);
        }
        SetChildrenShown(false);
    }
    void LerpPosition(float t) {
        transform.localPosition = t * relativePosition;
        if (children != null)
        foreach (var child in children)
            child.LerpPosition(t);
    }
    void SetChildrenShown(bool shown) {
        if (children != null)
        foreach (var child in children)
            child.SetShown(shown);
    }
    void SetShown(bool shown) {
        GetComponent<Renderer>().enabled = shown;
        GetComponent<Collider>().enabled = shown;
        if (children != null)
        foreach (var child in children)
            child.SetShown(shown);
    }
    void DestroyChildren() {
        if (children != null)
        foreach (var child in children)
            child.Destroy();
    }
    void SetAllowCutting(bool allow) {
        if (children != null)
        foreach (var child in children)
            child.SetAllowCutting(allow);
        this.allowCutting = allow;
    }
    void Destroy() {
        MonoBehaviour.Destroy(gameObject);
    }
    
}
