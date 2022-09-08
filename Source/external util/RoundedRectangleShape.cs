using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Map_Generator_CSharp.Source.external_util;
using SFML.Graphics;
using SFML.System;

namespace Map_Generator_CSharp.Source.external_util;
public class RoundedRectangleShape : Shape
{
    private Vector2f mySize;
    private float myRadius;
    private uint myCornerPointCount;
    public RoundedRectangleShape(Vector2f size, float radius, uint cornerPointCount)
    {
        mySize = size;
        myRadius = radius;
        myCornerPointCount = cornerPointCount;
        Update();
    }
    public void setSize(Vector2f size)
    {
        mySize = size;
        Update();
    }

    public Vector2f getSize()
    {
        return mySize;
    }

    public void setCornersRadius(float radius)
    {
        myRadius = radius;
        Update();
    }
    public float getCornersRadius()
    {
        return myRadius;
    }
    public void setCornerPointCount(uint count)
    {
        myCornerPointCount = count;
        Update();
    }

    public override uint GetPointCount()
    {
        return myCornerPointCount*4;
    }
    public override Vector2f GetPoint(uint index)
    {
    if(index >= myCornerPointCount*4)
        return new Vector2f(0,0);

    float deltaAngle = 90.0f / (myCornerPointCount - 1);
    Vector2f center = new Vector2f();
    uint centerIndex = index / myCornerPointCount;
    const float pi = 3.141592654f;

    switch(centerIndex)
    {
        case 0: center.X = mySize.X - myRadius; center.Y = myRadius; break;
        case 1: center.X = myRadius; center.Y = myRadius; break;
        case 2: center.X = myRadius; center.Y = mySize.Y - myRadius; break;
        case 3: center.X = mySize.X - myRadius; center.Y = mySize.Y - myRadius; break;
    }

    return new Vector2f((float)(myRadius * Math.Cos(deltaAngle*(index-centerIndex)* pi/180)+center.X),
                        (float)(-myRadius* Math.Sin(deltaAngle*(index-centerIndex)* pi/180)+center.Y));
    }
}