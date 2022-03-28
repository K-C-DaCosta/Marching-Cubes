using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct DenseMatrix  {
    private float[] data; 
    private int rows;
    private int cols;
    public  int id;

    public static Stack<DenseMatrix>[] matrixCache = new Stack<DenseMatrix>[16];
    public static Stack<DenseMatrix>[] tempCache = new Stack<DenseMatrix>[16];

    private const int MAX_ROWS = 8;
    private const int MAX_COLS = 8;
    private const int MAX_CAPACITY = MAX_ROWS * MAX_COLS;

    public static void InitMatrixCache()
    {
        for (int K = 0; K < 16; K++)
        {
            matrixCache[K] = new Stack<DenseMatrix>();
            tempCache[K] = new Stack<DenseMatrix>();
        }
    }


    /*
     * This procedure simply caches the matrix for reuse in the future
     */
    private static void FreeMatrix(DenseMatrix mat)
    {
        int id = mat.id; 
        matrixCache[id].Push(mat);
    }

    public static DenseMatrix AllocateMatrix(int rows,int cols,int id)
    {
       Stack<DenseMatrix> cache = matrixCache[id];
        if( cache.Count > 0)
        {
            DenseMatrix dm = cache.Pop();
            dm.rows = rows;
            dm.cols = cols;
            dm.id = id;
            return dm; 
        }
        else
        {
            return new DenseMatrix(rows, cols, id);
        }
    }


    public DenseMatrix(int rows, int cols){
        this.rows = rows;
        this.cols = cols;
        this.id = 0; 
        data = new float[MAX_CAPACITY];
    }
    public DenseMatrix(int rows, int cols, int id)
    {
        this.rows = rows;
        this.cols = cols;
        this.id = id;
        data = new float[MAX_CAPACITY];
    }

    private int GI(int I, int J){
        return I * cols + J; 
    }

    public DenseMatrix SetMatrix(DenseMatrix mat)
    {
        this.SetMatrix(mat.data);
        return this; 
    }

    public DenseMatrix SetMatrix(float[] data)
    {
        for(int I = 0; I < data.Length; I++)
        {
            this.data[I] = data[I];
        }
        return this; 
    }


    public DenseMatrix MakeZero()
    {
        for(int I = 0; I < data.Length; I++)
        {
            data[I] = 0.0f; 
        }
        return this;
    }
  

    /*
     * Calculates inverse by first doing LU decomp and then solving for A^-1 column by column 
     * The asymptotic running time should be on the order of O(n^3) , which is better than other bruteforce methods 
     */
    public DenseMatrix invert()
    {
        /*DenseMatrix b = new DenseMatrix(rows, 1,id).MakeZero();
        DenseMatrix L = this.LUDecomp();
        DenseMatrix U = this;
        DenseMatrix inverse = new DenseMatrix(rows, cols,id);*/

        DenseMatrix b = AllocateMatrix(rows,1,id).MakeZero();
        DenseMatrix L = this.LUDecomp();
        DenseMatrix U = this;
        DenseMatrix inverse = AllocateMatrix(rows,cols,id); 

        for (int K = 0; K < rows; K++)
        {
            b.data[K] = 1.0f; 

            if(K > 0)
            {
                b.data[K - 1] = 0.0f; 
            }

            ForwardSub(ref L, ref b);
            BackSub(ref U, ref b);

            //copy the column of the inverse (b) into the result matrix(a.k.a inverse) 
            for (int I = 0; I < rows; I++)
            {
                inverse.data[inverse.GI(I, K)] = b.data[I];
                b.data[I] = 0.0f; 
            }
        }

        FreeMatrix(L);
        FreeMatrix(b);

        //Debug.Log("L=\n " + L + "\nU=\n" + U + "\nInverse=\n" + inverse);
    
        return inverse;
    }


    /*
     * Returns a lower triangular Matrix
     * and converts the current object to upper triangular
     * [WARNING:this decomposition method is numerically unstable]
     */
    public DenseMatrix LUDecomp(){
        float inv_pivot;

        //DenseMatrix lower = new DenseMatrix(rows, cols,id).MakeZero();
        DenseMatrix lower = AllocateMatrix(rows,cols,id).MakeZero();

        //converts the current DenseMatrix to upper triangular 
        for (int K = 0; K < rows; K++)
        {
            
            lower.data[GI(K, K)] = 1.0f; // <- sets the diagonal of the lower matrix 

         
            inv_pivot = 1.0f / data[GI(K, K)];

            for (int I = K + 1; I < rows; I++)
            {
                float X = -data[GI(I, K)] * inv_pivot;
                lower.data[GI(I, K)] = -X;
                data[GI(I, K)] = 0.0f; 

                for (int J = K+1; J < cols; J++)
                {
                    data[GI(I, J)] += data[GI(K, J)] * X;
                }
            }
        }

        return lower; 
    }

    public override string ToString()
    {
        string result = "";
        for(int I = 0; I < rows; I++)
        {
            for(int J = 0; J < cols; J++)
            {
                result += (J == 0) ? "[" + data[GI(I, J)] : "," + data[GI(I, J)];
            }
            result += "]\n";
        }
        return result;
    }

    /*
     * upper: is a dense matrix that is upper triangular 
     * b: is a column vector that has has row operations applied to it
     */
    static void BackSub(ref DenseMatrix upper,ref DenseMatrix b){

        for (int I = upper.rows-1; I > -1; I--)
        {
            for(int J = upper.cols-1; J > I ; J--)
            {
                b.data[b.GI(I, 0)] -= upper.data[upper.GI(I, J)] * b.data[b.GI(J, 0)];
            }
            b.data[b.GI(I, 0)] /= upper.data[upper.GI(I, I)];
        }
    }
    /*
     * Does forward substitution 
     */
    static void ForwardSub(ref DenseMatrix lower, ref DenseMatrix b){
        for (int I = 1; I < lower.rows; I++)
        {
            for (int J = 0; J < I; J++)
            {
                b.data[b.GI(I, 0)] -= lower.data[lower.GI(I, J)] * b.data[b.GI(J, 0)];
            }
        }
    }
    /*
     * Preforms partial pivoting on the K'th column 
     * This operation is done for better numerical stability 
     */
    static void PartialPivot(ref DenseMatrix a, ref DenseMatrix b,int K)
    {
        float temp,element,maxRow = a.data[a.GI(K, K)];
        int I,J,maxRowIndex = K;
         
        //iterate down the K'th column finding the maximum element
        for (I = K+1; I < a.rows; I++)
        {
            element = Mathf.Abs(a.data[a.GI(I, K)]);
            if ( element > maxRow )
            {
                maxRow = element;
                maxRowIndex = I; 
            }         
        }

        //if new maximum is found swap 
        if (maxRowIndex != K)
        {
            //iterate down the rows swapping the max elements with elements in the Kth row 
    
            for(J = 0; J < a.cols; J++)
            {
                temp = a.data[a.GI(K, J)];
                a.data[a.GI(K, J)] = a.data[a.GI(maxRowIndex, J)];
                a.data[a.GI(maxRowIndex, J)] = temp; 
            }

            temp = b.data[b.GI(K, 0)];
            b.data[b.GI(K, 0)] = b.data[b.GI(maxRowIndex, 0)];
            b.data[b.GI(maxRowIndex, 0)] = temp;
        }
    }
    /*
     * Preforms partial pivoting on the K'th column 
     * This operation is done for better numerical stability 
     * This version only works on matrix a
     */
    static void PartialPivot(ref DenseMatrix a, int K)
    {
        float temp, element, maxRow = a.data[a.GI(K, K)];
        int I, J, maxRowIndex = K;

        //iterate down the K'th column finding the maximum element
        for (I = K + 1; I < a.rows; I++)
        {
            element = Mathf.Abs(a.data[a.GI(I, K)]);
            if (element > maxRow)
            {
                maxRow = element;
                maxRowIndex = I;
            }
        }

        //if new maximum is found swap 
        if (maxRowIndex != K)
        {
            //iterate down the rows swapping the max elements with elements in the Kth row 
            for (J = 0; J < a.cols; J++)
            {
                temp = a.data[a.GI(K, J)];
                a.data[a.GI(K, J)] = a.data[a.GI(maxRowIndex, J)];
                a.data[a.GI(maxRowIndex, J)] = temp;
            }
        }
    }



    /*
        This solves the equation Ax = b 
        and returns the column vector x as the result
     
        A and b are "consumed" and generally should not be used again for future calculations
        A is an N-by-N matrix 
        b is a N-by-1 matrix 
    */
    public static DenseMatrix operator /(DenseMatrix a, DenseMatrix b)
    {

        float inv_pivot;
        for (int K = 0; K < a.rows; K++)
        {

            PartialPivot(ref a, ref b, K);

            inv_pivot = 1.0f/a.data[a.GI(K, K)];

            for(int I = K+1; I < a.rows; I++)
            {
                float X = -a.data[a.GI(I, K)]*inv_pivot;
                a.data[a.GI(I, K)] = 0.0f; 
                for (int J = K+1; J < a.cols; J++)
                {
                    a.data[a.GI(I, J)] += a.data[a.GI(K, J)] * X;
                }
                b.data[b.GI(I, 0)] += b.data[b.GI(K, 0)]*X;
            }
        }


        Debug.Log("A =\n " + a + " B = \n" + b);

        BackSub(ref a, ref b);

        Debug.Log("A =\n " + a + " B = \n" + b);

        return b; 
    }

    // c = a*b  ( matrix multiplication) 
    public static DenseMatrix operator* (DenseMatrix a,DenseMatrix b)
    {
        int M = a.rows;
        int N = b.cols;
        //DenseMatrix result = new DenseMatrix(a.rows, b.cols,a.id).MakeZero();
        
        DenseMatrix result = AllocateMatrix(M,N, a.id).MakeZero();


        for (int I = 0; I < M; I++)
        {
            for(int J = 0;  J < N; J++)
            {
                for(int K = 0; K < a.cols; K++)
                {
                    result.data[result.GI(I, J)] += a.data[a.GI(I, K)]*b.data[b.GI(K, J)];
                }
            }
        }

        tempCache[a.id].Push(result);

        return result;
    }

    // c = a + b (matrix addition) 
    public static DenseMatrix operator+ (DenseMatrix a, DenseMatrix b)
    {
        int M = a.rows;
        int N = b.cols;
        //DenseMatrix result = new DenseMatrix(M, N,a.id);
        DenseMatrix result = AllocateMatrix(M, N, a.id);

        for (int I = 0; I < M; I++)
        {
            for(int J = 0; J < N; J++)
            {
                result.data[result.GI(I, J)] = a.data[a.GI(I, J)] + b.data[b.GI(I, J)];
            }
        }

        tempCache[a.id].Push(result);

        return result; 
    }
    // ( matrix subtraction )
    //Example: c = (a - b).DONE(); 
    public static DenseMatrix operator -(DenseMatrix a, DenseMatrix b)
    {
        int M = a.rows;
        int N = b.cols;
        //DenseMatrix result = new DenseMatrix(M, N,a.id);
        DenseMatrix result = AllocateMatrix(M, N, a.id).MakeZero();
        for (int I = 0; I < M; I++)
        {
            for (int J = 0; J < N; J++)
            {
                result.data[result.GI(I, J)] = a.data[a.GI(I, J)] - b.data[b.GI(I, J)];
            }
        }
        tempCache[a.id].Push(result);
        return result;
    }
    /*
     * When you're finished with a matrix calculation make sure the last 
     * intermediate matrix(the result of the calculation) isn't being aliased in two different locations
     */
    public DenseMatrix DONE()
    {
        DenseMatrix result = tempCache[id].Pop();
        while(tempCache[id].Count > 0)
        {
           FreeMatrix(tempCache[id].Pop());
        }
        return result;
    }


    /*
   * Transposes matrix
   */
    public static DenseMatrix operator !(DenseMatrix a)
    {
        //DenseMatrix result = new DenseMatrix(a.cols, a.rows);
        DenseMatrix result = AllocateMatrix(a.cols,a.rows,a.id).MakeZero();

        for (int I = 0; I < a.rows; I++)
        {
            for(int J = 0; J < a.cols; J++)
            {
                result.data[result.GI(J, I)] = a.data[a.GI(I, J)];
            }
        }

        tempCache[a.id].Push(result);
        return result; 
    }

    /*
    * Inverts matrix  
    * Example 
    * Result = (~A).DONE(); 
    */
    public static DenseMatrix operator ~(DenseMatrix a)
    {
        DenseMatrix copyA = AllocateMatrix(a.rows, a.cols, a.id).SetMatrix(a);
        DenseMatrix result = copyA.invert();
        FreeMatrix(copyA);
        tempCache[a.id].Push(result);
        return result;
    }
}
