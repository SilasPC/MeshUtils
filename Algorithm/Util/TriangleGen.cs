
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static MeshUtils.Util;

namespace MeshUtils {

    static class TriangleGen {

        // -----------------------------------------------------------
        // Generate triangles for an intersecting triangle a,b,c
        // It is assumed that a and b are in the positive half space,
        // and c is in the negative half space.
        // -----------------------------------------------------------
        public static void GenTriangles(
            CuttingPlane plane,
            MeshPart pos, MeshPart neg,
            Vector3 a, Vector3 b,Vector3 c,
            Vector2 txa, Vector2 txb, Vector2 txc,
            int i_a, int i_b, int i_c,
            RingGenerator rings,
            bool addUVs
        ) {

            // find intersection vertices / uvs
            var es = plane.Intersection(a, c, txa, txc, 0);
            var ds = plane.Intersection(b, c, txb, txc, 0);

            Vector3 e = es.Item1, d = ds.Item1;
            Vector2 txe = es.Item2, txd = ds.Item2;

            // if e == d, the three vertices lie in a line,
            //   and thus do not make up a triangle
            if (e == d) {
                return;
                // not sure if this is nescessary
                throw OperationException.MalformedMesh();
            }

            // new indices
            int pi0 = pos.vertices.Count, ni0 = neg.vertices.Count;

            // add connected pair in ring generator

            // find proper direction for ring
            Vector3 tri_nor = Vector3.Cross(c-a,c-b);
            bool dir = Vector3.Dot(e-d,Vector3.Cross(plane.normal,tri_nor)) > 0;

            rings.AddConnected(dir?e:d,dir?d:e);

            // add new vertices and uvs
            pos.vertices.Add(d);
            pos.vertices.Add(e);
            neg.vertices.Add(d);
            neg.vertices.Add(e);
            if (addUVs) {
                pos.uvs.Add(txd);
                pos.uvs.Add(txe);
                neg.uvs.Add(txd);
                neg.uvs.Add(txe);
            }

            // generate triangles for sides ...

            // add a,d,e to positive indicies
            pos.indices.Add(pos.indexMap[i_a]);
            pos.indices.Add(pi0);
            pos.indices.Add(pi0+1);
            // add a,b,d to positive indicies
            pos.indices.Add(pos.indexMap[i_a]);
            pos.indices.Add(pos.indexMap[i_b]);
            pos.indices.Add(pi0);
            // add e,d,c to negative indicies
            neg.indices.Add(ni0+1);
            neg.indices.Add(ni0);
            neg.indices.Add(neg.indexMap[i_c]);

        }

        public static void GenPartialTriangles(
            CuttingPlane plane,
            MeshPart part,
            Vector3 a, Vector3 b,Vector3 c,
            Vector2 txa, Vector2 txb, Vector2 txc,
            int i_a, int i_b, int i_c,
            HashSet<Vector3> allow_cut,
            bool addUVs
        ) {

            // find intersection vertices / uvs
            var es = plane.Intersection(a, c, txa, txc, 0);
            var ds = plane.Intersection(b, c, txb, txc, 0);

            Vector3 e = es.Item1, d = ds.Item1;
            Vector2 txe = es.Item2, txd = ds.Item2;

            // if e == d, the three vertices lie in a line,
            //   and thus do not make up a triangle
            if (e == d) {
                return;
                // not sure if this is nescessary
                throw OperationException.MalformedMesh();
            }

            if (
                !allow_cut.Contains(e) &&
                !allow_cut.Contains(d)
            ) {
                // triangle must not be cut
                part.indices.Add(part.indexMap[i_a]);
                part.indices.Add(part.indexMap[i_b]);
                part.indices.Add(part.indexMap[i_c]);
                return;
            }

            // new indices
            int i0 = part.vertices.Count, i1 = part.vertices.Count + 2;

            // add new vertices and uvs
            part.vertices.Add(d);
            part.vertices.Add(e);
            part.vertices.Add(d);
            part.vertices.Add(e);
            if (addUVs) {
                part.uvs.Add(txd);
                part.uvs.Add(txe);
                part.uvs.Add(txd);
                part.uvs.Add(txe);
            }

            // generate triangles ...

            // add a,d,e
            part.indices.Add(part.indexMap[i_a]);
            part.indices.Add(i0);
            part.indices.Add(i0+1);
            // add a,b,d
            part.indices.Add(part.indexMap[i_a]);
            part.indices.Add(part.indexMap[i_b]);
            part.indices.Add(i0);
            // add e,d,c
            part.indices.Add(i1+1);
            part.indices.Add(i1);
            part.indices.Add(part.indexMap[i_c]);

        }

