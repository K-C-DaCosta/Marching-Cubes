using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeoPacker  {
	List<Vector3> normals;
	List<Vector3> verts; 
	List<int> indices; 
	int indexPtr; 

	public GeoPacker(){
		normals = new List<Vector3>(); 
		verts = new List<Vector3>(); 
		indices = new List<int>();
		indexPtr= 0;  
	}

	public void AddVertex(Vector3 v,Vector4 n){
		normals.Add(n);
		verts.Add(v);
		indices.Add(indexPtr);
		indexPtr++;  
	}

    public void ClearBuffers()
    {
       
        verts.Clear();
        normals.Clear();
        indices.Clear();
        indexPtr = 0; 
    }

    public int getNumVerts()
    {
        return indexPtr + 1; 

    }

	public void UpdateMesh(ref Mesh m){
		m.Clear();
       
		m.vertices = verts.ToArray(); 
		m.normals = normals.ToArray();
		m.triangles = indices.ToArray();
	}
}
