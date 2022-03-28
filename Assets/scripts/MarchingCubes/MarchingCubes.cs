using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MarchingCubes : MonoBehaviour {
    public GameObject spherePrefab;
	public const float VOX_DELTA = 0.5f;
    public GameObject[] debugSpheres =new GameObject[8];
    public int dx, dy, dz;
    public bool draw = false;
    public float t = 0; 

	float [] voxelCornerTable;
	float[] patternPointTable;
	int[] patternTable;
	int[] patternIndexTable; 
	int[] patternPointEdgeTable; 
	int[] patternOffsetTable;
    int[] complementPatternOffsetTable;

	int[] marchingCubesTable;
	Matrix4x4 [] marchingCubesMatrixTable;

	GeoPacker gp; 
	Mesh mesh;
	SDFEvaluator sdf;

    float T0;

    private void InitTables() {
        const float D = VOX_DELTA;

        voxelCornerTable = new float[]{
             0, 0, 0,1,//padding
			-D,-D,-D,1,
             D,-D,-D,1,
             D, D,-D,1,
            -D, D,-D,1,
            -D,-D, D,1,
             D,-D, D,1,
             D, D, D,1,
            -D, D, D,1,
        };

        patternPointTable = new float[]{
             0, 0, 0,1,//<--padding 
			 0,-D,-D,1,
             D, 0,-D,1,
             0, D,-D,1,
            -D, 0,-D,1,
             0,-D, D,1,
             D, 0, D,1,
             0, D, D,1,
            -D, 0, D,1,
            -D,-D, 0,1,//9
			 D,-D, 0,1,//10
			-D, D, 0,1,//11
			 D, D, 0,1 //12
		};

        patternPointEdgeTable = new int[]{
             0,0, //padding 
			 1,2, // e1 
			 2,3, // e2
			 3,4, // e3
			 4,1, // e4
			 5,6, // e5
			 6,7, // e6
			 7,8, // e7 
			 8,5, // e8 
			 5,1, // e9
			 6,2, // e10 
			 8,4, // e11
			 7,3, // e12
		};

        patternTable = new int[]
        {
            -1,//end of case 0 
			1 ,9, 4,
            -1,//end of case 1
			9, 4,2,
            9,2,10,
            -1,//end of case 2 
			1,9,4,
            12,2,3,
            -1,// end of case 3
			1, 9,4,
            12,6,7,
            -1,//end of case 4
			9,8,2,
            2,8,6,
            1,9,2,
            -1,//end of case 5
			6,7,12,
            9, 4,2,
            9,2,10,
            -1,//end of case 6 
			4,11,3,
            6,7,12,
            1,2,10,
            -1,//end of case 7
			4,8,2,
            2,8,6,
            -1,//end of case 8
			4,11,7,
            4,7,1,
            1,7,6,
            1,6,10,
            -1,// end of case 9
			9,3,1,
            9,11,3,
            5,12,10,
            5,7,12,
            -1,//end of case 10
			4,8,1,
            1,8,12,
            8,7,12,
            1,12,10,
            -1,//end of case 11 
			4,11,3,
            1,9,2,
            9,8,2,
            2,8,6,
            -1,//end of case 12 
			1,9,4,
            8,11,7,
            2,3,12,
            10,5,6,
            -1,//end of case 13 
			9,11,1,
            1,11,6,
            11,7,6,
            1,6,2,
            -1,//end of case 14   [[EXTRA CASES BELOW]]
            4,3,9,
            9,3,12,
            9,12,2,
            9,7,10,
            9,2,1,
            -1,//end of case 3c [ case 15] 
            4,7,2,
            2,7,12,
            4,7,9,
            9,7,10,
            10,7,6,
            -1,//end of case 6c [case 16] 
            4,11,1,
            1,11,7,
            9,12,6,
            1,7,10,
            10,7,6,
            2,3,12,
            -1,//end of  case 7c  b [case 17] 
            11,5,9,
            11,7,5,
            3,12,10,
            3,10,1,
            -1,//end of case 10c [case 18]
            1,4,9,
            8,11,3,
            8,3,2,
            8,2,6,
            -1, // end of case 12c [case 19]
            4,11,3,
            9,8,5,
            1,2,10,
            7,12,6,
            -1, //end of case 13c [case 20 ]
		};

        patternIndexTable = new int[]{
            0x00,
            0x01,
            0x03,
            0x05,
            0x41,
            0x32,
            0x43,
            0x4A,
            0x33,
            0xB1,
            0x69,
            0x71,
            0x3A,
            0xA5,
            0xB2,
        };
        complementPatternOffsetTable = new int[]
        {
            0,
            0,
            0,
            15,
            0,
            0,
            16,
            17,
            0,
            0,
            18,
            0,
            19,
            20,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
        };

		marchingCubesMatrixTable = new Matrix4x4[256];
        for(int K = 0; K < 256; K++)
        {
            marchingCubesMatrixTable[K] = Matrix4x4.identity;
        }

        marchingCubesTable = new int[256];
        patternOffsetTable = new int[22];

        generatePatternOffsetTable();
		generateMarchingCubesTable();


        DenseMatrix.InitMatrixCache();

        T0 = Time.time;
	}

	void generateMarchingCubesTable(){
		Vector4[] voxelCorners = new Vector4[8];
		int index,K;
		for(K = 0; K < 256; K++){
			index = K; 
			//if there are more than 4 bits set invert(there are only 15 distinct cases with maximum of 4 verticies set )
			if( NumberOfSetBits(index) > 4 ){
				index = ~index;
			}
			generateCornersFromIndex(index,ref voxelCorners);
			detectRotationalSymmetry(ref voxelCorners,K);
		}
	}

	/*
		Detects rotational symmetry and assigns the table
	*/
	void detectRotationalSymmetry(ref Vector4[] corners,int index){
		Matrix4x4 rotX,rotY,rotZ,rot;
		Vector4[] standardCorners = new Vector4[8];
 
		for(int angleX = 0; angleX <= 360; angleX+=90){
			for(int angleY = 0; angleY <=360; angleY+=90 ){
				for(int angleZ = 0; angleZ <=360; angleZ+=90){
					rotX = Matrix4x4.Rotate(Quaternion.AngleAxis((float)angleX,new Vector3(1,0,0)) );
					rotY = Matrix4x4.Rotate(Quaternion.AngleAxis((float)angleY,new Vector3(0,1,0)) );
					rotZ = Matrix4x4.Rotate(Quaternion.AngleAxis((float)angleZ,new Vector3(0,0,1)) );
					rot = rotZ*rotY*rotX;
					for(int K = 0; K < 15; K++){
						generateCornersFromIndex(patternIndexTable[K],ref standardCorners);
						if( isPatternMatching(corners,standardCorners,ref rot) ){
							marchingCubesMatrixTable[index] = rot;
							marchingCubesTable[index] = K;
                            //Debug.Log("Index = " + index + " K = " + K);
                            return;
						}
					}
				}
			}
		}
	}


    /*
    * Check if each vertex in A has a matching vertex in B 
    */
    bool isPatternMatching(Vector4[] A,Vector4[] B,ref Matrix4x4 rot){
		Vector4 disp; 
		const float EPSILON = 0.00125f;
        bool match;

        /*
         * Check if the number of verticies are the same
         */
        int vertCountA = 0;
        int vertCountB = 0;

        for(int K = 0; K < 8; K++){
            if( Vector4.Dot(A[K],A[K]) > 1.01f){
                vertCountA++;
            }

            if (Vector4.Dot(B[K],B[K]) > 1.01f){
                vertCountB++;
            }
        }

        if( vertCountA != vertCountB){
            return false; 
        }

        /*
        * Check if each vertex in A has a matching vertex in B 
        */
		for (int K =0 ; K < 8; K++){
            match = false; 
            for (int M = 0; M < 8; M++)
            {
            
                disp = A[K] - (rot * B[M]);
                if (Vector4.Dot(disp, disp) < EPSILON)
                {
                    match = true;
                    break;
                }
            }
            if (match == false)
            {
                return false;
            }
        }
		return true;
	}

    /*
     * This function generates voxel corner positions based on the index (which ranges from 0-255)
     */
	void generateCornersFromIndex(int index,ref Vector4[] cornerList){
		for(int K = 0; K < 8; K++){
			cornerList[K] = Vector4.zero; 
			cornerList[K].w = 1.0f; 
			if( ((index >> K) & 1)  == 1 ){
				int tableOffset = 4*(K+1);
				cornerList[K] =  new Vector4(
					voxelCornerTable[tableOffset+0], 
					voxelCornerTable[tableOffset+1],
					voxelCornerTable[tableOffset+2],
					voxelCornerTable[tableOffset+3]
				);
			}
		}
	}

	/*
	This function parses the PatternTable and records offsets for each case 
	*/
	void generatePatternOffsetTable(){
		int recsFilled = 1; 
		patternOffsetTable[0] = 0; 
		for(int K = 0; K < patternTable.Length; K++){
			if( patternTable[K] == -1){
				patternOffsetTable[recsFilled] = K+1;
				recsFilled++;
			} 
		}
	}

    /*
     * Calculates a "fit" matrix that translates and scales a unit square voxel to fit a voxel of arbitrary width,
     * height, and depth.
     */
    Matrix4x4 calcFitMat(Vector4 center,float w,float h,float d)
    {
        Vector3 sv = new Vector3(w, h, d);
        sv = sv*1.0f;
        Matrix4x4 scale = Matrix4x4.Scale(sv);
        Matrix4x4 translate = Matrix4x4.Translate(center);
        return translate*scale;
    }

    /*
     * Samples the scalar function from a non-unit voxel and calculates the voxel index 
     */
    int calculateVoxelIndex(Matrix4x4 fitMat) 
    {
        int index = 0;
        Vector4[] localCornerTable = new Vector4[8];
        generateCornersFromIndex(0xff, ref localCornerTable);

        for (int K = 0; K < 8; K++){
            Vector4 fitVert = fitMat*localCornerTable[K];
            if( sdf.EvaluateSDF(fitVert) < 0)
            {
                index |= (1 << K);
            } 
        }
        return index; 
    }

    void renderVoxel(AABB aabb,int xSlices,int ySlices, int zSlices, int x, int y, int z)
    {
        Vector4 size = aabb.getBoxSize();
        float width = size.x / (float)xSlices;
        float height = size.y / (float)ySlices;
        float depth = size.z / (float)zSlices;

        Vector4 centerOffset = new Vector4(
            width * 0.5f,
            height * 0.5f,
            depth * 0.5f,
            0.0f
        );

        Vector4 voxelPos,voxelCenter;
        Vector4 minCorner = aabb.corners[0];
     
        voxelPos = minCorner + new Vector4(
            width * x,
            height * y,
            depth * z,
            0.0f
        );

        drawVoxelWire(voxelPos, width, height, depth);

        if (draw)
        {
            voxelCenter = voxelPos + centerOffset;
            Matrix4x4 fitMat = calcFitMat(voxelCenter, width, height, depth);
            int index = calculateVoxelIndex(fitMat);
            Matrix4x4 rotMat = marchingCubesMatrixTable[index];
            int patternOffset = patternOffsetTable[marchingCubesTable[index]];
            transferPatternToVBO(patternOffset, fitMat * rotMat);
            draw = false;
            gp.UpdateMesh(ref mesh);
        }
    }

    public static void renderGrid(AABB aabb,int xSlices,int ySlices,int zSlices)
    {
        Vector4 size = aabb.getBoxSize();
        float width = size.x / (float)xSlices;
        float height = size.y / (float)ySlices;
        float depth = size.z / (float)zSlices;

        Vector4 centerOffset = new Vector4(
            width * 0.5f,
            height * 0.5f,
            depth * 0.5f,
            0.0f
        );

        Vector4 voxelPos;
        Vector4 minCorner = aabb.corners[0];
        int x, y, z;
     
        
        for (x = 0; x < xSlices; x++)
        {
            for (y = 0; y < ySlices; y++)
            {
                for (z = 0; z < zSlices; z++)
                {
                    voxelPos = minCorner + new Vector4(
                        width * x,
                        height * y,
                        depth * z,
                        0.0f
                    );

                    drawVoxelWire(voxelPos, width, height, depth);
                }
            }
        }
    }

    void drawDebugBoxes(float width,float height,float depth)
    {
        Vector4 centerOffset = new Vector4(
           width * 0.5f,
           height * 0.5f,
           depth * 0.5f,
           0.0f
       );
    
        float x;
        Matrix4x4 fitMat;
        for (int K = 0; K < 15; K++)
        {
            x = width * (K + 10);
            fitMat = calcFitMat(new Vector4(x, 0, 0, 0), width, height, depth);
            drawVoxelWire(new Vector4(x-width/2, -height/2, -depth/2, 0), width, height, depth);
        }
    }

    void DebugCases(float width,float height, float depth) 
    {
        Vector4 centerOffset = new Vector4(
            width * 0.5f,
            height * 0.5f,
            depth * 0.5f,
            0.0f
        );
        int patternOffset = 0;
        float x;
        Matrix4x4 fitMat; 
        for (int K = 0; K < 15; K++)
        {
            x = width * (K+10);
            fitMat = calcFitMat(new Vector4(x, 0, 0, 0), width, height, depth);
            patternOffset = patternOffsetTable[K];
            transferPatternToVBO(patternOffset, fitMat);
        }
    }

    void March(AABB aabb,SDFEvaluator sdfEval,int xSlices,int ySlices,int zSlices){
        Vector4 size = aabb.getBoxSize();
        float width = size.x / (float)xSlices;
        float height = size.y / (float)ySlices;
        float depth = size.z / (float)zSlices;

        Vector4 centerOffset = new Vector4(
            width * 0.5f, 
            height * 0.5f, 
            depth * 0.5f,
            0.0f
        );
      
        Vector4 voxelPos,voxelCenter;
        Vector4 minCorner = aabb.corners[0];
        int x, y, z;
        x = dx; y = dy; z = dz; 

        for (x = 0; x < xSlices; x++)
        {
            for(y = 0; y < ySlices; y++)
            {
                for(z = 0 ; z < zSlices; z++)
                {
                    voxelPos = minCorner + new Vector4(
                                    width*x,
                                    height*y,
                                    depth*z,
                                    0.0f    
                    );
                    voxelCenter = voxelPos + centerOffset;
                    Matrix4x4 fitMat = calcFitMat(voxelCenter, width, height, depth);
                    int index = calculateVoxelIndex(fitMat);
                    bool isInverted = NumberOfSetBits(index & 0xFF) > 4;
                    Matrix4x4 rotMat = marchingCubesMatrixTable[index];

                    int caseOffset = marchingCubesTable[index];
                    int patternOffset = patternOffsetTable[caseOffset];
                    int complementOffset = complementPatternOffsetTable[caseOffset];

                    //use complement cases to resolve ambiguity
                    if (  isInverted && complementOffset > 0 )
                    {
                        patternOffset = patternOffsetTable[complementOffset];
                    }
                    transferPatternToVBO(patternOffset, fitMat * rotMat);
                }
            }
        }
    }

    static void drawLine(Vector4 a,Vector4 b)
    {
       Debug.DrawLine(a,b,Color.red);
    }

    static void drawVoxelWire(Vector4 V,float width,float height,float depth)
    {
        Vector4 RIGHT = new Vector4(width, 0, 0, 0);
        Vector4 UP = new Vector4(0, height, 0, 0);
        Vector4 IN = new Vector4(0, 0, depth, 0);
        Vector4 TL0, TR0, BL0, BR0, TL1, TR1, BL1, BR1;
        TL0 = V + IN;
        TR0 = V + RIGHT + IN;
        BL0 = V;
        BR0 = V + RIGHT;
        TL1 = V + IN + UP;
        TR1 = V + RIGHT + IN + UP;
        BL1 = V + UP;
        BR1 = V + RIGHT + UP;

        /*for(int K = 0; K < 8; K++)
        {
            debugSpheres[K].transform.position = Vector3.zero;
        }

        if(sdf.EvaluateSDF(TL0) < 0)
        {
            debugSpheres[0].transform.position = TL0; 
        }

        if (sdf.EvaluateSDF(TR0) < 0)
        {
            debugSpheres[1].transform.position = TR0;
        }

        if (sdf.EvaluateSDF(BL0) < 0)
        {
            debugSpheres[2].transform.position = BL0;
        }

        if (sdf.EvaluateSDF(BR0) < 0)
        {
            debugSpheres[3].transform.position = BR0;
        }


        if (sdf.EvaluateSDF(TL1) < 0)
        {
            debugSpheres[4].transform.position = TL1;
        }

        if (sdf.EvaluateSDF(TR1) < 0)
        {
            debugSpheres[5].transform.position = TR1;
        }

        if (sdf.EvaluateSDF(BL1) < 0)
        {
            debugSpheres[6].transform.position = BL1;
        }

        if (sdf.EvaluateSDF(BR1) < 0)
        {
            debugSpheres[7].transform.position = BR1;
        }*/




        drawLine(TL0,TR0);
        drawLine(BL0,BR0);
        drawLine(TL0,BL0);
        drawLine(TR0,BR0);

        drawLine(TL1, TR1);
        drawLine(BL1, BR1);
        drawLine(TL1, BL1);
        drawLine(TR1, BR1);

        drawLine(TL0, TL1);
        drawLine(BL0, BL1);
        drawLine(TR0, TR1);
        drawLine(BR0, BR1);
    }

    Vector4 interpolateIntersectingEdge(int pptIndex,float sdfC,Matrix4x4 finalMat){
        int vpAindex = patternPointEdgeTable[2 * pptIndex + 0];
        int vpBindex = patternPointEdgeTable[2 * pptIndex + 1];
        Vector4 vpA = finalMat * new Vector4(
                voxelCornerTable[vpAindex * 4 + 0],
                voxelCornerTable[vpAindex * 4 + 1],
                voxelCornerTable[vpAindex * 4 + 2],
                voxelCornerTable[vpAindex * 4 + 3]
                );
        Vector4 vpB = finalMat * new Vector4(
                voxelCornerTable[vpBindex * 4 + 0],
                voxelCornerTable[vpBindex * 4 + 1],
                voxelCornerTable[vpBindex * 4 + 2],
                voxelCornerTable[vpBindex * 4 + 3]
                );
        float sdfA = sdf.EvaluateSDF(vpA), 
              sdfB = sdf.EvaluateSDF(vpB);

        float t = (sdfC - sdfA) / (sdfB - sdfA);
        t = Mathf.Clamp(t, 0, 1);
        //t = 0.5f;
        return (vpB - vpA) * t + vpA; 
    }

    void transferPatternToVBO(int patternOffset, Matrix4x4 finalTransform){
        int pptIndex;
        Vector4 vertex; 
        while (patternTable[patternOffset] != -1)
        {
            for (int K = 0; K < 3; K++)
            {
                pptIndex = patternTable[patternOffset + K];
                vertex = interpolateIntersectingEdge(pptIndex, 0.0f, finalTransform);
                gp.AddVertex(vertex, sdf.EvaluateGrad(vertex)  );
            }
            patternOffset += 3; 
        }
    }

	// Use this for initialization
	void Start () {
		mesh = new Mesh();
		GetComponent<MeshFilter>().mesh = mesh; 
		gp = new GeoPacker(); 
		sdf = new SDFEvaluator(); 	
		InitTables();
	} 

	// Update is called once per frame 
	void Update () {
        //sdf.t = t;
        AABB box = new AABB(new Vector4(-0.3f, -0.3f, -1.5f, 1), new Vector4(5, 5, 2, 1));
        MarchingCubes.renderGrid(box, 3,3, 3);
        //drawDebugBoxes(5, 5, 5);

        if ((Time.time - T0) > 0.1f)
        {
            gp.ClearBuffers();
            March(box, sdf, 20, 20, 20);
            gp.UpdateMesh(ref mesh);
            sdf.Animate();
            T0 = Time.time;
        }

        renderVoxel(box, 24, 24, 24, dx, dy, dz);
        //sdf.Animate();

        if (draw)
        {
            
            draw = false; 
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
            DenseMatrix a = DenseMatrix.AllocateMatrix(3, 3, 0).SetMatrix(new float[]{
                2,5,7,
                1,2,1,
                5,7,8
                });
            DenseMatrix b = DenseMatrix.AllocateMatrix(3, 1, 0).SetMatrix(new float[]{
                1,
                2,
                3
                });


            //Debug.Log("A = \n" + a);
            //Debug.Log("B = \n" + b);
            DenseMatrix c = (~a*b).DONE();
            //Debug.Log("c = \n" + c);
            
            int mcCount = DenseMatrix.matrixCache[0].Count;
            int tcCount = DenseMatrix.tempCache[0].Count;
            Debug.Log("McCount = "+mcCount + "\ntcCount = " + tcCount +"\n");
            
           

           /* 
            gp.ClearBuffers();
            March(box, sdf,4,4,4 );
            gp.UpdateMesh(ref mesh);
         
            draw = false;*/ 
		}else if (Input.GetKeyDown(KeyCode.D))
        {
            DebugCases(5, 5, 5);
            gp.UpdateMesh(ref mesh);
        }


        //debugSpheres[0].transform.position = box.corners[0];
       // debugSpheres[1].transform.position = box.corners[1];
    }
	
	/*
	Ripped this function off of StackOverflow :
	https://stackoverflow.com/questions/12171584/what-is-the-fastest-way-to-count-set-bits-in-uint32
	*/
	int NumberOfSetBits(int i){
        i = i - ((i >> 1) & 0x55555555);
        i = (i & 0x33333333) + ((i >> 2) & 0x33333333);
        return (((i + (i >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
    }
}
