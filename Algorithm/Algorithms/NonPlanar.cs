
// enable closed ring in triangle handling
// status: not working
#define HANDLE_CLOSED

// try to ignore triangles that could result in bad directional projections
#define EPSILON_TEST

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using static MeshUtils.Util;
using static MeshUtils.API;
using static MeshUtils.MeshingUtil;

namespace MeshUtils {

    static class NonPlanarAlgorithm {

        public static CutResult Run(
            GameObject target,
            CuttingTemplate template
        ) {

            Mesh mesh = target.GetComponent<MeshFilter>().mesh;
            MeshPart pos = new MeshPart(true), neg = new MeshPart(false);
            RingGenerator intersection_ring = new RingGenerator();

            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            int[] triangles = mesh.triangles;

            bool addNormals = normals.Length > 0;

            // Debug.Log(vertices.Length + " verts, " + triangles.Length / 3 + " tris, " + template.points.Count + "tpl. points");

            // divide mesh in half by vertices
            int i = 0;
            foreach (Vector3 vertex in vertices) {
                if (template.IsAbove(vertex)) {
                    pos.indexMap.Add(i,pos.vertices.Count);
                    pos.vertices.Add(vertex);
                    if (addNormals) pos.normals.Add(normals[i]);
                } else {
                    neg.indexMap.Add(i,neg.vertices.Count);
                    neg.vertices.Add(vertex);
                    if (addNormals) neg.normals.Add(normals[i]);
                }
                i++;
            }

            // RingGen's are associated to first template point
            //   these define the rings used for generating strip-wise inner geometry
            // SortedDictionary are for connecting edges of strip-wise inner geometry
            //   Sorted by signed distance to template plane, so they get connected in the right order
            Dictionary<Vector3,Tuple<SortedDictionary<float,Vector3>,RingGen>> point_data = new Dictionary<Vector3,Tuple<SortedDictionary<float,Vector3>,RingGen>>();

            foreach (Vector3 point in template.points) {
                point_data.Add(point,new Tuple<SortedDictionary<float,Vector3>,RingGen>(new SortedDictionary<float,Vector3>(),new RingGen()));
            }

            IvSaver ivSaver = new IvSaver();

            // put triangles in correct mesh
            for (i = 0; i < triangles.Length; i += 3) {

                // find orignal indices
                int i_a = triangles[i],
                    i_b = triangles[i+1],
                    i_c = triangles[i+2];
                
                // find original verticies
                Vector3 a = vertices[i_a],
                        b = vertices[i_b],
                        c = vertices[i_c];

                // seperation check
                if (!ProcessTriangle(template,a,b,c,point_data,pos,neg,intersection_ring,ivSaver,addNormals)) {
                    if (template.IsAbove(a)) {
                        // triangle above plane
                        pos.indices.Add(pos.indexMap[i_a]);
                        pos.indices.Add(pos.indexMap[i_b]);
                        pos.indices.Add(pos.indexMap[i_c]);
                    } else {
                        // triangle below plane
                        neg.indices.Add(neg.indexMap[i_a]);
                        neg.indices.Add(neg.indexMap[i_b]);
                        neg.indices.Add(neg.indexMap[i_c]);
                    }
                }
            }

            // Generate strip (inner) geometry
            SortedDictionary<float, Vector3> next_dist_map = point_data[template.points[template.isClosed ? 0 : template.points.Count - 1]].Item1;
            Vector3 next_point = template.isClosed ? template.points.First() : template.points.Last();
            for (i = template.points.Count - (template.isClosed ? 1 : 2); i >= 0; i--) {
                SortedDictionary<float, Vector3> dist_map = point_data[template.points[i]].Item1;
                Vector3 point = template.points[i];
                //Debugging.LogLine(template.points[i-1],template.normal);
                //Debugging.LogLine(template.points[i],template.normal);
                //Debugging.LogList(point_data[template.points[i-1]].Item1.Keys);
                //Debugging.LogList(point_data[template.points[i]].Item1.Keys);
                RingGen rg = point_data[template.points[i]].Item2;
                // Debug.Log("points: "+point+" -> "+next_point);
                //rg.MyDebugLog();
                rg.TemplateJoin(template,dist_map);
                rg.TemplateJoin(template,next_dist_map);
                //rg.MyDebugLog();
                foreach (Ring ring in rg.GetRings(false,false)) {
                    //Debugging.DebugRing(ring.verts);
                    Vector3 normal = Vector3.Cross(template.normal,next_point-point);
                    GenerateRingMesh(ring.verts,pos,normal,true,null,addNormals);
                    //TmpGen(ring.verts,pos,normal);
                    GenerateRingMesh(ring.verts,neg,normal,false,null,addNormals);
                    //TmpGen(ring.verts,neg,normal,true);
                }
                // Debug.Log("---------------------------");
                next_dist_map = dist_map;
                next_point = point;
            }

            // Debug.Log("template: "+template);

            return new CutResult(target, Vector3.zero, intersection_ring.GetRings(false,false), pos, neg);

        }

