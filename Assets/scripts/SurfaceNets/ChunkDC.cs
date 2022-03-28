using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkDC : MonoBehaviour {

    private GeoPacker gp;
    private Mesh mesh;
    private DCVoxel[] grid;
    private List<DCEdge> edgeList = new List<DCEdge>();
    private int[,] edgeLookup;
    private SDFEvaluator sdfe = new SDFEvaluator();
    private int xSlices;
    private int ySlices;
    private int zSlices;
    private AABB initbox; 
    private AABB aabb; 
    PointOctree pointTree;


    public void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        gp = new GeoPacker();
        xSlices = 32;
        ySlices = 32;
        zSlices = 32;
        this.grid = new DCVoxel[xSlices * ySlices* zSlices];


        for (int K = 0; K < grid.Length; K++)
        {
            grid[K] = new DCVoxel();
        }

        this.aabb = new AABB(new Vector4(-2f, -1f, -2f, 1), new Vector4(5, 5, 2, 1));
        this.initbox = aabb.clone();

        pointTree = new PointOctree(aabb);
        
       
    }

    private int GI(int X,int Y, int Z)
    {
        return X + Y * xSlices + Z * xSlices * ySlices;
    }

    private int GIsafe(int X, int Y, int Z)
    {
        if (X < 0 || X > xSlices - 1) return -1;
        if (Y < 0 || Y > ySlices - 1) return -1;
        if (Z < 0 || Z > zSlices - 1) return -1;
        return X + Y * xSlices + Z * xSlices * ySlices;
       
    }
    
    public void Update()
    {
        Transform trans = GetComponent<Transform>();
        this.aabb =initbox.clone().translate(trans.position);

        MarchingCubes.renderGrid(aabb, 1, 1, 1);

        if (Input.GetKeyDown(KeyCode.Space))
        {

            gp.ClearBuffers();
            pointTree.clear(aabb);//update aabb for the octree
            initChunkEdges(aabb);
            extract();
            gp.UpdateMesh(ref mesh);
            
            Debug.Log("Triangles = " + gp.getNumVerts()/3);
        }
    }

    private void initChunkEdges(AABB aabb)
    {

        Vector4 size = aabb.getBoxSize();
        float width = size.x  / (float)xSlices;
        float height = size.y / (float)ySlices;
        float depth = size.z  / (float)zSlices;

        Vector4 centerOffset = new Vector4(
            width * 0.5f,
            height * 0.5f,
            depth * 0.5f,
            0.0f
        );

        Vector4 voxelPos;
        Vector4 minCorner = aabb.corners[0];
        int x, y, z;
        /*
                  TL0,TR0,    0 1  
                  BL0,BR0,    2 3 
                  TL1,TR1,    4 5
                  BL1,BR1,    6 7
                  */
        const int TL0 = 0, TR0 = 1,
                  BL0 = 2, BR0 = 3,
                  TL1 = 4, TR1 = 5,
                  BL1 = 6, BR1 = 7;    
                  
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
                    DCVoxel vox = grid[GI(x, y, z)];
                    Vector4[] ec = GenerateEdgesVerts(voxelPos, width, height, depth);

                    /*RegisterEdge(TL0, TR0);
                    RegisterEdge(BL0, BR0);
                    RegisterEdge(TL0, BL0);
                    RegisterEdge(TR0, BR0);

                    RegisterEdge(TL1, TR1);
                    RegisterEdge(BL1, BR1);
                    RegisterEdge(TL1, BL1);
                    RegisterEdge(TR1, BR1);

                    RegisterEdge(TL0, TL1);
                    RegisterEdge(BL0, BL1);
                    RegisterEdge(TR0, TR1);
                    RegisterEdge(BR0, BR1);*/

                    /*
                    TL0,TR0,    0 1  
                    BL0,BR0,    2 3 
                    TL1,TR1,    4 5
                    BL1,BR1,    6 7
                    */

                    vox.edgePtrList[4] = RegisterEdge(aabb,ec[TL0],ec[TR0],
                                         new int[] {
                                            GIsafe(x+0,y+0,z+0),
                                            GIsafe(x+0,y-1,z+0),
                                            GIsafe(x+0,y-1,z+1),
                                            GIsafe(x+0,y+0,z+1),
                                         });//DONE 

                    vox.edgePtrList[6] = RegisterEdge(aabb,ec[TL1], ec[TR1],
                                            new int[] {
                                                GIsafe(x+0,y+0,z+0),
                                                GIsafe(x+0,y+0,z+1),
                                                GIsafe(x+0,y+1,z+1),
                                                GIsafe(x+0,y+1,z+0),
                                            });//DONE


                    vox.edgePtrList[0] = RegisterEdge(aabb,ec[BL0], ec[BR0],
                                         new int[] {
                                            GIsafe(x+0,y+0,z+0),
                                            GIsafe(x+0,y+1,z+0),
                                            GIsafe(x+0,y+1,z-1),
                                            GIsafe(x+0,y+0,z-1),
                                         });//DONE 

                    vox.edgePtrList[8] = RegisterEdge(aabb,ec[TL0], ec[BL0],
                                         new int[] {
                                            GIsafe(x+0,y+0,z+0),
                                            GIsafe(x+0,y+1,z+0),
                                            GIsafe(x-1,y+1,z+0),
                                            GIsafe(x-1,y+0,z+0),
                                         }); //DONE 

                    vox.edgePtrList[9] = RegisterEdge(aabb,ec[TR0], ec[BR0],
                                         new int[] {
                                            GIsafe(x+0,y+0,z+0),
                                            GIsafe(x+0,y-1,z+0),
                                            GIsafe(x+1,y-1,z+0),
                                            GIsafe(x+1,y-0,z+0),
                                         });//DONE 

                    vox.edgePtrList[2] = RegisterEdge(aabb,ec[BL1], ec[BR1],
                                         new int[] {
                                            GI(x+0,y+0,z+0),
                                            GI(x+0,y+1,z+0),
                                            GI(x+0,y+1,z-1),
                                            GI(x+0,y+0,z-1),
                                         });//DONE 


                    vox.edgePtrList[10] = RegisterEdge(aabb,ec[TL1], ec[BL1],
                                         new int[] {
                                            GI(x+0,y+0,z+0),
                                            GI(x+0,y+1,z+0),
                                            GI(x-1,y+1,z+0),
                                            GI(x-1,y+0,z+0),
                                         });//DONE 

                    vox.edgePtrList[11] = RegisterEdge(aabb,ec[TR1], ec[BR1],
                                         new int[] {
                                            GI(x+0,y+0,z+0),
                                            GI(x+1,y+0,z+0),
                                            GI(x+1,y+1,z+0),
                                            GI(x+0,y+1,z+0),
                                         });//DONE 

                    vox.edgePtrList[7] = RegisterEdge(aabb,ec[TL0], ec[TL1],
                                         new int[] {
                                            GI(x+0,y+0,z+0),
                                            GI(x+0,y+0,z+1),
                                            GI(x-1,y+0,z+1),
                                            GI(x-1,y+0,z+0),
                                         });//DONE 

                    vox.edgePtrList[3] = RegisterEdge(aabb,ec[BL0], ec[BL1],
                                         new int[] {
                                            GI(x+0,y+0,z+0),
                                            GI(x-1,y+0,z+0),
                                            GI(x-1,y+0,z-1),
                                            GI(x+0,y+0,z-1),
                                         });//DONE 

                    vox.edgePtrList[5] = RegisterEdge(aabb,ec[TR0], ec[TR1],
                                         new int[] {
                                            GIsafe(x+0,y+0,z+0),
                                            GIsafe(x+1,y+0,z+0),
                                            GIsafe(x+1,y+0,z+1),
                                            GIsafe(x+0,y+0,z+1),
                                         });//DONE 

                    vox.edgePtrList[1] = RegisterEdge(aabb,ec[BR0], ec[BR1],
                                         new int[] {
                                            GI(x+0,y+0,z+0),
                                            GI(x+0,y+0,z-1),
                                            GI(x+1,y+0,z-1),
                                            GI(x+1,y+0,z+0),
                                         });//DONE 
                }
            }
        }
    }
    void removeEdgesWithEmptyVoxels()
    {
        //for each voxel count intersections 
        for (int K = 0; K < grid.Length; K++)
        {
            DCVoxel vox = grid[K];
            vox.isEmpty = false;//initialize this flag to false  
            int intersectCount = 0;
            for(int M = 0; M < vox.edgePtrList.Length; M++)
            {
                DCEdge dce = edgeList[vox.edgePtrList[M]];
                if( (dce.mask&DCEdge.INTERSECTING) == 1)
                {
                    intersectCount++;
                    break;//if we find just one intersection we move on to the next voxel
                }
            }
            //if there are no intersections mark voxel as 'empty'
            if(intersectCount == 0)
            {
                vox.isEmpty = true; 
            }
        }

        //Iterate through edge lis and mark intersecting edges
        //that are adjacent to empty voxels as 'not intersecting' 
        for(int I = 0; I < edgeList.Count; I++)
        {
            DCEdge dce = edgeList[I]; 

            for(int K = 0; K < dce.adjVoxels.Length; K++)
            {
                if ( dce.isOutBounds == false && grid[dce.adjVoxels[K]].isEmpty)
                {
                    dce.mask &= ~DCEdge.INTERSECTING;//unset bit 
                    break;
                }
            }
        }
    }

    /*
     * 
     * f(c) = f(b)-f(a)*t + f(a) 
     * t = f(c)-f(a)/[ f(b) - f(a)];
     * 
     */
    private unsafe void calculateEdgeIntersections(){
        for(int K = 0; K < edgeList.Count; K++){
            DCEdge dce = edgeList[K];

            //clear bit mask 
            dce.mask = 0;

            //Solve for F(x,y,z) = 0 along the edge using linear interpolation
            //Store point of intersection and normal associated with it  in the DCEdge object
            float fa = sdfe.EvaluateSDF(dce.p0),
                  fb = sdfe.EvaluateSDF(dce.p1),
                  fc = 0.0f; 
            float t = (fc-fa)/(fb-fa);
         
            if ( (fa < 0.0f) != (fb <0.0f) )
            {
                dce.mask = 1; 
            }

            dce.intersectionPoint = (dce.p1 - dce.p0)*t + dce.p0;
            dce.normal = sdfe.EvaluateGrad(dce.intersectionPoint);
        }
    }
    public void calculateVoxelQEF()
    {
        Vector4 sum;
        int numEdges;
        for(int K = 0; K < grid.Length; K++)
        {
            DCVoxel vox = grid[K];

            sum = Vector4.zero;
            numEdges = 0;
            for(int J = 0; J < vox.edgePtrList.Length; J++)
            {
                int edgePtr = vox.edgePtrList[J];

                if ( edgeList[edgePtr].mask > 0)
                {
                    sum += edgeList[edgePtr].intersectionPoint;
                    numEdges++;
                }

                //sum += (edgeList[edgePtr].p0 + edgeList[edgePtr].p1) * 0.5f;
            }
            //sum *= 1.0f / (float)vox.edgePtrList.Length;
            sum *= 1.0f / (float)numEdges;
            vox.qefVertex = sum ; 
        }
    }
    void extract()
    {
        gp.ClearBuffers();
        calculateEdgeIntersections();
        removeEdgesWithEmptyVoxels();
        calculateVoxelQEF();
        generateGeometry();
    }
    void generateGeometry(){
        int[] importantEdges = new int[]{4,5,9};
        
        for(int K = 0; K < grid.Length; K++)
        {
            DCVoxel vox = grid[K];
            if (vox.isEmpty == false)
            {
                for(int E = 0; E < 3; E++)
                {
                    if( traverseEdge(vox.edgePtrList[importantEdges[E]]))
                    {
                        
                    }
                }        
            }
        }
    }
    /*
     * returns true if edge was traversed and false if it was skipped over
     */
    bool traverseEdge(int edgeIndex)
    {
        Vector4 qefVert;
        int[] adjVoxels;
        int K = edgeIndex;//cuz im too lazy to rename 
        Vector4 transformPos = GetComponent<Transform>().position;

        if (edgeList[K].mask > 0 && edgeList[K].isOutBounds == false)
        {
            adjVoxels = edgeList[K].adjVoxels;

            for (int L = 0; L < 3; L++)
            {
                qefVert = grid[adjVoxels[L]].qefVertex;
                gp.AddVertex(qefVert-transformPos, sdfe.EvaluateGrad(qefVert));
            }
            for (int L = 2; L < 5; L++)
            {
                qefVert = grid[adjVoxels[L & 3]].qefVertex;
                gp.AddVertex(qefVert - transformPos, sdfe.EvaluateGrad(qefVert));
            }
            return true;
        }
        return false; 
    }

    /*
     * Search for edge and returns its position in the edge array. 
     * If the search fails the edge is added to the array and its index is returned 
     */
    private int RegisterEdge(AABB aabb,Vector4 e0, Vector4 e1,int[] adjList){
        Vector4 mid = (e0 + e1) * 0.5f;
        //search for matching edge and return index 
        int query = pointTree.search(mid);
        if(query != -1)
        {
            return query;
        }
        
        /*const float EPSILON = 0.001f;
        for (int K = 0; K < edgeList.Count; K++)
        {
            DCEdge e = edgeList[K];
            if ( ( (e.p0 - e0).SqrMagnitude() < EPSILON && (e.p1 - e1).SqrMagnitude() < EPSILON) ||
                 ( (e.p0 - e1).SqrMagnitude() < EPSILON && (e.p1 - e0).SqrMagnitude() < EPSILON)  ){
               return K;
            }
        }*/
     


        DCEdge newEdge = new DCEdge(e0, e1,0,adjList);
        for (int K = 0; K < 4; K++)
        {
            if (adjList[K] > grid.Length-1 || adjList[K] <= -1)
            {
                newEdge.isOutBounds = true;
            }
        }

        edgeList.Add(newEdge);
        pointTree.insert(mid, edgeList.Count - 1);
        return edgeList.Count-1; 
    }


    private Vector4[] GenerateEdgesVerts(Vector4 V,float width,float height,float depth)
    {
        Vector4 RIGHT= new Vector4(width,      0, 0    , 0);
        Vector4 UP   = new Vector4(0    , height, 0    , 0);
        Vector4 IN   = new Vector4(0    ,      0, depth, 0);
        Vector4 TL0, TR0, BL0, BR0, TL1, TR1, BL1, BR1;
        TL0 = V + IN;
        TR0 = V + RIGHT + IN;
        BL0 = V;
        BR0 = V + RIGHT;
        TL1 = V + IN + UP;
        TR1 = V + RIGHT + IN + UP;
        BL1 = V + UP;
        BR1 = V + RIGHT + UP;

        return new Vector4[]
        {
            TL0,TR0,
            BL0,BR0,
            TL1,TR1,
            BL1,BR1,
        };
    }
}
