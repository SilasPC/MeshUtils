
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshUtils {

    class CuttingTemplate {

        public static CuttingTemplate InLocalSpace(Vector3 normal, Vector3 pointInPlane, Transform transform) {
            Vector3 world_normal = transform.TransformDirection(normal),
                    world_point = transform.TransformPoint(pointInPlane);
            return new CuttingTemplate(normal, pointInPlane,world_normal,world_point);
        }

        public static CuttingTemplate InWorldSpace(Vector3 normal, Vector3 pointInPlane) {
            return new CuttingTemplate(normal, pointInPlane, normal, pointInPlane);
        }

        public CuttingTemplate ToLocalSpace(Transform transform) {
            var t = new CuttingTemplate(
                transform.InverseTransformDirection(worldNormal),
                transform.InverseTransformPoint(world_pointInPlane),
                worldNormal,
                world_pointInPlane
            );
            foreach (Vector3 p in points)
                t.AddPoint(transform.InverseTransformPoint(p)); // tmp. p is assumed to be in world space
            return t;
        }

        public readonly List<Vector3> points = new List<Vector3>();
        private readonly MUPlane plane;
        public readonly Vector3 pointInPlane, normal;
        private readonly Vector3 world_pointInPlane, worldNormal;

        private CuttingTemplate(
            Vector3 normal, Vector3 pointInPlane,
            Vector3 worldNormal, Vector3 world_pointInPlane
        ) {
            this.worldNormal = worldNormal;
            this.world_pointInPlane = world_pointInPlane;
            this.pointInPlane = pointInPlane;
            this.normal = normal.normalized;
            this.plane = new MUPlane(this.normal,pointInPlane);
        }

        // ----------------------------------------------------------
        // Checks if a point is on the positive side of the template
        // ----------------------------------------------------------
        public bool IsAbove(Vector3 v) {
            Vector3 pv = plane.Project(v);
            float dMin = float.PositiveInfinity;
            int i0 = 0;
            for (int i = 0; i < points.Count - 1; i++) {
                float d = VectorUtil.DistanceToEdge(pv,points[i],points[i+1]);
                if (d < dMin) {
                    i0 = i;
                    dMin = d;
                }
            }
            return Vector3.Dot(
                Vector3.Cross(
                    points[i0]-pv,
                    points[i0+1]-points[i0]
                ),
                normal
            ) > 0;
        }

        public void AddPoint(Vector3 p) {
            p = plane.Project(p);
            if (points.Count > 1) {
                Vector3 dif = points[points.Count-1] - points[points.Count-2];
                if (Vector3.Angle(dif,p-points[points.Count-1]) < 0) return;
            }
            if (points.Count > 0) {
                Vector3 last = points[points.Count-1];
                if ((last-p).magnitude < 0.1f) return;
            }
            points.Add(p);
        }

        public void Draw() {
            foreach(Vector3 p in points)
                Debug.DrawRay(p-normal,normal * 2,Color.red,1,true);
            for (int i = 0; i < points.Count - 1; i++) {
                Debug.DrawRay(points[i]+normal,points[i+1]-points[i],Color.red,1,true);
                Debug.DrawRay(points[i],points[i+1]-points[i],Color.red,1,true);
                Debug.DrawRay(points[i]-normal,points[i+1]-points[i],Color.red,1,true);
            }
        }

        public void PrepareTmp() {
            Vector3 dif = points[0] - points[1];
            points.Insert(0,points[0]+5*dif);
            dif = points[points.Count-1] - points[points.Count-2];
            points.Add(points[points.Count-1]+5*dif);
        }

        public void ProcessTriangle(
            Vector3 a, Vector3 b, Vector3 c,
            RingGenerator rings,
            Util.MeshPart part, Util.MeshPart part2
        ) {
            RingGenerator self_rings = new RingGenerator();
            Vector3 tri_nor = -Vector3.Cross(c-a,c-b);
            MUPlane tri_plane = new MUPlane(tri_nor,a); // rounding errors are inbound here
            Vector3 ab_nor = Vector3.Cross(tri_nor,b-a),
                    bc_nor = Vector3.Cross(tri_nor,c-b),
                    ca_nor = Vector3.Cross(tri_nor,a-c);
            Dictionary<float,Vector3> map_ab = new Dictionary<float, Vector3>(),
                                    map_bc = new Dictionary<float, Vector3>(),
                                    map_ca = new Dictionary<float, Vector3>();
            HashSet<Vector3> exiting_ivs = new HashSet<Vector3>();
            MUPlane ab = new MUPlane(ab_nor,a),
                    bc = new MUPlane(bc_nor,b),
                    ca = new MUPlane(ca_nor,c);
            //Debug.Log(tri_plane+" "+normal);
            //Debugging.DebugRing(points);
            Vector3 opi = tri_plane.DirectionalProject(points[0],normal);
            bool oaab = ab.IsAbove(opi),
                oabc = bc.IsAbove(opi),
                oaca = ca.IsAbove(opi);
            bool oldInside = !(oaab||oabc||oaca);
            for (int i = 1; i < points.Count; i++) {
                Vector3 pi = tri_plane.DirectionalProject(points[i],normal);
                // Debug.Log(opi+" => "+pi);
                bool aab = ab.IsAbove(pi),
                    abc = bc.IsAbove(pi),
                    aca = ca.IsAbove(pi);
                bool inside = !(aab||abc||aca);
                // Debug.Log(aab+" "+abc+" "+aca+" | "+oaab+" "+oabc+" "+oaca);
                if (inside != oldInside) {
                    // add edge pair
                    MUPlane edge_plane;
                    Dictionary<float,Vector3> map;
                    Vector3 ep1, ep2, iv;
                    if (inside) {
                        if (oaab) {
                            edge_plane = ab; map = map_ab; ep1 = a; ep2 = b;
                            iv = edge_plane.Intersection(opi,pi);
                            if (Vector3.Dot(ep1-iv,ep2-iv) < 0) goto connect;
                        }
                        if (oabc) {
                            edge_plane = bc; map = map_bc; ep1 = b; ep2 = c;
                            iv = edge_plane.Intersection(opi,pi);
                            if (Vector3.Dot(ep1-iv,ep2-iv) < 0) goto connect;
                        }
                        if (oaca) {
                            edge_plane = ca; map = map_ca; ep1 = c; ep2 = a;
                            iv = edge_plane.Intersection(opi,pi);
                            if (Vector3.Dot(ep1-iv,ep2-iv) < 0) goto connect;
                        }
                        throw OperationException.Internal("Point on neither side of triangle");
                    connect:
                        map.Add((ep1-iv).magnitude,iv);
                        //rings.AddConnected(iv,pi);
                        self_rings.AddConnected(iv,pi);
                    } else {
                        //Debug.Log(a+" "+b+" "+c+" "+template_plane);
                        //Debug.Log(pa+" "+pb+" "+pc+" "+points[i-1]+" "+points[i]);
                        if (aab) {
                            edge_plane = ab; map = map_ab; ep1 = a; ep2 = b;
                            iv = edge_plane.Intersection(opi,pi);
                            if (Vector3.Dot(ep1-iv,ep2-iv) < 0) goto connect;
                        }
                        if (abc) {
                            edge_plane = bc; map = map_bc; ep1 = b; ep2 = c;
                            iv = edge_plane.Intersection(opi,pi);
                            if (Vector3.Dot(ep1-iv,ep2-iv) < 0) goto connect;
                        }
                        if (aca) {
                            edge_plane = ca; map = map_ca; ep1 = c; ep2 = a;
                            iv = edge_plane.Intersection(opi,pi);
                            if (Vector3.Dot(ep1-iv,ep2-iv) < 0) goto connect;
                        }
                        throw OperationException.Internal("Point on neither side of triangle");
                    connect:
                        map.Add((ep1-iv).magnitude,iv);
                        //rings.AddConnected(opi,iv);
                        self_rings.AddConnected(opi,iv);
                        exiting_ivs.Add(iv);
                    }
                } else if (inside) {
                    // add inner pair
                    //rings.AddConnected(opi,pi);
                    self_rings.AddConnected(opi,pi);
                } else {
                    // add outer pair
                    if ( // test to check if edge does not cross triangle
                        (aab && oaab) ||
                        (abc && oabc) ||
                        (aca && oaca)
                    ) goto continue_for;
                    // crosses triangle
                    if (!aab && !oaab) {
                        Vector3 iv0 = bc.Intersection(opi,pi),
                                iv1 = ca.Intersection(opi,pi);
                        if (
                            Vector3.Dot(b-iv0,c-iv0) > 0 ||
                            Vector3.Dot(c-iv1,a-iv1) > 0
                        ) goto continue_for;
                        map_bc.Add((b-iv0).magnitude,iv0);
                        map_ca.Add((c-iv1).magnitude,iv1);
                        bool iv0_first = (iv0-opi).magnitude < (iv1-opi).magnitude;
                        exiting_ivs.Add(iv0_first?iv1:iv0);
                        self_rings.AddConnected(iv0_first?iv0:iv1,iv0_first?iv1:iv0);
                    } else if (!abc && !oabc) {
                        Vector3 iv0 = ca.Intersection(opi,pi),
                                iv1 = ab.Intersection(opi,pi);
                        if (
                            Vector3.Dot(c-iv0,a-iv0) > 0 ||
                            Vector3.Dot(a-iv1,b-iv1) > 0
                        ) goto continue_for;
                        map_ca.Add((c-iv0).magnitude,iv0);
                        map_ab.Add((a-iv1).magnitude,iv1);
                        bool iv0_first = (iv0-opi).magnitude < (iv1-opi).magnitude;
                        exiting_ivs.Add(iv0_first?iv1:iv0);
                        self_rings.AddConnected(iv0_first?iv0:iv1,iv0_first?iv1:iv0);
                    } else if (!aca && !oaca) {
                        Vector3 iv0 = ab.Intersection(opi,pi),
                                iv1 = bc.Intersection(opi,pi);
                        if (
                            Vector3.Dot(a-iv0,b-iv0) > 0 ||
                            Vector3.Dot(b-iv1,c-iv1) > 0
                        ) goto continue_for;
                        map_ab.Add((a-iv0).magnitude,iv0);
                        map_bc.Add((b-iv1).magnitude,iv1);
                        bool iv0_first = (iv0-opi).magnitude < (iv1-opi).magnitude;
                        exiting_ivs.Add(iv0_first?iv1:iv0);
                        self_rings.AddConnected(iv0_first?iv0:iv1,iv0_first?iv1:iv0);
                    } else {
                        Debug.LogError("case not handled");
                    }
                }
            continue_for:
                oldInside = inside;
                oaab = aab;
                oabc = abc;
                oaca = aca;
                opi = pi;
            }
            if (self_rings.GetPartials().Count == 0) {
                Debug.Log("dropped");
                return;
            }
            //Debugging.DebugRing(points.ConvertAll(p=>tri_plane.DirectionalProject(p,normal)));
            Debug.Log(a+" "+b+" "+c);
            //self_rings.MyDebugLog();
            RingGenerator self_rings2 = self_rings.Duplicate();
            ConnectIVs2(exiting_ivs,a,b,c,map_ab,map_ca,map_bc,self_rings,self_rings2);
            ConnectIVs2(exiting_ivs,b,c,a,map_bc,map_ab,map_ca,self_rings,self_rings2);
            ConnectIVs2(exiting_ivs,c,a,b,map_ca,map_bc,map_ab,self_rings,self_rings2);
            self_rings2.MyDebugLog();
            bool partToUse = Vector3.Dot(tri_nor,normal) > 0;
            try {
                foreach (var ring in self_rings.GetRings()) {
                    //Debugging.DebugRing(ring.verts);
                    TmpGen(ring.verts,partToUse?part:part2,tri_nor);
                }
                foreach (var ring in self_rings2.GetRings()) {
                    // Debugging.DebugRing(ring.verts);
                    ring.verts.Reverse();
                    TmpGen(ring.verts,partToUse?part2:part,tri_nor);
                }
            } catch (Exception e) {
                Debug.LogException(e);
            }
            Debug.Log("---------------");
        }

        private void ConnectIVs(
            HashSet<Vector3> exiting_ivs,
            Vector3 ep1, Vector3 ep2, Vector3 oep,
            Dictionary<float,Vector3> map,
            Dictionary<float,Vector3> prev_map,
            Dictionary<float,Vector3> prev2_map,
            RingGenerator ring, 
            RingGenerator _
        ) {
            List<float> keyList = new List<float>(map.Keys);
            keyList.Sort();
            //Debugging.LogList(keyList);
            // check first is entry
            if (keyList.Count > 0 && !exiting_ivs.Contains(map[keyList[0]])) {
                // connect first edge point to first entry
                ring.AddConnected(ep1,map[keyList[0]]);
                keyList.RemoveAt(0);
                // if previous are empty, connect around
                if (prev_map.Count == 0) {
                    ring.AddConnected(oep,ep1);
                    if (prev2_map.Count == 0) {
                        ring.AddConnected(ep2,oep);
                    }
                }
            }
            int i;
            for (i = 1; i < keyList.Count; i+=2) {

                ring.AddConnected(map[keyList[i-1]],map[keyList[i]]);
            }
            // check exit
            if (keyList.Count == i && exiting_ivs.Contains(map[keyList[i-1]])) {
                // connect last exit to last edge point
                ring.AddConnected(map[keyList[i-1]],ep2);
            }
        }

        private void ConnectIVs2(
            HashSet<Vector3> exiting_ivs,
            Vector3 ep1, Vector3 ep2, Vector3 oep,
            Dictionary<float,Vector3> map,
            Dictionary<float,Vector3> prev_map,
            Dictionary<float,Vector3> prev2_map,
            RingGenerator rings, RingGenerator rings2
        ) {
            if (map.Count == 0) return;
            List<float> keyList = new List<float>(map.Keys);
            keyList.Sort();
            int oneIfFirstIsEntry = 0;
            //Debugging.LogList(keyList);
            // check first is exit or entry
            // connect in relevant ring generator
            if (!exiting_ivs.Contains(map[keyList[0]])) {
                // connect first edge point to first entry
                rings.AddConnected(ep1,map[keyList[0]]);
                oneIfFirstIsEntry = 1;
                // if previous are empty, connect around
                if (prev_map.Count == 0) {
                    rings.AddConnected(oep,ep1);
                }
            } else {
                rings2.AddConnected(map[keyList[0]],ep1);
                if (prev_map.Count == 0) {
                    rings2.AddConnected(ep1,oep);
                }
            }
            // connect inner
            int i;
            for (i = 1; i < keyList.Count; i++) {
                if (i % 2 == oneIfFirstIsEntry)
                    rings.AddConnected(map[keyList[i-1]],map[keyList[i]]);
                else
                    rings2.AddConnected(map[keyList[i]],map[keyList[i-1]]);
            }
            // check exit or entrance
            // connect to edge point in relevant ring generator
            i = keyList.Count - 1;
            if (exiting_ivs.Contains(map[keyList[i]])) {
                rings.AddConnected(map[keyList[i]],ep2);
            } else {
                rings2.AddConnected(ep2,map[keyList[i]]);
            }
        }

        private void TmpGen(
            List<Vector3> ring, Util.MeshPart part, Vector3 normal
        ) {

            // List<List<Vector3>> reduceHist = new List<List<Vector3>>();
            // reduceHist.Add(ring);

            int indStart = part.vertices.Count;
            part.vertices.AddRange(ring);

            List<Tuple<Vector3,int>> set = ring.ConvertAll(v=>new Tuple<Vector3,int>(v,indStart++));

            int i = -1 , lsc = set.Count;
            bool didInf = false;
            while (set.Count > 1) {
                i++;
                if (i >= set.Count) {
                    if (lsc == set.Count) {
                        if (didInf) {
                            // Debug.LogError("ear clipping failed");
                            // DebugRings(reduceHist);
                            // Debug.Log("normal "+(normal*1000));
                            // DebugRing(set.ConvertAll(s=>s.Item1));
                            throw OperationException.Internal("Ear clipping failed");
                        }
                        didInf = true;
                    } else didInf = false;
                    lsc = set.Count;
                }
                i %= set.Count;

                Tuple<Vector3,int> vi0 = set[i],
                                   vi1 = set[(i+1)%set.Count],
                                   vi2 = set[(i+2)%set.Count];
                Vector3 d1 = vi1.Item1 - vi0.Item1; // i0 -> i1
                Vector3 d2 = vi2.Item1 - vi0.Item1; // i0 -> i2

                // we assume points do not lie in a line (see SimplifyRing)
                // if they do however, it's just a waste of vertices

                // check convex ear
                float conv = Vector3.Dot(Vector3.Cross(d1,d2),normal);
                if (conv > 0) continue;

                // check that there are no other vertices inside this triangle
                if (set.Exists(v => {
                    // note: we must compare the vectors, otherwise coinciding vectors do not
                    // register as being the same
                    if (v.Item1 == vi0.Item1 || v.Item1 == vi1.Item1 || v.Item1 == vi2.Item1) return false;
                    return VectorUtil.CheckIsInside(d1,d2,v.Item1-vi0.Item1);
                })) continue;

                // generate indices
                if (part.side) {
                    part.indices.Add(vi0.Item2);
                    part.indices.Add(vi1.Item2);
                    part.indices.Add(vi2.Item2);
                } else {
                    part.indices.Add(vi0.Item2);
                    part.indices.Add(vi2.Item2);
                    part.indices.Add(vi1.Item2);
                }

                // eliminate vertex
                set.RemoveAt((++i)%set.Count);
                i++;

                // reduceHist.Add(set.ConvertAll(s=>s.Item1));

            }

        }

    }

}