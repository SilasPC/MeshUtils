
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshCutter {


    // -----------------------------------------------
    // Used to organize rings for further processing
    // -----------------------------------------------
    class Hierarchy {
        
        public static List<List<Vector3>> Analyse(List<List<Vector3>> rings, CuttingPlane plane) {
            List<Hierarchy> siblings = new List<Hierarchy>();
            List<Hierarchy> list = rings.ConvertAll(r => new Hierarchy(r/*, r.ConvertAll(v=>plane.Project(v))*/));
            // sort from largest to smallest
            // when looping forwards, if the next is not contained
            //   within any previous, it will never be contained,
            //   and as such can be considered a top-level sibling
            list.Sort(new HierarchySorter());
            foreach (Hierarchy h in list) {
                // try add hierarchy to siblings
                // if failed, add as new sibling
                //   (because no other siblings contain hierarchy)
                if (!siblings.Exists(sib => sib.TryAdd(h)))
                    siblings.Add(h);
            }
            List<List<Vector3>> result = new List<List<Vector3>>();
            // Debug.Log(siblings.Count + " top level siblings");
            siblings.ForEach(sib => result.AddRange(sib.Reduce()));
            return result;
        }

        /*private readonly Vector2
            // x,y minimum of ring bounding box
            min2 = new Vector2(float.PositiveInfinity,float.PositiveInfinity),
            // x,y maximum of ring bounding box
            max2 = new Vector2(float.NegativeInfinity,float.NegativeInfinity),
            // x,y size of ring bounding box
            size2;*/
        private readonly Vector3
            // x,y minimum of ring bounding box
            min = new Vector3(float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity),
            // x,y maximum of ring bounding box
            max = new Vector3(float.NegativeInfinity,float.NegativeInfinity,float.NegativeInfinity),
            // x,y size of ring bounding box
            size;
        private readonly List<Vector3> ring;
        // private readonly List<Vector2> projectedRing;
        // private Vector2 pointOutsideProjection;
        private readonly List<Hierarchy> children = new List<Hierarchy>();
        private Hierarchy (List<Vector3> ring/*, List<Vector2> projectedRing*/) {
            this.ring = ring;
            //this.projectedRing = projectedRing;
            /*foreach (Vector2 v in projectedRing) {
                if (v.x < min.x) min.x = v.x;
                if (v.y < min.y) min.y = v.y;
                if (v.x > max.x) max.x = v.x;
                if (v.y > max.y) max.y = v.y;
            }*/
            foreach (Vector3 v in ring) {
                if (v.x < min.x) min.x = v.x;
                if (v.y < min.y) min.y = v.y;
                if (v.z < min.z) min.z = v.z;
                if (v.x > max.x) max.x = v.x;
                if (v.y > max.y) max.y = v.y;
                if (v.z > max.z) max.z = v.z;
            }
            size = max-min;
            //pointOutsideProjection = min;
            // pointOutsideProjection.x -= 100;
        }

        // ------------------------------------
        // Reduce to simple rings recursively
        // ------------------------------------
        public List<List<Vector3>> Reduce() {
            List<List<Vector3>> res = new List<List<Vector3>>();
            List<Vector3> resRing = ring;
            // while sorting here is a decent solution,
            //   it is not complete, and it can be fooled.
            foreach (var child in ChildRingSorter.Sort(this)) {
                resRing = JoinRings(resRing,child.ring);
                res.AddRange(child.ReduceChildren());
            }
            res.Add(resRing);
            return res;
        }

        private List<List<Vector3>> ReduceChildren() {
            List<List<Vector3>> res = new List<List<Vector3>>();
            foreach (var child in children)
                res.AddRange(child.Reduce());
            return res;
        }

        // ------------------------------------------
        // Add hierarchy as child. Returns false
        //   if child is not contained by this
        // ------------------------------------------
        public bool TryAdd(Hierarchy h) {
            if (!this.Contains(h)) return false;
            if (children.Exists(child => child.TryAdd(h))) return true;
            children.Add(h);
            return true;
        }

        // -----------------------------------------------
        // Check if hierarchy is contained by this
        // Initial check is done with bounding boxes,
        // Complete check is done by checking
        //   if a point is strictly contained
        //   within the other ring (not implemented yet)
        // -----------------------------------------------
        public bool Contains(Hierarchy sub) {
            bool bb = 
                max.x > sub.max.x &&
                max.y > sub.max.y &&
                max.z > sub.max.z &&
                min.x < sub.min.x &&
                min.y < sub.min.y &&
                min.z < sub.min.z;
            if (!bb) return false;

            return true;

            // The following is a nescesary check that is disabled right now
            // It handles the edge case where a boundary check is not good enough (U shaped ring around another ring)
            // for now we assume this doesn't happen

            /*if (sub.projectedRing.Count > projectedRing.Count)
                return ContainsProjectedPoint(sub.projectedRing[0]);
                
            return sub.ContainsProjectedPoint(projectedRing[0]);*/

        }

        // ----------------------------------------------------------
        // Check if a point is contained within the projected ring
        // ----------------------------------------------------------
        /*private bool ContainsProjectedPoint(Vector2 p) {
            bool doesContain = false;
            int j = projectedRing.Count - 1;
            for (int i = 0; i < projectedRing.Count; i++) {
                if (Util.Vec2D.DoIntersect(
                    pointOutsideProjection,
                    p,
                    projectedRing[j],
                    projectedRing[i]
                )) doesContain = !doesContain;
            }
            return doesContain;
        }*/

        // --------------------------------------------------------
        // Check if hierarchy size-wise can be contained by this
        // --------------------------------------------------------
        public bool CanContain(Hierarchy sub) {
            return
                size.x > sub.size.x &&
                size.y > sub.size.y &&
                size.z > sub.size.z;
        }

        // --------------------------------------------------------
        // Join two rings (one inside the other) at nearest points
        // This will create a single concave ring
        // --------------------------------------------------------
        private List<Vector3> JoinRings(List<Vector3> r0, List<Vector3> r1) {
            // join indices
            int i0 = 0, i1 = 0;
            Util.RingDist(r0, r1, ref i0, ref i1);
            
            // the inner ring should have the opposite direction
            // join by inserting r1 at i0 in r0
            List<Vector3> res = new List<Vector3>();
            res.AddRange(r0.GetRange(0,i0+1));      // r0 from start to split
            res.AddRange(r1.GetRange(i1,r1.Count-i1));  // r1 from split to end
            res.AddRange(r1.GetRange(0,i1+1));            // r1 from start to split
            res.AddRange(r0.GetRange(i0,r0.Count-i0));  // r0 from split to end

            return res;
        }
            
        // ---------------------------------------------
        // Sort children by distance to parent ring
        // ---------------------------------------------
        private class ChildRingSorter : IComparer<Tuple<Hierarchy,float>> {

            public static List<Hierarchy> Sort(Hierarchy parent) {
                ChildRingSorter sorter = new ChildRingSorter(parent);
                int _ = 0;
                var tuples = parent
                    .children
                    .ConvertAll(c=>new Tuple<Hierarchy,float>(c,Util.RingDist(c.ring,parent.ring,ref _,ref _)));
                tuples.Sort(sorter);
                return tuples.ConvertAll(c=>c.Item1);
            }

            private readonly Hierarchy parent;
            private ChildRingSorter(Hierarchy parent) {
                this.parent = parent;
            }

            public int Compare(Tuple<Hierarchy,float> r0, Tuple<Hierarchy,float> r1) {
                return Math.Sign(r0.Item2 - r1.Item2);
            }

        }

    }
    
    // --------------------------------
    // Sort biggest hierarchies first
    // --------------------------------
    class HierarchySorter : IComparer<Hierarchy> {
        public int Compare(Hierarchy a, Hierarchy b) {
            return b.CanContain(a) ? 1 : 0;
        }
    }

}