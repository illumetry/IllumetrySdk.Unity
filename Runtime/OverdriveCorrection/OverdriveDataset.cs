using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Illumetry.Unity {
public static class ColorMethods {
    public static UnityEngine.Color ToColor(this OverdriveDataset.Color color) {
        switch (color) {
            default:
            case OverdriveDataset.Color.R: return UnityEngine.Color.red;
            case OverdriveDataset.Color.G: return UnityEngine.Color.green;
            case OverdriveDataset.Color.B: return UnityEngine.Color.blue;
        }
    }
}

public class OverdriveDataset {
    public enum Color {
        R,
        G,
        B
    };

    [Serializable]
    public struct Sample {
        public Int16 OverdriveR, OverdriveG, OverdriveB;

        public Sample(Int16 overdriveR, Int16 overdriveG, Int16 overdriveB) {
            OverdriveR = overdriveR;
            OverdriveG = overdriveG;
            OverdriveB = overdriveB;
        }

        public Int16 this[Color color] {
            get {
                switch (color) {
                    default:
                    case OverdriveDataset.Color.R: return OverdriveR;
                    case OverdriveDataset.Color.G: return OverdriveG;
                    case OverdriveDataset.Color.B: return OverdriveB;
                }
            }
            set {
                switch (color) {
                    default:
                    case OverdriveDataset.Color.R:
                        OverdriveR = value;
                        return;
                    case OverdriveDataset.Color.G:
                        OverdriveG = value;
                        return;
                    case OverdriveDataset.Color.B:
                        OverdriveB = value;
                        return;
                }
            }
        }

        public static Sample operator +(Sample a, Sample b) {
            var res = new Sample();
            res.OverdriveR = (short)(a.OverdriveR + b.OverdriveR);
            res.OverdriveG = (short)(a.OverdriveG + b.OverdriveG);
            res.OverdriveB = (short)(a.OverdriveB + b.OverdriveB);
            return res;
        }

        public static Sample operator *(Sample a, float k) {
            var res = new Sample();
            res.OverdriveR = (short)(Math.Round(k * a.OverdriveR));
            res.OverdriveG = (short)(Math.Round(k * a.OverdriveG));
            res.OverdriveB = (short)(Math.Round(k * a.OverdriveB));
            return res;
        }
    }

    public readonly byte[] Starts, InitialEnds;
    public readonly float[] Timemarks;

    public int NEnds {
        get => InitialEnds.Length;
    }

    public int NStarts {
        get => Starts.Length;
    }

    public int NTimemarks {
        get => Timemarks.Length;
    }

    public Sample[,,] Data;

    public float[] StartsF {
        get => Starts.Select(b => ByteToFloat(b)).ToArray();
    }

    public float[] InitialEndsF {
        get => InitialEnds.Select(b => ByteToFloat(b)).ToArray();
    }

    public double[] StartsD {
        get => Starts.Select(b => (double)ByteToFloat(b)).ToArray();
    }

    public double[] InitialEndsD {
        get => InitialEnds.Select(b => (double)ByteToFloat(b)).ToArray();
    }

    public OverdriveDataset(int nTimemarks, float timemarksMargin, int numIntensitySteps, float logisticK, float timeGamma) {
        var logistic = new Logistic01(logisticK);
        Timemarks = new float[nTimemarks];
        var dTimemark = 1.0f / (nTimemarks - 1);
        for (int i = 0; i < nTimemarks; ++i) {
            Timemarks[i] = timemarksMargin + (1 - 2 * timemarksMargin) * (float)Math.Pow(i * dTimemark, timeGamma);
        }

        float intensityStep = 1f / (numIntensitySteps - 1);
        Starts = new byte[numIntensitySteps];
        for (int i = 0; i < numIntensitySteps; ++i) {
            Starts[i] = FloatToByte(logistic.Apply(intensityStep * i));
        }

        InitialEnds = (byte[])Starts.Clone();
        Data = new Sample[NTimemarks, NStarts, NEnds];
        for (int itm = 0; itm < NTimemarks; ++itm) {
            for (int iStart = 0; iStart < NStarts; ++iStart) {
                for (int iEnd = 0; iEnd < NEnds; ++iEnd) {
                    Data[itm, iStart, iEnd] = new Sample(InitialEnds[iEnd], InitialEnds[iEnd], InitialEnds[iEnd]);
                }
            }
        }
    }

    public OverdriveDataset(byte[] starts, byte[] ends, float[] timemarks) {
        Starts = (byte[])starts.Clone();
        InitialEnds = (byte[])ends.Clone();
        Timemarks = (float[])timemarks.Clone();
        Data = new Sample[NTimemarks, NStarts, NEnds];
    }

    public OverdriveDataset(BinaryReader br) {
        InitialEnds = ReadByteArray(br);
        Starts = ReadByteArray(br);
        Timemarks = ReadByteArray(br).Select(b => ByteToFloat(b)).ToArray();
        Data = new Sample[NTimemarks, NStarts, NEnds];

        for (int itm = 0; itm < NTimemarks; ++itm) {
            for (int iStart = 0; iStart < NStarts; ++iStart) {
                for (int iEnd = 0; iEnd < NEnds; ++iEnd) {
                    int initialEnd = InitialEnds[iEnd];

                    if (iStart < iEnd) {
                        Data[itm, iStart, iEnd] = new Sample(
                            (short)(initialEnd + br.ReadByte()),
                            (short)(initialEnd + br.ReadByte()),
                            (short)(initialEnd + br.ReadByte())
                        );
                    }
                    else {
                        Data[itm, iStart, iEnd] = new Sample(
                            (short)(initialEnd - br.ReadByte()),
                            (short)(initialEnd - br.ReadByte()),
                            (short)(initialEnd - br.ReadByte())
                        );
                    }
                }
            }
        }
    }

    public void Serialize(BinaryWriter bw) {
        WriteByteArray(bw, InitialEnds);
        WriteByteArray(bw, Starts);
        WriteByteArray(bw, Timemarks.Select(f => FloatToByte(f)).ToArray());
        for (int itm = 0; itm < NTimemarks; ++itm) {
            for (int iStart = 0; iStart < NStarts; ++iStart) {
                for (int iEnd = 0; iEnd < NEnds; ++iEnd) {
                    int initialEnd = InitialEnds[iEnd];
                    var sample = Data[itm, iStart, iEnd];
                    if (iStart < iEnd) {
                        bw.Write((byte)Mathf.Clamp(sample.OverdriveR - initialEnd, 0, 255));
                        bw.Write((byte)Mathf.Clamp(sample.OverdriveG - initialEnd, 0, 255));
                        bw.Write((byte)Mathf.Clamp(sample.OverdriveB - initialEnd, 0, 255));
                    }
                    else {
                        bw.Write((byte)Mathf.Clamp(initialEnd - sample.OverdriveR, 0, 255));
                        bw.Write((byte)Mathf.Clamp(initialEnd - sample.OverdriveG, 0, 255));
                        bw.Write((byte)Mathf.Clamp(initialEnd - sample.OverdriveB, 0, 255));
                    }
                }
            }
        }
    }


    /*
    const int deltaBytes = 5;
    const int deltaMask = (1 << deltaBytes) - 1;

    public void SerializeCompressed(BinaryWriter bw){
        WriteByteArray(bw, InitialEnds);
        WriteByteArray(bw, Starts);
        WriteByteArray(bw, Timemarks.Select(f => FloatToByte(f)).ToArray());

        int[,,] deltaBase = new int[NStarts, NEnds, 3];
        for(int iStart = 0; iStart < NStarts; ++iStart){
            for(int iEnd = 0; iEnd < NEnds; ++iEnd){ 
                deltaBase[iStart, iEnd, 0] = InitialEnds[iEnd];
                deltaBase[iStart, iEnd, 1] = InitialEnds[iEnd];
                deltaBase[iStart, iEnd, 2] = InitialEnds[iEnd];
            } 
        }

        var deltas = new int[3];
        for(int itm = NTimemarks-1; itm >= 0; --itm){
            for(int iStart = 0; iStart < NStarts; ++iStart){
                for(int iEnd = 0; iEnd < NEnds; ++iEnd){
                    var sample = Data[itm, iStart, iEnd];
                    for(int color = 0; color < 3; ++color){ 
                        var delta = sample[(Color)color] - deltaBase[iStart, iEnd, color]; 
                        if(iStart > iEnd){ 
                            delta *= -1;    
                        }
                        deltaBase[iStart, iEnd, color] = sample.OverdriveR;
                        delta = Math.Clamp(delta, 0, 31);
                        deltas[color] = delta;
                    }

                    var compressedDeltas = (deltas[0] & deltaMask) + ((deltas[1] & deltaMask) << deltaBytes) + ((deltas[2] & deltaMask) << (2*deltaBytes));
                    bw.Write((short)compressedDeltas);
                }
            }
        }
    }

    public OverdriveDataset DeserializeCompressed(BinaryReader br){ 
        var initialEnds = ReadByteArray(br);
        var starts = ReadByteArray(br);
        var timemarks = ReadByteArray(br).Select(b => ByteToFloat(b)).ToArray();
        var res = new OverdriveDataset(starts, initialEnds, timemarks);


        return res;
    }*/

    public void Analyse(Color color) {
        var sb = new StringBuilder();
        for (int iStart = 0; iStart < NStarts; ++iStart) {
            for (int iEnd = 0; iEnd < NEnds; ++iEnd) {
                var sample = Data[0, iStart, iEnd];
                sb.Append($"{sample[color] - InitialEnds[iEnd]}\t");
            }

            sb.AppendLine();
        }

        UnityEngine.Debug.Log(sb.ToString());
    }

    public byte GetActualEnd(int iTimemark, int iStart, int iEnd, Color color) {
        int initialEnd = InitialEnds[iEnd];
        Int16 overdrive = Data[iTimemark, iStart, iEnd][color];
        byte end;
        if (overdrive > 255) {
            end = (byte)Mathf.Clamp(initialEnd - (overdrive - 255), 0, 255);
        }
        else {
            if (overdrive < 0) {
                end = (byte)Mathf.Clamp(initialEnd - overdrive, 0, 255);
            }
            else {
                end = (byte)Mathf.Clamp(initialEnd, 0, 255);
            }
        }

        return end;
    }

    public float GetActualEndF(int iTimemark, int iStart, int iEnd, Color color) {
        return ByteToFloat(GetActualEnd(iTimemark, iStart, iEnd, color));
    }

    public float GetClampedOverdriveF(int iTimemark, int iStart, int iEnd, Color color) {
        Int16 overdrive = Data[iTimemark, iStart, iEnd][color];
        var res = overdrive / 255f;
        if (res > 1) res = 1;
        else if (res < 0) res = 0;
        return res;
    }

    public static float ByteToFloat(byte b) {
        return b / 255f;
    }

    public static byte FloatToByte(float f) {
        return (byte)Mathf.Clamp(f * 255, 0, 255);
    }

    static byte[] ReadByteArray(BinaryReader br) {
        var n = br.ReadByte();
        var res = new byte[n];
        for (int i = 0; i < n; ++i) {
            res[i] = br.ReadByte();
        }

        return res;
    }

    static void WriteByteArray(BinaryWriter bw, byte[] array) {
        bw.Write((byte)array.Length);
        for (int i = 0; i < array.Length; ++i) {
            bw.Write(array[i]);
        }
    }
}
}