        private static bool ProcessTriangle(
            CuttingTemplate template,
            Vector3 a, Vector3 b, Vector3 c,
            Dictionary<Vector3,Tuple<SortedDictionary<float,Vector3>,RingGen>> point_data,
            MeshPart pos, MeshPart neg,
            RingGenerator intersection_ring,
            IvSaver ivSaver,
            bool addNormals
        ) {
            List<Vector3> points = template.points;
            Vector3 normal = template.normal;
            RingGenerator tri_rings = new RingGenerator();
            Vector3 tri_nor = -Vector3.Cross(c-a,c-b).normalized;
            if (tri_nor == Vector3.zero) {
                // Debug.LogWarning("zero area tri");
                return true;
            }
            bool partToUse = Vector3.Dot(tri_nor,normal) > 0;
            bool ring_dir = partToUse;
            MUPlane tri_plane = new MUPlane(tri_nor,a);
            Vector3 ab_nor = Vector3.Cross(tri_nor,(b-a).normalized), // vector perpendicular to edge and pointing away from triangle
                    bc_nor = Vector3.Cross(tri_nor,(c-b).normalized),
                    ca_nor = Vector3.Cross(tri_nor,(a-c).normalized);
        #if EPSILON_TEST
            if (Math.Abs(Vector3.Dot(tri_nor,normal)) < 0.01f) {
                Debug.LogWarning("epsilon ignore");
                return false;
            }
        #endif
            // Debug.Log("tri verts: "+a+" "+b+" "+c);
            // Debug.Log((b-a).magnitude+" "+(c-b).magnitude+" "+(a-c).magnitude);
            // Debug.Log("edge norms: "+ab_nor+" "+bc_nor+" "+ca_nor);
            Dictionary<float,Vector3> map_ab = new Dictionary<float, Vector3>(), // distance (from first vertex in edge) -> vertex in edge
                                    map_bc = new Dictionary<float, Vector3>(), // used for sorting... TODO: change to SortedDictionary
                                    map_ca = new Dictionary<float, Vector3>();
            HashSet<Vector3> exiting_ivs = new HashSet<Vector3>();
            MUPlane ab = new MUPlane(ab_nor,a), // planes passing through edges, perpendicular to triangle
                    bc = new MUPlane(bc_nor,b),
                    ca = new MUPlane(ca_nor,c);
            // Debug.Log("tri: "+tri_plane+" "+normal);
            //Debugging.DebugRing(points);
            Vector3 opi, old_point;
            bool oaab, oabc, oaca;
            SortedDictionary<float,Vector3> dist_map_old;
            RingGen strip_rings;
            if (template.isClosed) {
                opi = tri_plane.DirectionalProject(points.Last(),normal);
                old_point = points.Last();
                oaab = ab.IsAbove(opi);
                oabc = bc.IsAbove(opi);
                oaca = ca.IsAbove(opi);
                dist_map_old = point_data[points.Last()].Item1;
                strip_rings = point_data[points.Last()].Item2;
            } else {
                opi = tri_plane.DirectionalProject(points.First(),normal);
                old_point = points.First();
                oaab = ab.IsAbove(opi);
                oabc = bc.IsAbove(opi);
                oaca = ca.IsAbove(opi);
                dist_map_old = point_data[points.First()].Item1;
                strip_rings = point_data[points.First()].Item2;
            }
            bool oldInside = !(oaab||oabc||oaca);
            for (int i = template.isClosed ? 0 : 1; i < points.Count; i++) {
                Vector3 cur_point = points[i];
                SortedDictionary<float,Vector3> dist_map = point_data[cur_point].Item1;
                Vector3 pi = tri_plane.DirectionalProject(cur_point,normal);
                // Debug.Log(opi+" => "+pi);
                bool aab = ab.IsAbove(pi), // above means outside edge
                    abc = bc.IsAbove(pi),
                    aca = ca.IsAbove(pi);
                bool inside = !(aab||abc||aca);
                // Debug.Log(aab+" "+abc+" "+aca+" | "+oaab+" "+oabc+" "+oaca);
                if (inside != oldInside) {
                    // Debug.Log("EDGE PAIR: "+a+" "+b+" "+c+" pi-opi: "+pi+" "+opi+" hc: "+pi.GetHashCode()+" "+opi.GetHashCode());
                    // add edge pair
                    MUPlane edge_plane;
                    Dictionary<float,Vector3> map;
                    Vector3 ep1, ep2, iv;
                    if (inside) {
                        if (oaab) {
                            edge_plane = ab; map = map_ab; ep1 = a; ep2 = b;
                            iv = ivSaver.Intersect(edge_plane,ep1,ep2,opi,pi,old_point,cur_point);
                            if (Vector3.Dot(ep1-iv,ep2-iv) < 0) goto connect;
                        }
                        if (oabc) {
                            edge_plane = bc; map = map_bc; ep1 = b; ep2 = c;
                            iv = ivSaver.Intersect(edge_plane,ep1,ep2,opi,pi,old_point,cur_point);
                            if (Vector3.Dot(ep1-iv,ep2-iv) < 0) goto connect;
                        }
                        if (oaca) {
                            edge_plane = ca; map = map_ca; ep1 = c; ep2 = a;
                            iv = ivSaver.Intersect(edge_plane,ep1,ep2,opi,pi,old_point,cur_point);
                            if (Vector3.Dot(ep1-iv,ep2-iv) < 0) goto connect;
                        }
                        throw MeshUtilsException.Internal("Point on neither side of triangle");
                    connect:
                        // Debug.Log("EDGE PAIR: "+iv+" "+pi+" hc: "+iv.GetHashCode()+" "+pi.GetHashCode());
                        float dist_key = template.plane.SignedDistance(pi);
                        if (!dist_map.ContainsKey(dist_key)) dist_map.Add(dist_key,pi);
                        map.Add((ep1-iv).magnitude,iv);
                        strip_rings.AddConnected(ring_dir?iv:pi,ring_dir?pi:iv);
                        intersection_ring.AddConnected(ring_dir?iv:pi,ring_dir?pi:iv);
                        tri_rings.AddConnected(iv,pi);
                    } else {
                        // Debug.Log(a+" "+b+" "+c+" "+template_plane);
                        // Debug.Log(pa+" "+pb+" "+pc+" "+points[i-1]+" "+points[i]);
                        if (aab) {
                            edge_plane = ab; map = map_ab; ep1 = a; ep2 = b;
                            iv = ivSaver.Intersect(edge_plane,ep1,ep2,opi,pi,old_point,cur_point);
                            if (Vector3.Dot(ep1-iv,ep2-iv) < 0) goto connect;
                        }
                        if (abc) {
                            edge_plane = bc; map = map_bc; ep1 = b; ep2 = c;
                            iv = ivSaver.Intersect(edge_plane,ep1,ep2,opi,pi,old_point,cur_point);
                            if (Vector3.Dot(ep1-iv,ep2-iv) < 0) goto connect;
                        }
                        if (aca) {
                            edge_plane = ca; map = map_ca; ep1 = c; ep2 = a;
                            iv = ivSaver.Intersect(edge_plane,ep1,ep2,opi,pi,old_point,cur_point);
                            if (Vector3.Dot(ep1-iv,ep2-iv) < 0) goto connect;
                        }
                        throw MeshUtilsException.Internal("Point on neither side of triangle");
                    connect:
                        // Debug.Log("EDGE PAIR: "+iv+" "+opi+" hc: "+iv.GetHashCode()+" "+opi.GetHashCode());
                        float dist_key = template.plane.SignedDistance(opi);
                        if (!dist_map_old.ContainsKey(dist_key)) dist_map_old.Add(dist_key,opi);
                        map.Add((ep1-iv).magnitude,iv);
                        strip_rings.AddConnected(ring_dir?opi:iv,ring_dir?iv:opi);
                        intersection_ring.AddConnected(ring_dir?opi:iv,ring_dir?iv:opi);
                        tri_rings.AddConnected(opi,iv);
                        exiting_ivs.Add(iv);
                    }
                } else if (inside) {
                    // add inner pair
                    // Debug.Log("INNER PAIR: "+a+" "+b+" "+c+" pi-opi: "+pi+" "+opi+" hc: "+pi.GetHashCode()+" "+opi.GetHashCode());
                    // Debug.Log("INNER PAIR: "+pi+" "+opi+" hc: "+pi.GetHashCode()+" "+opi.GetHashCode());
                    strip_rings.AddConnected(ring_dir?opi:pi,ring_dir?pi:opi);
                    intersection_ring.AddConnected(ring_dir?opi:pi,ring_dir?pi:opi);
                    tri_rings.AddConnected(opi,pi);
                    float dist_key = template.plane.SignedDistance(pi);
                    if (!dist_map.ContainsKey(dist_key)) dist_map.Add(dist_key,pi);
                    dist_key = template.plane.SignedDistance(opi);
                    if (!dist_map_old.ContainsKey(dist_key)) dist_map_old.Add(dist_key,opi);
                } else {
                    // add outer pair
                    if ( // test to check if edge does not cross triangle
                        (aab && oaab) ||
                        (abc && oabc) ||
                        (aca && oaca)
                    ) goto continue_for;
                    // crosses triangle
                    if (!aab && !oaab) { // not ab
                        Vector3 iv0 = ivSaver.Intersect(bc,b,c,opi,pi,old_point,cur_point),
                                iv1 = ivSaver.Intersect(ca,c,a,opi,pi,old_point,cur_point);
                        if (
                            Vector3.Dot(b-iv0,c-iv0) > 0 ||
                            Vector3.Dot(c-iv1,a-iv1) > 0
                        ) goto continue_for;
                        // Debug.Log("OUTER PAIR: "+iv0+" "+iv1+" hc: "+iv0.GetHashCode()+" "+iv1.GetHashCode());
                        map_bc.Add((b-iv0).magnitude,iv0);
                        map_ca.Add((c-iv1).magnitude,iv1);
                        bool iv0_first = (iv0-opi).magnitude < (iv1-opi).magnitude;
                        exiting_ivs.Add(iv0_first?iv1:iv0);
                        tri_rings.AddConnected(iv0_first?iv0:iv1,iv0_first?iv1:iv0);
                        strip_rings.AddConnected((!ring_dir^iv0_first)?iv0:iv1,(!ring_dir^iv0_first)?iv1:iv0);
                        intersection_ring.AddConnected((!ring_dir^iv0_first)?iv0:iv1,(!ring_dir^iv0_first)?iv1:iv0);
                    } else if (!abc && !oabc) { // not bc
                        Vector3 iv0 = ivSaver.Intersect(ca,c,a,opi,pi,old_point,cur_point),
                                iv1 = ivSaver.Intersect(ab,a,b,opi,pi,old_point,cur_point);
                        if (
                            Vector3.Dot(c-iv0,a-iv0) > 0 ||
                            Vector3.Dot(a-iv1,b-iv1) > 0
                        ) goto continue_for;
                        // Debug.Log("OUTER PAIR: "+iv0+" "+iv1+" hc: "+iv0.GetHashCode()+" "+iv1.GetHashCode());
                        map_ca.Add((c-iv0).magnitude,iv0);
                        map_ab.Add((a-iv1).magnitude,iv1);
                        bool iv0_first = (iv0-opi).magnitude < (iv1-opi).magnitude;
                        exiting_ivs.Add(iv0_first?iv1:iv0);
                        tri_rings.AddConnected(iv0_first?iv0:iv1,iv0_first?iv1:iv0);
                        strip_rings.AddConnected((!ring_dir^iv0_first)?iv0:iv1,(!ring_dir^iv0_first)?iv1:iv0);
                        intersection_ring.AddConnected((!ring_dir^iv0_first)?iv0:iv1,(!ring_dir^iv0_first)?iv1:iv0);
                    } else if (!aca && !oaca) { // not ca
                        Vector3 iv0 = ivSaver.Intersect(ab,a,b,opi,pi,old_point,cur_point),
                                iv1 = ivSaver.Intersect(bc,b,c,opi,pi,old_point,cur_point);
                        if (
                            Vector3.Dot(a-iv0,b-iv0) > 0 ||
                            Vector3.Dot(b-iv1,c-iv1) > 0
                        ) goto continue_for;
                        // Debug.Log("OUTER PAIR: "+iv0+" "+iv1+" hc: "+iv0.GetHashCode()+" "+iv1.GetHashCode());
                        map_ab.Add((a-iv0).magnitude,iv0);
                        map_bc.Add((b-iv1).magnitude,iv1);
                        bool iv0_first = (iv0-opi).magnitude < (iv1-opi).magnitude;
                        exiting_ivs.Add(iv0_first?iv1:iv0);
                        tri_rings.AddConnected(iv0_first?iv0:iv1,iv0_first?iv1:iv0);
                        strip_rings.AddConnected((!ring_dir^iv0_first)?iv0:iv1,(!ring_dir^iv0_first)?iv1:iv0);
                        intersection_ring.AddConnected((!ring_dir^iv0_first)?iv0:iv1,(!ring_dir^iv0_first)?iv1:iv0);
                    } else { // opposite sides
                        MUPlane edge_plane, edge_plane1;
                        Dictionary<float,Vector3> map, map1;
                        Vector3 iv, iv1; // iv is entry, must be one side; iv1 is exit, can be either two other sides
                        float iv_mag, iv1_mag;
                        bool swap_ring_dir = false;

                        if (aab && !abc && !aca || oaab && !oabc && !oaca) {
                            // Debug.Log("1 swap "+oaab);
                            if (oaab) swap_ring_dir = true;
                            edge_plane = ab;
                            map = map_ab;
                            iv = ivSaver.Intersect(ab,a,b,opi,pi,old_point,cur_point);
                            iv_mag = (a-iv).magnitude;
                            if (Vector3.Dot(a-iv,b-iv) > 0) goto continue_for;
                            iv1 = ivSaver.Intersect(bc,b,c,opi,pi,old_point,cur_point);
                            if (Vector3.Dot(b-iv1,c-iv1) > 0) {
                                iv1 = ivSaver.Intersect(ca,c,a,opi,pi,old_point,cur_point);
                                iv1_mag = (c-iv1).magnitude;
                                edge_plane1 = ca;
                                map1 = map_ca;
                            } else {
                                iv1_mag = (b-iv1).magnitude;
                                edge_plane1 = bc;
                                map1 = map_bc;
                            }
                            goto connect_opposite;
                        } else if (!aab && abc && !aca || !oaab && oabc && !oaca) {
                            // Debug.Log("2 swap "+oabc);
                            if (oabc) swap_ring_dir = true;
                            edge_plane = bc;
                            map = map_bc;
                            iv = ivSaver.Intersect(bc,b,c,opi,pi,old_point,cur_point);
                            iv_mag = (b-iv).magnitude;
                            if (Vector3.Dot(b-iv,c-iv) > 0) goto continue_for;
                            iv1 = ivSaver.Intersect(ca,c,a,opi,pi,old_point,cur_point);
                            if (Vector3.Dot(c-iv1,a-iv1) > 0) {
                                iv1 = ivSaver.Intersect(ab,a,b,opi,pi,old_point,cur_point);
                                iv1_mag = (a-iv1).magnitude;
                                edge_plane1 = ab;
                                map1 = map_ab;
                            } else {
                                iv1_mag = (c-iv1).magnitude;
                                edge_plane1 = ca;
                                map1 = map_ca;
                            }
                            goto connect_opposite;
                        } else if (!aab && !abc && aca || !oaab && !oabc && oaca) {
                            // Debug.Log("3 swap "+oaca);
                            if (oaca) swap_ring_dir = true;
                            edge_plane = ca;
                            map = map_ca;
                            iv = ivSaver.Intersect(ca,c,a,opi,pi,old_point,cur_point);
                            iv_mag = (c-iv).magnitude;
                            if (Vector3.Dot(c-iv,a-iv) > 0) goto continue_for;
                            iv1 = ivSaver.Intersect(ab,a,b,opi,pi,old_point,cur_point);
                            if (Vector3.Dot(a-iv1,b-iv1) > 0) {
                                iv1 = ivSaver.Intersect(bc,b,c,opi,pi,old_point,cur_point);
                                iv1_mag = (b-iv1).magnitude;
                                edge_plane1 = bc;
                                map1 = map_bc;
                            } else {
                                iv1_mag = (a-iv1).magnitude;
                                edge_plane1 = ab;
                                map1 = map_ab;
                            }
                            goto connect_opposite;
                        }

                        throw MeshUtilsException.Internal("Case exhaustion failed");
                    connect_opposite:
                        // Debug.Log("CROSS PAIR: "+iv+" "+iv1+" hc: "+iv.GetHashCode()+" "+iv1.GetHashCode());
                        // Debug.Log("yay "+opi+" "+pi+" "+a+" "+b+" "+c+" "+aab+" "+abc+" "+aca+" "+oaab+" "+oabc+" "+oaca);
                        map.Add(iv_mag,iv);
                        map1.Add(iv1_mag,iv1);
                        exiting_ivs.Add(swap_ring_dir?iv1:iv);
                        // Debug.Log("exiting is "+(swap_ring_dir?iv1:iv));
                        strip_rings.AddConnected((!ring_dir^swap_ring_dir)?iv:iv1,(!ring_dir^swap_ring_dir)?iv1:iv);
                        intersection_ring.AddConnected((!ring_dir^swap_ring_dir)?iv:iv1,(!ring_dir^swap_ring_dir)?iv1:iv);
                        tri_rings.AddConnected(swap_ring_dir?iv:iv1,swap_ring_dir?iv1:iv);
                    }
                }
            continue_for:
                old_point = cur_point;
                oldInside = inside;
                oaab = aab;
                oabc = abc;
                oaca = aca;
                opi = pi;
                dist_map_old = dist_map;
                strip_rings = point_data[points[i]].Item2;
            }
            if (tri_rings.HasComplete()) {
            #if !HANDLE_CLOSED
                throw MeshUtilsException.Internal("Template closed and within triangle not handled");
            #else
                // Debug.Log("inside");
                if (tri_rings.HasPartials()) throw MeshUtilsException.Internal("Unexpected condition");
                List<Ring> ring = tri_rings.GetRings(false, false);
                if (ring.Count != 1) throw MeshUtilsException.Internal("Multiple rings in single triangle");
                Debugging.DebugRing(ring[0].verts);
                GenerateRingMesh(ring[0].verts,partToUse?neg:pos,tri_nor,true,null,addNormals);

                var r0 = new Ring(new List<Vector3>() {a,b,c});
                var r1 = ring[0];

                r1.verts.Reverse();

                // join indices
                int i0 = 0, i1 = 0;
                r0.RingDist(r1, ref i0, ref i1);
                
                // the inner ring should have the opposite direction
                // join by inserting r1 at i0 in r0
                List<Vector3> res = new List<Vector3>();
                res.AddRange(r0.verts.GetRange(0,i0+1));      // r0 from start to split
                res.AddRange(r1.verts.GetRange(i1,r1.verts.Count-i1));  // r1 from split to end
                res.AddRange(r1.verts.GetRange(0,i1+1));            // r1 from start to split
                res.AddRange(r0.verts.GetRange(i0,r0.verts.Count-i0));  // r0 from split to end

                // Debugging.DebugRing(res);

                GenerateRingMesh(res,partToUse?pos:neg,tri_nor,true,null,addNormals);
                return true;
            #endif
            }
            if (!tri_rings.HasPartials()) return false;
            // Debugging.DebugRing(points.ConvertAll(p=>tri_plane.DirectionalProject(p,normal)));
            // Debug.Log("tri verts:"+a+" "+b+" "+c);
            // tri_rings.MyDebugLog();
            // Debug.Log("connecting ivs...");
            RingGenerator inv_tri_rings = tri_rings.Duplicate(); // seperate rings needed for other MeshPart
            ConnectIVs(exiting_ivs,a,b,c,map_ab,map_ca,map_bc,tri_rings,inv_tri_rings); // connect edges around in triangle rings
            ConnectIVs(exiting_ivs,b,c,a,map_bc,map_ab,map_ca,tri_rings,inv_tri_rings); // one call per edge
            ConnectIVs(exiting_ivs,c,a,b,map_ca,map_bc,map_ab,tri_rings,inv_tri_rings);
            // Debug.Log("gen:");
            //tri_rings.MyDebugLog();
            foreach (var ring in tri_rings.GetRings(false, false)) {
                // Debugging.DebugRing(ring.verts);
                GenerateRingMesh(ring.verts,partToUse?neg:pos,tri_nor,true,null,addNormals);
            }
            // Debug.Log("gen inv:");
            //inv_tri_rings.MyDebugLog();
            foreach (var ring in inv_tri_rings.GetRings(false, false)) {
                //Debugging.DebugRing(ring.verts);
                ring.verts.Reverse();
                GenerateRingMesh(ring.verts,partToUse?pos:neg,tri_nor,true,null,addNormals);
            }
            //Debug.Log("--------------- END PROCESS TRIANGLE");
            return true;
        }

