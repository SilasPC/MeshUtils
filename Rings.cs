
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

        public bool HasPartials() {return this.partials.Count > 0;}
        public List<List<Vector3>> GetRings() {return this.complete;}

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

}