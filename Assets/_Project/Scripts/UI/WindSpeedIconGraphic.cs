using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// A small vector wind symbol; it stays sharp at any UI scale.
[RequireComponent(typeof(CanvasRenderer))]
public class WindSpeedIconGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Draw(vh, new Vector2(-.43f,.17f), new Vector2(.10f,.17f),
            new Vector2(.40f,.17f),new Vector2(.40f,.46f),new Vector2(.13f,.40f));
        Draw(vh, new Vector2(-.43f,0), new Vector2(.35f,0),
            new Vector2(.64f,0),new Vector2(.62f,.28f),new Vector2(.39f,.23f));
        Draw(vh, new Vector2(-.43f,-.18f), new Vector2(.08f,-.18f),
            new Vector2(.38f,-.18f),new Vector2(.38f,-.47f),new Vector2(.12f,-.40f));
    }

    private void Draw(VertexHelper vh, Vector2 start, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        var points = new List<Vector2> { start, a };
        for (int i=1;i<=14;i++)
        {
            float t=i/14f,u=1-t;
            points.Add(u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d);
        }
        Rect rect=GetPixelAdjustedRect();float size=Mathf.Min(rect.width,rect.height);
        for(int i=0;i<points.Count-1;i++)
        {
            Vector2 p=rect.center+points[i]*size,q=rect.center+points[i+1]*size;
            Vector2 side=Vector2.Perpendicular((q-p).normalized)*size*.032f;
            int first=vh.currentVertCount;
            vh.AddVert(p-side,color,Vector2.zero);vh.AddVert(p+side,color,Vector2.zero);
            vh.AddVert(q+side,color,Vector2.zero);vh.AddVert(q-side,color,Vector2.zero);
            vh.AddTriangle(first,first+1,first+2);vh.AddTriangle(first,first+2,first+3);
        }
    }
}
