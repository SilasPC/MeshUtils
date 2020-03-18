
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshUtils {

    // -----------------------------------------------------------
    // A ring is a set of coplanar verticies representing the
    //   polygon of the revealed inside geometry after a cut
    // Because it is a set of vectors, the polygon is positioned
    //   correctly for any newly generated meshes
    // -----------------------------------------------------------
    class RingGenerator {

        private List<List<Vector3>> complete = new List<List<Vector3>>();
        private List<List<Vector3>> partials = new List<List<Vector3>>();

        public List<Ring> GetRings() {
            if (this.partials.Count > 0) throw OperationException.MalformedMesh();
            return this.complete.ConvertAll(r=>new Ring(r));
        }

        public void MyDebugLog() {
            Debug.Log(complete.Count + " complete rings, " +  partials.Count + " partial rings");
        }

        // ----------------------------------
        // Add a vector pair to the ring
        // ----------------------------------
        public void AddConnected(Vector3 v0, Vector3 v1) {
            foreach (List<Vector3> ring in partials) {
                if (ring[0] == v1) {
                    if (ring[ring.Count - 1] == v0) {
                        complete.Add(ring);
                        partials.Remove(ring);
                        return;
                    }
                    foreach (List<Vector3> ring2 in partials) {
                        if (ring == ring2) continue;
                        if (ring2[ring2.Count - 1] == v0) {
                            ring2.AddRange(ring);
                            partials.Remove(ring);
                            return;
                        }
                    }
                    ring.Insert(0,v0);
                    return;
                } else if (ring[ring.Count - 1] == v0) {
                    if (ring[0] == v1) {
                        complete.Add(ring);
                        partials.Remove(ring);
                        return;
                    }
                    foreach (List<Vector3> ring2 in partials) {
                        if (ring == ring2) continue;
                        if (ring2[0] == v1) {
                            ring.AddRange(ring2);
                            partials.Remove(ring2);
                            return;
                        }
                    }
                    ring.Add(v1);
                    return;
                }
            }
            List<Vector3> newRing = new List<Vector3>() {v0,v1};
            partials.Add(newRing);
        }

    }

    struct Ring {
        public readonly List<Vector3> verts;
        public Ring(List<Vector3> ring) {
            this.verts = ring;
        }

        // ---------------------------------------
        // Check if a point lies within the ring
        // ---------------------------------------
        public bool CheckPointInsideRing(Vector3 v, Vector3 normal) {
            int i0, i1;
            DistanceToRingPerimeter(v,out i0, out i1);
            Vector3 side = verts[i0] - verts[i1];
            Vector3 toSide = verts[i0] - v;
            return
                Vector3.Dot(
                    Vector3.Cross(side,toSide),
                    normal
                ) < 0;
        }

        // ---------------------------------------------------------------
        // Determine shortest distance to ring perimeter
        // i0 and i1 are indices for vertexes around closest line in ring
        // ---------------------------------------------------------------
        public float DistanceToRingPerimeter(Vector3 v, out int i0, out int i1) {
            int prevIndex = verts.Count - 1;
            i0 = prevIndex;
            i1 = 0;
            float dMin = float.PositiveInfinity;
            for (int i = 0; i < verts.Count; i++) {
                float d = VectorUtil.DistanceToEdge(v,verts[prevIndex],verts[i]);
                if (d < dMin) {
                    dMin = d;
                    i0 = prevIndex;
                    i1 = i;
                }
                prevIndex = i;
            }
            return dMin;
        }

        // ---------------------------------------------
        // Vector from point to furthest point in ring
        // ---------------------------------------------
        public Vector3 FurthestVectorToRingPerimeter(Vector3 p) {
            float dist = 0;
            Vector3 v0 = Vector3.zero;
            foreach (Vector3 v in verts) {
                float newDist = (v-p).magnitude;
                if (newDist > dist) {
                    dist = newDist;
                    v0 = v;
                }
            }
            return v0-p;
        }
        
        // ------------------------------------
        // Closest indices between two rings
        // ------------------------------------
        public float RingDist(Ring other, ref int i0, ref int i1) {
            float mag = float.PositiveInfinity;
            for (int i = 0; i < verts.Count; i++) {
                for (int j = 0; j < other.verts.Count; j++) {
                    float nMag = (verts[i]-other.verts[j]).magnitude;
                    if (nMag < mag) {
                        i0 = i;
                        i1 = j;
                        mag = nMag;
                    }
                }
            }
            return mag;
        }

    }

}
