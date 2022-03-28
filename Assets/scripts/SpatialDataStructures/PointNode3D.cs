using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointNode3D {
    public PointNode3D parent = null;
    public PointNode3D[] children = null;
    public AABB aabb;
    public Vector4 key = Vector4.positiveInfinity;
    public int val = int.MaxValue;
    public bool containsPoint = false;

    public static readonly int BL0 = 0,
                               BR0 = 1,
                               TL0 = 2,
                               TR0 = 3,
                               BL1 = 4,
                               BR1 = 5,
                               TL1 = 6,
                               TR1 = 7;

    public PointNode3D(AABB aabb)
    {
        this.aabb = aabb;
    }

    public void subDivide()
    {
        this.children = new PointNode3D[8];
        Vector4 halfsize = aabb.getBoxSize() * 0.5f;
        Vector4 RIGHT = new Vector4(halfsize.x, 0, 0, 0);
        Vector4 UP = new Vector4(0, halfsize.y, 0, 0);
        Vector4 IN = new Vector4(0, 0, halfsize.z, 0);

        Vector4 vox_max = aabb.min() + halfsize;
        children[BL0] = new PointNode3D(new AABB(aabb.min(), vox_max).translate(Vector4.zero));
        children[BR0] = new PointNode3D(new AABB(aabb.min(), vox_max).translate(RIGHT));
        children[TL0] = new PointNode3D(new AABB(aabb.min(), vox_max).translate(IN));
        children[TR0] = new PointNode3D(new AABB(aabb.min(), vox_max).translate(RIGHT+IN));

        children[BL1] = new PointNode3D(new AABB(aabb.min(), vox_max).translate(Vector4.zero+UP));
        children[BR1] = new PointNode3D(new AABB(aabb.min(), vox_max).translate(RIGHT + UP));
        children[TL1] = new PointNode3D(new AABB(aabb.min(), vox_max).translate(IN + UP));
        children[TR1] = new PointNode3D(new AABB(aabb.min(), vox_max).translate(RIGHT + IN + UP));
    }
    public bool hasPointContained(Vector4 p)
    {
        return p.x > aabb.corners[AABB.MIN].x &&
               p.x < aabb.corners[AABB.MAX].x &&
               p.y > aabb.corners[AABB.MIN].y &&
               p.y < aabb.corners[AABB.MAX].y &&
               p.z > aabb.corners[AABB.MIN].z &&
               p.z < aabb.corners[AABB.MAX].z;
    }
}
