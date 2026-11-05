using UnityEngine;
using UnityEngine.UI;

public class MapPlayerArrow : Graphic
{
    protected MapPlayerArrow() { useLegacyMeshGeneration=false; }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        var r=rectTransform.rect;
        Vector2[] points={new Vector2(0,r.height*.5f),new Vector2(-r.width*.5f,-r.height*.5f),new Vector2(0,-r.height*.2f),new Vector2(r.width*.5f,-r.height*.5f)};
        foreach(var point in points)mesh.AddVert(point,color,Vector2.zero);
        mesh.AddTriangle(0,2,1);mesh.AddTriangle(0,3,2);
    }
}
