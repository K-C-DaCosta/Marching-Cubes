using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DCEdge {
    public Vector4 p0;
    public Vector4 p1;
    public Vector4 intersectionPoint;
    public Vector4 normal; 
    public int[] adjVoxels;
    public int mask;
    public bool isOutBounds; 
    public static readonly int INTERSECTING = 0x1; 
    public DCEdge(Vector4 p0,Vector4 p1, int mask)
    {
        this.p0 = p0;
        this.p1 = p1;
        this.intersectionPoint = Vector4.zero;
        this.normal = Vector4.zero;
        this.mask = mask;
        this.adjVoxels = null;
        this.isOutBounds = false; 
    }
    public DCEdge(Vector4 p0, Vector4 p1, int mask,int[] adjVoxels)
    {
        this.p0 = p0;
        this.p1 = p1;
        this.mask = mask;
        this.intersectionPoint = Vector4.zero;
        this.normal = Vector4.zero;
        this.adjVoxels = adjVoxels;
        this.isOutBounds = false; 
    }

}
