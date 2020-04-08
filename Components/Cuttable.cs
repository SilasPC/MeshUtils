
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static MeshUtils.API;
using static MeshUtils.VectorUtil;
using CuttingPlane = MeshUtils.CuttingPlane;

namespace MeshUtils {

    public class Cuttable : MonoBehaviour {

        public bool TryApproximation = false;
        public bool CloseOpenSurfaces = false;
        public bool AllowOpenSurfaces = false;
        public bool PolySeperate = false;
        
    }

}