using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class BilinearGrid : IFunc2d
{
    public static UnityEngine.Vector2 selectedPoint;
    Vector3[,] _values;
    double[] _xGridValues;

    public BilinearGrid(Vector3[,] values, double[] xGridValues){ 
        _values = (Vector3[,])values.Clone();        
        _xGridValues = (double[])xGridValues.Clone();
    }
    
    public double[] Coeffs {get {return new double[0];} set { } }

    public int DoFs => 0;

    public double Evaluate(double x, double y){
        // debug 
        /*
        UnityEngine.Vector2Int selectedPointIds = new UnityEngine.Vector2Int(0,0);
        double mindist = double.MaxValue;
        for(int i = 0; i < _values.GetLength(0); ++i){
            for(int j = 0; j < _values.GetLength(1); ++j){ 
                var val = _values[i, j];
                var dist = (val.x - selectedPoint.x)*(val.x - selectedPoint.x) + (val.y - selectedPoint.y)*(val.y - selectedPoint.y);
                if(dist < mindist){ 
                    mindist = dist;
                    selectedPointIds.x = i;
                    selectedPointIds.y = j;
                }
            }
        }
        return _functions[Math.Clamp(selectedPointIds.x-1, 0, fMaxX), Math.Clamp(selectedPointIds.y-1, 0, fMaxY)].Evaluate(x, y);
        */
        
        Weights weights;
        var cell = GetCell(x, y, out weights);

        
        var a = (1-weights.lr)*_values[cell.x, cell.y].z + weights.lr*_values[cell.x+1, cell.y].z;
        var b = (1-weights.lr)*_values[cell.x, cell.y+1].z + weights.lr*_values[cell.x+1, cell.y+1].z;
        var res = (1-weights.bt)*a + weights.bt*b;
        return res;
    }

    public struct Weights{ 
        public double bt, lr;    
        public Weights(double bt_, double lr_){ 
            bt = bt_;
            lr = lr_;
        }
    }
    public UnityEngine.Vector2Int GetCell(double x, double y, out Weights weights){ 
        x = Mathf.Clamp((float)x, 0, 0.999999f);
        y = Mathf.Clamp((float)y, 0, 0.999999f);
        var w = _values.GetLength(0);
        var h = _values.GetLength(1);

        
        //var gridX = _logistic.Revert(x)*(w-1);
        //var gridY = _logistic.Revert(y)*(h-1);

        // we dont have logistic anymore and definately dont want to render this mesh, so lets do the stupid search instead
        int firstLargerId = Array.BinarySearch(_xGridValues, (float)x);
        if(firstLargerId < 0) firstLargerId = ~firstLargerId;

        int leftEdge;
        if(firstLargerId == 0){ 
            leftEdge = 0;
        } else if(firstLargerId >= _xGridValues.Length){ 
            leftEdge = _xGridValues.Length - 2;    
        } else { 
            leftEdge = firstLargerId - 1;    
        }

        var bottomEdge = (int)Math.Floor(y*(h-1));
        

        var a = _values[leftEdge, bottomEdge];
        var b = _values[leftEdge+1, bottomEdge];
        var constPartOfEdgeEquation = (x-a.x)/(b.x-a.x);
        Func<int, double> edgeVal = row => {
            var a = _values[leftEdge, row];
            var b = _values[leftEdge+1, row];
            return a.y + (b.y-a.y)*constPartOfEdgeEquation;
        };

        double bottomEdgeVal = edgeVal(bottomEdge);
        while(bottomEdge < h-2){ 
            bottomEdgeVal = edgeVal(bottomEdge + 1);
            if(bottomEdgeVal > y){ 
                break;
            }
            ++bottomEdge;
        }
        while(bottomEdge >= 0){ 
            if(bottomEdge == 0){ 
                break;    
            }
            bottomEdgeVal = edgeVal(bottomEdge);
            if(bottomEdgeVal < y){ 
                break;    
            }
            --bottomEdge;
        }

        int topEdge = bottomEdge + 1;
        double topEdgeVal = edgeVal(topEdge);
        bottomEdgeVal = edgeVal(bottomEdge);
        double kbt, klr;
        if(y < bottomEdgeVal){
            kbt = 0;
        } else if(y > topEdgeVal){
            kbt = 1;
        } else {
            kbt = (y - bottomEdgeVal)/(topEdgeVal - bottomEdgeVal);
        }

        klr = constPartOfEdgeEquation;
        weights = new Weights(kbt, klr);

        return new UnityEngine.Vector2Int(leftEdge, bottomEdge);
    }
}

public class OverdriveOptimization
{
    public IFunc2d[,] Functions; //[color, timemark]

    public OverdriveOptimization(int nTimemarks){ 
        Functions = new IFunc2d[3, nTimemarks];
    }

    public void Optimize(OverdriveDataset dataset)
    {
        Optimize(dataset, OverdriveDataset.Color.R);
        Optimize(dataset, OverdriveDataset.Color.G);
        Optimize(dataset, OverdriveDataset.Color.B);
    }

    public void Optimize(OverdriveDataset dataset, OverdriveDataset.Color color){ 
        var starts = dataset.StartsD;
        for(int itm = 0; itm < dataset.NTimemarks; ++itm){
            var gridData = GetArrayDataForTimemark(dataset, color, itm);
            var func = new BilinearGrid(gridData, starts);
            Functions[(int)color, itm] = func;
        }
    }

    public Vector3[,] GetArrayDataForTimemark(OverdriveDataset dataset, OverdriveDataset.Color color, int iTimemark){
        var startsF = dataset.StartsF;
        var endsF = dataset.InitialEndsF;
        var result = new Vector3[startsF.Length, endsF.Length];
        for(int iStart = 0; iStart < startsF.Length; ++iStart){
            for(int iEnd = 0; iEnd < endsF.Length; ++iEnd){
                result[iStart, iEnd] = new Vector3(
                    startsF[iStart], 
                    dataset.GetActualEndF(iTimemark, iStart, iEnd, color), 
                    dataset.GetClampedOverdriveF(iTimemark, iStart, iEnd, color)
                );
            }
        }
        return result;
    }
}
