using System;
using System.Linq;
using UnityEngine;
using System.Globalization;
using System.Collections.Generic;

namespace Illumetry.Unity {
public class AdnPropertiesReader : IDisposable {
    Antilatency.DeviceNetwork.INetwork _network;
    Antilatency.DeviceNetwork.NodeHandle _node;

    public AdnPropertiesReader(Antilatency.DeviceNetwork.INetwork network, Antilatency.DeviceNetwork.NodeHandle node) {
        _network = network;
        _node = node;
    }

    public void Dispose() {
        _node = Antilatency.DeviceNetwork.NodeHandle.Null;
        _network = null;
    }

    public T? TryRead<T>(string propertyName, Func<string, T> parser) where T : struct {
        try {
            var stringValue = _network.nodeGetStringProperty(_node, propertyName);
            return parser(stringValue);
        }
        catch (Exception) {
            return null;
        }
    }

    public T Read<T>(string propertyName, Func<string, T> parser) where T : class {
        try {
            var stringValue = _network.nodeGetStringProperty(_node, propertyName);
            return parser(stringValue);
        }
        catch (Exception) {
            return null;
        }
    }


    public static Vector3 ReadVector3(string value) {
        var parts = value
            .Split(' ')
            .Select(x => float.Parse(x.Trim(), CultureInfo.InvariantCulture))
            .ToArray();
        return new Vector3(parts[0], parts[1], parts[2]);
    }

    public static Vector2Int ReadVector2Int(string value) {
        var parts = value
            .Split(' ')
            .Select(x => int.Parse(x.Trim(), CultureInfo.InvariantCulture))
            .ToArray();
        return new Vector2Int(parts[0], parts[1]);
    }

    public static int ReadInt(string value) {
        return int.Parse(value.Trim());
    }

    public static float ReadFloat(string value) {
        return float.Parse(value.Trim(), CultureInfo.InvariantCulture);
    }

    public static float[] ReadFloatArray(string value) {
        return value
            .Split(new []{' '}, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => float.Parse(x.Trim(), CultureInfo.InvariantCulture))
            .ToArray();
    }
}
}
