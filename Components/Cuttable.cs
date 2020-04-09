
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
        public float HighlightWidth = 0.015f;
        [Tooltip("Color of intersection highlighting.")]
        public Color HighLightColor = Color.white;

        [Tooltip("Velocity resulting objects should drift apart with.")]
        public float DriftVelocity = 0;

        [Tooltip("If object has textures, use this texture coordinate for inner geometry.")]
        public Vector2 InnerTextureCoordinate = Vector2.zero;

        [Tooltip("Analyses intersection for potential holes in intersection.")]
        public bool CheckForHoles = false;
        
        [Tooltip("For a less well-formed mesh, this will attempt to merge points in intersection that are almost exactly identical.")]
        public bool TryApproximation = false;
        
        [Tooltip("Naively close open intersecting surfaces.")]
        public bool CloseOpenSurfaces = false;
        
        [Tooltip("Ignore open surfaces. Otherwise an exception is thrown.")]
        public bool AllowOpenSurfaces = false;
        
        [Tooltip("Attempt to seperate disconnected parts of mesh. Note: not terribly fast for big objects.")]
        public bool PolySeperate = false;
        
    }

}