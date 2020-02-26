
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshCutter {

    class API {

        public struct CutParams {
            public bool polySeperation;
            public bool destroyOriginal;
            public CutParams (bool polySeperation, bool destroyOriginal) {
                this.polySeperation = polySeperation;
                this.destroyOriginal = destroyOriginal;
            }
        }

        public struct CutResult {
            public readonly GameObject gameObject;
            public readonly int capturedVertexCount;
            public CutResult(GameObject gameObject, int capturedVertexCount) {
                this.gameObject = gameObject;
                this.capturedVertexCount = capturedVertexCount;
            }
        }

        public class MeshPart {
            public readonly List<Vector3> vertices = new List<Vector3>();
            public readonly List<int> indices = new List<int>();
            
            // Map from original indices to new indices
            public readonly Dictionary<int,int> indexMap = new Dictionary<int, int>();
            public readonly bool side;
            public MeshPart (bool side) {
                this.side = side;
            }
            public void AddMeshTo(GameObject obj) {
                var mesh = obj.AddComponent<MeshFilter>().mesh;
                mesh.vertices = vertices.ToArray();
                mesh.triangles = indices.ToArray();
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
            }

        }

        public static bool PerformCut(
            GameObject target,
            CuttingPlane cutting_plane,
            CutParams param,
            out List<CutResult> result
        ) {

            Mesh mesh = target.GetComponent<MeshFilter>().mesh;
            MeshPart pos = new MeshPart(true), neg = new MeshPart(false);
            RingGenerator rings = new RingGenerator();

            // divide mesh in half by vertices
            int i = 0;
            foreach (Vector3 vertex in mesh.vertices) {
                if (cutting_plane.IsAbove(vertex)) {
                    pos.indexMap.Add(i,pos.vertices.Count);
                    pos.vertices.Add(vertex);
                } else {
                    neg.indexMap.Add(i,neg.vertices.Count);
                    neg.vertices.Add(vertex);
                }
                i++;
            }

            // if either vertex list is empty the knife plane didn't collide
            if (pos.vertices.Count == 0 || neg.vertices.Count == 0) {
                result = null;
                return false;
            }

            int pos_cnt = pos.vertices.Count, neg_cnt = neg.vertices.Count;

            // put triangles in correct mesh
            for (i = 0; i < mesh.triangles.Length; i += 3) {

                // find orignal indices
                int i_a = mesh.triangles[i],
                    i_b = mesh.triangles[i+1],
                    i_c = mesh.triangles[i+2];
                
                // find original verticies
                Vector3 a = mesh.vertices[i_a],
                        b = mesh.vertices[i_b],
                        c = mesh.vertices[i_c];

                // seperation check
                bool aAbove = cutting_plane.IsAbove(a),
                    bAbove = cutting_plane.IsAbove(b),
                    cAbove = cutting_plane.IsAbove(c);

                if (aAbove && bAbove && cAbove) {
                    // triangle above plane
                    pos.indices.Add(pos.indexMap[i_a]);
                    pos.indices.Add(pos.indexMap[i_b]);
                    pos.indices.Add(pos.indexMap[i_c]);
                } else if (!aAbove && !bAbove && !cAbove) {
                    // triangle below plane
                    neg.indices.Add(neg.indexMap[i_a]);
                    neg.indices.Add(neg.indexMap[i_b]);
                    neg.indices.Add(neg.indexMap[i_c]);
                } else {
                    // triangle crosses plane
                    // call Util.GenTriangles 
                    if (aAbove == bAbove) {
                        // a, b, c
                        Util.GenTriangles(
                            cutting_plane,
                            aAbove ? pos : neg,
                            !aAbove ? pos : neg,
                            a, b, c, i_a, i_b, i_c,
                            rings
                        );
                    } else if (aAbove == cAbove) {
                        // c, a, b
                        Util.GenTriangles(
                            cutting_plane,
                            aAbove ? pos : neg,
                            !aAbove ? pos : neg,
                            c, a, b, i_c, i_a, i_b,
                            rings
                        );

                    } else if (bAbove == cAbove) {
                        // b, c, a (use bAbove)
                        Util.GenTriangles(
                            cutting_plane,
                            bAbove ? pos : neg,
                            !bAbove ? pos : neg,
                            b, c, a, i_b, i_c, i_a,
                            rings
                        );
                    }
                }
            }

            // discard cut if any incomplete rings were found
            //   as assumptions for mesh then do not hold
            if (rings.HasPartials()) {
                result = null;
                return false;
            }

            var reduced = Hierarchy.Analyse(rings.GetRings(), cutting_plane);

            // generate seperation meshing
            foreach (var ring in reduced) {
                Util.GenerateRingMesh(ring,pos,cutting_plane.normal);
                Util.GenerateRingMesh(ring,neg,cutting_plane.normal); 
            }

            result = new List<CutResult>();

            // create new objects
            if (param.polySeperation) {
                result.AddRange(PolySep(pos).ConvertAll(p=>CreateResult(p,pos_cnt,target)));
                result.AddRange(PolySep(neg).ConvertAll(p=>CreateResult(p,neg_cnt,target)));
            } else {
                result.Add(CreateResult(pos,pos_cnt,target));
                result.Add(CreateResult(neg,neg_cnt,target));
            }

            // destroy original object
            if (param.destroyOriginal) MonoBehaviour.Destroy(target);

            return true;
        }

        // for now just some default settings
        private static CutResult CreateResult(MeshPart part, int orig_verts, GameObject original) {
            GameObject obj = new GameObject();
            part.AddMeshTo(obj);
            MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
            obj.AddComponent<MeshCollider>().convex = true;
            Material newMat = renderer.material;
            Material oldMat = original.GetComponent<Renderer>().material;
            newMat.color = oldMat.color;
            newMat.mainTexture = oldMat.mainTexture;
            Transform transform = original.transform;
            obj.transform.position = transform.position;
            obj.transform.rotation = transform.rotation;
            obj.transform.localScale = transform.localScale;
            Rigidbody rb = obj.AddComponent<Rigidbody>();
            return new CutResult(obj,orig_verts);
        }
        
        private static List<MeshPart> PolySep(MeshPart mesh) {

            List<List<int>> list = new List<List<int>>();

            // create index groups
            for (int i = 0; i < mesh.indices.Count; i += 3) {
                int i0 = mesh.indices[i], i1 = mesh.indices[i+1], i2 = mesh.indices[i+2];
                Vector3 v0 = mesh.vertices[i0], v1 = mesh.vertices[i1], v2 = mesh.vertices[i2];
                AddIndices(list,mesh.vertices,v0,v1,v2,i0,i1,i2);
            }

            // Debug.Log("sets:"+list.Count);

            //MeshPart part = new MeshPart(mesh.side);
            //part.vertices.AddRange(mesh.vertices);
            //part.indices.AddRange(list[0]);
            //return new List<MeshPart>() {part};
            // create new meshes
            return list.ConvertAll(indices=>{

                MeshPart part = new MeshPart(mesh.side);

                foreach (int ind in indices) {
                    if (part.indexMap.ContainsKey(ind)) part.indices.Add(part.indexMap[ind]);
                    else {
                        Debug.Log(part.vertices.Count+" => "+ind);
                        part.indexMap.Add(ind,part.vertices.Count);
                        part.indices.Add(part.vertices.Count);
                        part.vertices.Add(mesh.vertices[ind]);
                    }
                }

                return part;

            });

            
        }

        private static void AddIndices(
            List<List<int>> list,
            List<Vector3> verts,
            Vector3 v0, Vector3 v1, Vector3 v2,
            int i0, int i1, int i2
        ) {

            List<int> l0 = null, l1 = null, l2 = null;

            // find the set that each vertex belongs to
            foreach (List<int> set in list) {
                foreach (int i in set){
                    Vector3 v = verts[i];
                    if (v == v0 && l0 == null)
                        l0 = set;
                    if (v == v1 && l1 == null)
                        l1 = set;
                    if (v == v2 && l2 == null)
                        l2 = set;
                }
                if (l0 != null && l1 != null && l2 != null) break;
            }

            // if no sets were found, make a new set
            if (l0 == null && l1 == null && l2 == null) {
                list.Add(new List<int>() {i0,i1,i2});
                return;
            }

            // this ensures that if we get lists A,B,A, then we do not add A onto A+B again
            List<int> mergedFrom = null;

            // merge l0 into l1
            if (l0 != null) {
                if (l1 == null) l1 = l0;
                else if (l0 != l1) {
                    list.Remove(l0);
                    l1.AddRange(l0);
                    mergedFrom = l0;
                }
            }

            // merge l1 into l2
            if (l1 != null) {
                if (l2 == null) l2 = l1;
                else if (l1 != l2 && mergedFrom != l2) {
                    list.Remove(l1);
                    l2.AddRange(l1);
                }
            }

            // add to l2
            l2.Add(i0);
            l2.Add(i1);
            l2.Add(i2);

        }

        private static string LogList(List<int> list) {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (int i in list) sb.Append(i.ToString()+" ");
            return sb.ToString();
        }

    }

}