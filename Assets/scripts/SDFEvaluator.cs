using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
 Author: Khadeem DaCosta
 Description: This class is responsible for managing the 
 list of SDF 'edits' and evaluating the final SDF at a point P.
 */
public class SDFEvaluator {
    /*
		SDF function des
	*/
    Vector4 center = new Vector4(4.0f, 4, -0.5f, 1);
    public float t = 0; 

    float Sphere(Vector3 p, Vector3 center, float radius){ 
        return Vector3.Magnitude(p - center) - radius;
    }

    float min(float a, float b ,float c)
    {
        return Mathf.Min(a, b);
    }
    float smin(float a, float b, float k){
        float h = Mathf.Max(k - Mathf.Abs(a - b), 0.0f) / k;
        return Mathf.Min(a, b) - h * h * k * (1.0f / 4.0f);
    }

    public float EvaluateSDF(Vector3 p){
        float result = 9999999999.0f;
        //if ( p.x > 4){
          //  return -99999.0f; 
        //}
        result = smin(result, Sphere(p, new Vector3(0, 0, 0), 2.0f),0.4f);
        result = smin(result, Sphere(p, new Vector3(3.0f, 0, 0), 1.5f),0.9f);
        
        result = smin(result, Sphere(p, new Vector3(3.0f + Mathf.Sin(t), 3, 0), 1.5f),0.9f);
        
        result = smin(result, Sphere(p, new Vector3(5.0f, 3, 0), 0.5f), 0.6f);
        result = smin(result, Sphere(p, new Vector3(4.0f, 4, 0.5f), 0.3f), 0.3f);//eye1
        result = smin(result, Sphere(p, new Vector3(4.0f, 4, -0.5f),0.3f), 0.3f);//eye2
        
       
        return result;
	}

    public void Animate()
    {
        t += 0.1f;
    }

    public Vector4 EvaluateGrad(Vector3 p){
        float h = 0.001f;
        float inv_denom = 1.0f/(2.0f * h);
        float dfdx = (EvaluateSDF(p + new Vector3(h, 0, 0)) - EvaluateSDF(p - new Vector3(h, 0, 0)))*inv_denom;
        float dfdy = (EvaluateSDF(p + new Vector3(0, h, 0)) - EvaluateSDF(p - new Vector3(0, h, 0)))*inv_denom;
        float dfdz = (EvaluateSDF(p + new Vector3(0, 0, h)) - EvaluateSDF(p - new Vector3(0, 0, h)))*inv_denom;
        return new Vector4(dfdx, dfdy, dfdz, 0.0f);
    }
}
