
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshUtils {
    static class Debugging {
        // --------------------
        // Describe a vector3
        // --------------------
        public static string VecStr(Vector3 v) {
            int intx = BitConverter.ToInt32(BitConverter.GetBytes(v.x),0);
            int inty = BitConverter.ToInt32(BitConverter.GetBytes(v.y),0);
            int intz = BitConverter.ToInt32(BitConverter.GetBytes(v.z),0);
            return v.x+" | "+v.y+" | "+v.z+" ### "+intx+" | "+inty+" | "+intz+" ### "+v.GetHashCode();
        }

        // ---------------------------------------
        // Make a GeoGebra compatible point list
        // ---------------------------------------
        public static void DebugRing(List<Vector3> ring) {
            List<String> strings = new List<String>();
            foreach (var v in ring) strings.Add("("+v.x.ToString().Replace(',','.')+","+v.y.ToString().Replace(',','.')+","+v.z.ToString().Replace(',','.')+")");
            Debug.Log("{"+String.Join(",",strings)+"}");
        }

        // ------------------------------------------
        // Make a GeoGebra compatible 2d point list
        // ------------------------------------------
        public static void DebugRing2D(List<Vector2> ring) {
            List<String> strings = new List<String>();
            foreach (var v in ring) strings.Add("("+v.x.ToString().Replace(',','.')+","+v.y.ToString().Replace(',','.')+")");
            Debug.Log("{"+String.Join(",",strings)+"}");
        }

        // --------------------------------------
        // Make a GeoGebra compatible ring list
        // --------------------------------------
        public static void DebugRings(List<List<Vector3>> rings) {
            List<String> topStrings = new List<String>();
            foreach (var ring in rings) {
                List<String> strings = new List<String>();
                foreach (var v in ring) strings.Add("("+v.x.ToString().Replace(',','.')+","+v.y.ToString().Replace(',','.')+","+v.z.ToString().Replace(',','.')+")");
                topStrings.Add("{"+String.Join(",",strings)+"}");
            }
            Debug.Log("{"+String.Join(",",topStrings)+"}");
        }

        // ------------------
        // Make an int list
        // ------------------
        public static void LogList<T>(List<T> list) {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (T i in list) sb.Append(i.ToString()+" ");
            Debug.Log(sb.ToString());
        }
    }
}