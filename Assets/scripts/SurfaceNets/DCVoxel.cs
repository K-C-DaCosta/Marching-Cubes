using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DCVoxel {
    public Vector4 qefVertex;
    public int[] edgePtrList = new int[12];
    public bool isEmpty = false; 

    public DCVoxel()
    {
        qefVertex = Vector4.zero; 
        for(int K = 0; K < 12; K++)
        {
            edgePtrList[K] = 0; 
        }
    }
}