        public static void GenTwoTriangles(
            CuttingPlane plane,
            MeshPart pos,
            Vector3 a, Vector3 b,Vector3 c,
            Vector2 txa, Vector2 txb, Vector2 txc,
            int i_a, int i_b, int i_c,
            RingGenerator rings,
            bool addUVs,
            float shift
        ) {

            // find intersection vertices / uvs
            var es = plane.Intersection(a, c, txa, txc, shift);
            var ds = plane.Intersection(b, c, txb, txc, shift);

            Vector3 e = es.Item1, d = ds.Item1;
            Vector2 txe = es.Item2, txd = ds.Item2;

            // if e == d, the three vertices lie in a line,
            //   and thus do not make up a triangle
            if (e == d) {
                return;
                // not sure if this is nescessary
                throw OperationException.MalformedMesh();
            }

            // new indices
            int i0 = pos.vertices.Count;

            // add connected pair in ring generator

            // find proper direction for ring
            Vector3 tri_nor = Vector3.Cross(c-a,c-b);
            bool dir = Vector3.Dot(e-d,Vector3.Cross(plane.normal,tri_nor)) > 0;

            rings.AddConnected(dir?e:d,dir?d:e);

            // add new vertices and uvs
            pos.vertices.Add(d);
            pos.vertices.Add(e);
            if (addUVs) {
                pos.uvs.Add(txd);
                pos.uvs.Add(txe);
            }

            // generate triangles for sides ...

            // add a,d,e to positive indicies
            pos.indices.Add(pos.indexMap[i_a]);
            pos.indices.Add(i0);
            pos.indices.Add(i0+1);
            // add a,b,d to positive indicies
            pos.indices.Add(pos.indexMap[i_a]);
            pos.indices.Add(pos.indexMap[i_b]);
            pos.indices.Add(i0);

        }

        // ------------------------------------------------------------
        // Generate single triangle for an intersecting triangle a,b,c
        // It is assumed that a is on the positive half plane
        // ------------------------------------------------------------
        public static void GenTriangle(
            CuttingPlane plane,
            MeshPart pos,
            Vector3 a, Vector3 b,Vector3 c,
            Vector2 txa, Vector2 txb, Vector2 txc,
            int i_a, int i_b, int i_c,
            RingGenerator rings,
            bool addUVs,
            float shift
        ) {

            // find intersection vertices / uvs
            var es = plane.Intersection(c, a, txa, txc, shift);
            var ds = plane.Intersection(b, a, txb, txc, shift);

            Vector3 e = es.Item1, d = ds.Item1;
            Vector2 txe = es.Item2, txd = ds.Item2;

            // if e == d, the three vertices lie in a line,
            //   and thus do not make up a triangle
            if (e == d) {
                return;
                // not sure if this is nescessary
                throw OperationException.MalformedMesh();
            }

            // new indices
            int i0 = pos.vertices.Count;

            // add connected pair in ring generator

            // find proper direction for ring
            Vector3 tri_nor = Vector3.Cross(c-a,c-b);
            bool dir = Vector3.Dot(e-d,Vector3.Cross(plane.normal,tri_nor)) > 0;

            rings.AddConnected(dir?e:d,dir?d:e);

            // add new vertices and uvs
            pos.vertices.Add(d);
            pos.vertices.Add(e);
            if (addUVs) {
                pos.uvs.Add(txd);
                pos.uvs.Add(txe);
            }

            // add a,d,e to positive indicies
            pos.indices.Add(pos.indexMap[i_a]);
            pos.indices.Add(i0);
            pos.indices.Add(i0+1);

        }

    }

}