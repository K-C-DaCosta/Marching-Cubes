using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AABB  {
    public Vector4[] corners = new Vector4[2];

    public static readonly int MIN = 0;
    public static readonly int MAX = 1; 

    public AABB(Vector4 c0, Vector4 c1){
        corners[MIN] = c0;
        corners[MAX] = c1; 
    }
    public AABB clone()
    {
        return new AABB(corners[0], corners[1]);
    }
    
    public Vector4 getBoxSize()
    {
        
        Vector4 disp = corners[MAX] - corners[MIN];
        disp.x = Mathf.Abs(disp.x);
        disp.y = Mathf.Abs(disp.y);
        disp.z = Mathf.Abs(disp.z);
        return disp; 
    } 

    public AABB translate(Vector4 t)
    {
        this.corners[0] += t;
        this.corners[1] += t;
        return this;
    }

    public Vector4 min()
    {
        return corners[MIN];
    }
    public Vector4 max()
    {
        return corners[MAX];
    }
}