        // ----------------------------
        // Complete rings in triangle
        // ----------------------------
        private static void ConnectIVs(
            HashSet<Vector3> exiting_ivs,
            Vector3 ep1, Vector3 ep2, Vector3 oep,
            Dictionary<float,Vector3> map,
            Dictionary<float,Vector3> prev_map,
            Dictionary<float,Vector3> next_map,
            RingGenerator rings, RingGenerator rings2
        ) {
            if (map.Count == 0) return;
            List<float> keyList = new List<float>(map.Keys);
            keyList.Sort();
            int oneIfFirstIsEntry = 0;
            // Debugging.LogList(keyList);
            // check first is exit or entry
            // connect in relevant ring generator
            if (!exiting_ivs.Contains(map[keyList[0]])) {
                // Debug.Log("First is entry");
                // connect first edge point to first entry
                rings.AddConnected(ep1,map[keyList[0]]);
                oneIfFirstIsEntry = 1;
                // if previous are empty, connect around
                if (prev_map.Count == 0) {
                    rings.AddConnected(oep,ep1);
                }
            } else {
                // Debug.Log("First is exit");
                rings2.AddConnected(map[keyList[0]],ep1);
                if (prev_map.Count == 0) {
                    rings2.AddConnected(ep1,oep);
                }
            }
            // connect inner
            int i;
            for (i = 1; i < keyList.Count; i++) {
                // Debug.Log(map[keyList[i-1]]+" "+map[keyList[i]]);
                if (i % 2 != oneIfFirstIsEntry)
                    rings.AddConnected(map[keyList[i-1]],map[keyList[i]]);
                else
                    rings2.AddConnected(map[keyList[i]],map[keyList[i-1]]);
            }
            // check exit or entrance
            // connect to edge point in relevant ring generator
            i = keyList.Count - 1;
            if (exiting_ivs.Contains(map[keyList[i]])) {
                //Debug.Log("last was exit");
                rings.AddConnected(map[keyList[i]],ep2);
                if (next_map.Count == 0 && prev_map.Count == 0) {
                    rings.AddConnected(ep2,oep);
                }
            } else {
                // Debug.Log("last was entry");
                rings2.AddConnected(ep2,map[keyList[i]]);
                if (next_map.Count == 0 && prev_map.Count == 0) {
                    rings2.AddConnected(oep,ep2);
                }
            }
        }

