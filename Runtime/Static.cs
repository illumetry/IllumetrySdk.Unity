using System;
using System.Collections.Generic;
using UnityEngine;

namespace Illumetry.Unity {
    public static class Static{
        public static T GetNext<T>(this IEnumerator<T> value) {
            if (!value.MoveNext()) {
                throw new IndexOutOfRangeException();
            };
            return value.Current;
        }

        public static Vector3 ReadVector3(this IEnumerator<float> values) {
            return new Vector3(values.GetNext(), values.GetNext(), values.GetNext());
        }

         public static Vector2 ReadVector2(this IEnumerator<float> values) {
            return new Vector2(values.GetNext(), values.GetNext());
        }
    }
}
