using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Logistic01{ 
    private float _k = 0.2f; 
    public Logistic01(float k){ 
        _k = k;    
    }
    public float Apply(float inp){ 
        return 1.0f/(1+Mathf.Pow(inp/(1-inp), -_k));
    }

    public float Revert(float inp){ 
        var z = Mathf.Pow(1.0f/inp - 1.0f, -1/_k); // x/1-x
        return z/(1+z); 
    }  
}
public class Logistic01D{ 
    private double _k = 0.2; 
    public Logistic01D(double k){ 
        _k = k;    
    }
    public double Apply(double inp){ 
        return 1.0/(1+Math.Pow(inp/(1-inp), -_k));
    }

    public double Revert(double inp){ 
        var z = Math.Pow(1.0f/inp - 1.0f, -1/_k); // x/1-x
        return z/(1+z); 
    }  
}
