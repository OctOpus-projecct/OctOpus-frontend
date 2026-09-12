using System.Collections.Generic;
using UnityEngine;

public static class SculptedParts
{
    // A continuous swept surface with tapered ends, shared vertices and smooth normals.
    public static GameObject Curve(ArtMesh art,string name,Transform parent,Vector3 a,Vector3 control,Vector3 end,float startRadius,float endRadius,Color color,int rings=20)
    {
        const int sides=16;
        var vertices=new List<Vector3>();var triangles=new List<int>();
        Vector3 previousSide=Vector3.right;
        for(int r=0;r<=rings;r++)
        {
            float t=(float)r/rings;
            var center=(1-t)*(1-t)*a+2*(1-t)*t*control+t*t*end;
            var tangent=(2*(1-t)*(control-a)+2*t*(end-control)).normalized;
            var side=Vector3.ProjectOnPlane(previousSide,tangent).normalized;
            if(side.sqrMagnitude<.01f)side=Vector3.Cross(tangent,Vector3.forward).normalized;
            previousSide=side;
            var normal=Vector3.Cross(side,tangent).normalized;
            float radius=Mathf.Lerp(startRadius,endRadius,t);
            for(int j=0;j<sides;j++)
            {
                float angle=j*Mathf.PI*2/sides;
                vertices.Add(center+radius*(Mathf.Cos(angle)*side+Mathf.Sin(angle)*normal));
            }
        }
        for(int r=0;r<rings;r++)for(int j=0;j<sides;j++)
        {int a0=r*sides+j,b0=r*sides+(j+1)%sides,c0=a0+sides,d0=b0+sides;triangles.AddRange(new[]{a0,c0,b0,b0,c0,d0});}
        vertices.Add(a);vertices.Add(end);
        for(int j=0;j<sides;j++)triangles.AddRange(new[]{vertices.Count-2,j,(j+1)%sides,vertices.Count-1,rings*sides+(j+1)%sides,rings*sides+j});
        var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();art.Assets.Add(mesh);
        return art.Part(name,parent,Vector3.zero,Vector3.one,mesh,color);
    }
}
