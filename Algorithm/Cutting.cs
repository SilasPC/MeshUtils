
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MeshUtils.Util;
using static MeshUtils.API;

namespace MeshUtils {

    static class Algorithms {

        public static CutResult NonPlanarCut(
            GameObject target,
            CuttingTemplate template
        ) {return NonPlanarAlgorithm.Run(target,template);}

        public static CutResult PartialPlanarCut(
            GameObject target,
            CuttingPlane plane,
            CutParams param
        ) {return PartialAlgorithm.Run(target,plane,param);}

        public static CutResult PlanarCutWithoutGap(
            GameObject target,
            CuttingPlane plane,
            CutParams param
        ) {return BaseAlgorithm.Run(target,plane,param);}

        public static CutResult PlanarCutWithGap(
            GameObject target,
            CuttingPlane plane,
            CutParams param
        ) {return GapAlgorithm.Run(target,plane,param);}

    }

}