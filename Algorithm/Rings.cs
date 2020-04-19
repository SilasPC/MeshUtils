
using System;
using System.Linq;
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

        protected List<List<Vector3>> complete = new List<List<Vector3>>();
        protected List<List<Vector3>> partials = new List<List<Vector3>>();

        public RingGenerator Duplicate() {
            RingGenerator gen = new RingGenerator();
            gen.complete = complete.ConvertAll(r=>r.ConvertAll(v=>v));
            gen.partials = partials.ConvertAll(r=>r.ConvertAll(v=>v));
            return gen;
        }

        public bool HasComplete() {
            return this.complete.Count > 0;
        }

        public bool HasPartials() {
            return this.partials.Count > 0;
        }

        public List<Ring> GetRings(bool selfConnectPartials, bool ignorePartials) {
            List<Ring> res = this.complete.ConvertAll(r=>new Ring(r));
            if (selfConnectPartials) {
                foreach (List<Vector3> p in partials) {
                    if (p.Count < 3) continue;
                    res.Add(new Ring(p));
                }
            }
            if (!ignorePartials && !selfConnectPartials && this.partials.Count > 0) throw MeshUtilsException.MalformedMesh("Incomplete intersections found");
            return res;
        }

        public void MyDebugLog() {
            Debugging.DebugRings(partials);
            /*foreach (var p in partials) {
                Debug.Log(p.First().GetHashCode()+" -> "+p.Last().GetHashCode());
            }*/
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

    // Newer version, not in use atm.
    // Time complexity should be better, not tested much yet
    public class RingGenerator2 {

        protected List<List<Vector3>> complete = new List<List<Vector3>>();
        protected Dictionary<Vector3, List<Vector3>> startMap = new Dictionary<Vector3, List<Vector3>>(), endMap = new Dictionary<Vector3, List<Vector3>>();

        public void MyDebugLog() {
            Debug.Log(complete.Count + " complete rings, " +  startMap.Count + " partial rings");
        }

        public RingGenerator2 Duplicate() {
            RingGenerator2 gen = new RingGenerator2();
            gen.complete = complete.ConvertAll(r=>new List<Vector3>(r));
            List<List<Vector3>> partials = new List<List<Vector3>>(startMap.Values.Select(l => new List<Vector3>(l)));
            foreach (var p in partials) {
                gen.startMap.Add(p.First(),p);
                gen.endMap.Add(p.Last(),p);
            }
            return gen;
        }

        public bool HasComplete() {
            return this.complete.Count > 0;
        }

        public bool HasPartials() {
            return this.startMap.Count > 0;
        }

        public List<Ring> GetRings(bool selfConnectPartials, bool ignorePartials) {
            List<Ring> res = this.complete.ConvertAll(r=>new Ring(r));
            if (selfConnectPartials) {
                foreach (List<Vector3> p in startMap.Values) {
                    if (p.Count < 3) continue;
                    res.Add(new Ring(p));
                }
            }
            if (!ignorePartials && !selfConnectPartials && this.startMap.Count > 0) throw MeshUtilsException.MalformedMesh("Incomplete intersections found");
            return res;
        }

        public void AddConnected(Vector3 v0, Vector3 v1) {
            if (startMap.ContainsKey(v1)) {
                List<Vector3> p = startMap[v1];
                List<Vector3> p2 = null;
                endMap.TryGetValue(v0, out p2);
                startMap.Remove(v1);
                if (p == p2) {
                    complete.Add(p);
                    endMap.Remove(v0);
                    return;
                }
                if (p2 != null) {
                    // prepend p2 to p
                    endMap[p.Last()] = p2;
                    p2.AddRange(p);
                    return;
                }
                p.Insert(0, v0);
                startMap.Add(v0,p);
                return;
            } else if (endMap.ContainsKey(v0)) {
                List<Vector3> p = endMap[v0];
                List<Vector3> p2 = null;
                startMap.TryGetValue(v1, out p2);
                endMap.Remove(v0);
                if (p == p2) {
                    complete.Add(p);
                    startMap.Remove(v1);
                    return;
                }
                if (p2 != null) {
                    // append p2 to p
                    endMap[p2.Last()] = p;
                    p.AddRange(p2);
                    return;
                }
                p.Add(v1);
                endMap.Add(v1,p);
                return;
            }
            List<Vector3> pNew = new List<Vector3>() {v0,v1};
            startMap.Add(v0,pNew);
            endMap.Add(v1,pNew);
        }
    }

    public struct Ring {
        public readonly List<Vector3> verts;
        public Ring(List<Vector3> ring) {
            this.verts = ring;
        }

        // ---------------------------------------
        // Check if a point lies within the ring
        // ---------------------------------------
        public bool CheckPointInsideRing(Vector3 v, Vector3 normal) {
            int i0, i1;
            DistanceToRingPerimeter(v,out i0, out i1); // method handles degenerate case
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
        // In the case of equality, one is chosen specifically to handle
        //   the degenerate case in point-in-polygon test
        // ---------------------------------------------------------------
        public float DistanceToRingPerimeter(Vector3 v, out int i0, out int i1) {
            int prevIndex = verts.Count - 1;
            i0 = prevIndex;
            i1 = 0;
            float dMin = float.PositiveInfinity;
            float lastLineDist = VectorUtil.DistanceToLine(v,verts[prevIndex-1],verts[prevIndex]);
            for (int i = 0; i < verts.Count; i++) {
                float lineDist = VectorUtil.DistanceToLine(v,verts[prevIndex],verts[i]);;
                float d = VectorUtil.DistanceToEdge(v,verts[prevIndex],verts[i]);
                if (d == dMin) { // handle degenerate case for point-in-polygon test
                    if (lineDist > lastLineDist) {
                        dMin = d;
                        i0 = prevIndex;
                        i1 = i;
                    }
                } else if (d < dMin) {
                    dMin = d;
                    i0 = prevIndex;
                    i1 = i;
                }
                lastLineDist = lineDist;
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
