using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Illumetry.Unity {
    public static class Extensions {
        public static double GetElapsedMillisecondsWithFractionalPart(this Stopwatch stopwatch) {
            var milliseconds = 1000 * stopwatch.ElapsedTicks / (double)Stopwatch.Frequency;
            return milliseconds;
        }
    }
}
