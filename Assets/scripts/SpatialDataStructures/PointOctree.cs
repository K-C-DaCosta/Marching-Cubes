using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointOctree  {
    public AABB aabb;
    PointNode3D root; 

    public PointOctree(AABB aabb)
    {
        this.aabb = aabb.clone();
        //this.aabb.corners[1] += new Vector4(2, 2, 2);
        //this.aabb.corners[0] += new Vector4(-2, -2, -2);
        root = new PointNode3D(this.aabb);
    }

    public void clear(AABB newbox)
    {
        this.aabb = newbox.clone(); 
        root = new PointNode3D(this.aabb.clone());
    }

    public void clear()
    {
        //assume GC will handle it and just make a new root
        root = new PointNode3D(this.aabb.clone());
    }
    public void insert(Vector4 pkey,int val)
    {
        insert_helper(root, pkey, val,0);
    }

    private void insert_helper(PointNode3D node, Vector4 pkey, int val,int depth)
    {
        if (depth > 8)
        {
            return; 
        }

        if (node.hasPointContained(pkey))
        {
            if(node.children == null && node.containsPoint)
            {
                node.subDivide();
                for(int K = 0; K < 8; K++)
                {
                    insert_helper(node.children[K], pkey,val,depth+1);
                    insert_helper(node.children[K], node.key, node.val,depth+1);
                } 
            }else
            {
                node.key = pkey;
                node.val = val;
                node.containsPoint = true; 
            }
        }
    }

    public int search(Vector4 pkey)
    {
        return search_helper(root, pkey);
    }
    private int search_helper(PointNode3D node,Vector4 pkey)
    {
        int query_val = -1;

        if (node.hasPointContained(pkey) == false)
        {
            return -1;
        } else if( node.parent == null)
        {
            if ((pkey - node.key).sqrMagnitude < 0.0001f)
            {
                return node.val;
            }
            else
            {
                return -1;
            }
        }
        else
        {
            for (int K = 0; K < 8; K++)
            {
                query_val = search_helper(node.children[K], pkey);
                if (query_val != -1) {
                    return query_val;
                }
            }
        }

        return query_val;
    }
}