        class IvSaver {

            struct DualKey {
                public Vector3 a,b;
                public DualKey(Vector3 a, Vector3 b) {
                    bool swap = (
                        b.x > a.x ||
                        (b.x == a.x && b.y > a.y) || 
                        (b.y == a.y && b.z > a.z)
                    );
                    this.a = swap ? b : a;
                    this.b = swap ? a : b;
                }
            }

            private readonly Dictionary<Tuple<DualKey, DualKey>,Vector3> ivs = new Dictionary<Tuple<DualKey, DualKey>, Vector3>();

            public Vector3 Intersect(
                MUPlane p,
                Vector3 e0, Vector3 e1, // edge points
                Vector3 pp0, Vector3 pp1, // projected points for calculation
                Vector3 p0, Vector3 p1 // template points
            ) {
                var key = new Tuple<DualKey, DualKey>(new DualKey(e0,e1),new DualKey(p0,p1));
                //var x = " e0e1 hc: "+key.Item1.a.GetHashCode()+" "+key.Item1.b.GetHashCode()+" p0p1 hc: "+key.Item2.a.GetHashCode()+" "+key.Item2.b.GetHashCode();
                if (ivs.ContainsKey(key)) {
                    //Debug.Log("recovered iv: "+ivs[key]+x);
                    return ivs[key];
                }
                Vector3 iv = p.Intersection(pp0,pp1);
                //Debug.Log("missed iv: "+iv+x);
                ivs[key] = iv;
                return iv;
            }

        }

        class RingGen : RingGenerator {

            // ------------------------
            // Connect rings in strip
            // ------------------------
            public void TemplateJoin(CuttingTemplate template,SortedDictionary<float,Vector3> map) {
                if (map.Count == 0) return;
                if (map.Count % 2 == 1) throw MeshUtilsException.Internal("Odd strip entry/exit count");
                bool first_is_entry = false;
                Vector3 first_vec = map.First().Value;
                foreach (List<Vector3> part in partials) {
                    if (part[0] != first_vec) continue;
                    first_is_entry = true;
                    break;
                }
                var x = map.Values.GetEnumerator();
                for (int i = 0; i < map.Count; i+=2) {
                    if (!x.MoveNext()) throw MeshUtilsException.Internal("Enumerator exhausted");
                    Vector3 a = x.Current;
                    if (!x.MoveNext()) throw MeshUtilsException.Internal("Enumerator exhausted");
                    Vector3 b = x.Current;
                    AddConnected(first_is_entry?b:a,first_is_entry?a:b);
                }
            }

        }

    }

}