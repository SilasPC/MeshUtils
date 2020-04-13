
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static MeshUtils.API;
using static MeshUtils.VectorUtil;
using CuttingPlane = MeshUtils.CuttingPlane;

namespace MeshUtils {

    public class Cuttable : MonoBehaviour {

        [Tooltip("Width of seperation intersection highlighting. Zero for no highlighting.")]
        [Range(0,0.5f)]
        public float highlightWidth = 0.015f;
        [Tooltip("Color of intersection highlighting.")]
        public Color highLightColor = Color.white;

        [Tooltip("If object has textures, use this texture coordinate for inner geometry.")]
        public Vector2 innerTextureCoordinate = Vector2.zero;

        [Tooltip("Analyses intersection for potential holes in intersection. If unsure and object is concave, leave on.")]
        public bool checkForHoles = true;
        
        [Tooltip("For a less well-formed mesh, this will attempt to merge points in intersection that are almost exactly identical.")]
        public bool tryApproximation = false;
        
        [Tooltip("Naively close open intersecting surfaces.")]
        public bool closeOpenSurfaces = false;
        
        [Tooltip("Ignore open surfaces. Otherwise an exception is thrown.")]
        public bool allowOpenSurfaces = false;
        
        [Tooltip("Attempt to seperate disconnected parts of resulting meshes. Note: not terribly fast for big objects.")]
        public bool polySeperate = false;
        
    }

}