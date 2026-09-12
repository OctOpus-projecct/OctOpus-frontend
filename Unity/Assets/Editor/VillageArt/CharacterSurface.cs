using System.Collections.Generic;
using UnityEngine;

// Ring profiles describe the silhouette directly instead of assembling boxes.
public static class CharacterSurface
{
    public static GameObject Form(ArtMesh art,string name,Transform parent,Vector4[] profile,Color color,float frontArc=0)
    {
        const int rows=48,columns=48;
        var vertices=new List<Vector3>();var triangles=new List<int>();
        for(int r=0;r<=rows;r++)
        {
            float f=(float)r/rows*(profile.Length-1);int i=Mathf.Min((int)f,profile.Length-2);float t=f-i;
            var p0=profile[Mathf.Max(0,i-1)];var p1=profile[i];var p2=profile[i+1];var p3=profile[Mathf.Min(profile.Length-1,i+2)];
            var p=.5f*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t);
            for(int j=0;j<=columns;j++)
            {
                float angle=frontArc>0?Mathf.Lerp(-frontArc,frontArc,(float)j/columns):j*Mathf.PI*2/columns;
                vertices.Add(new Vector3(Mathf.Sin(angle)*Mathf.Max(.001f,p.y),p.x,Mathf.Cos(angle)*Mathf.Max(.001f,p.z)+p.w));
            }
        }
        for(int r=0;r<rows;r++)for(int j=0;j<columns;j++)
        {int a=r*(columns+1)+j,b=a+columns+1;triangles.AddRange(new[]{a,a+1,b,b,a+1,b+1});}
        if(frontArc==0)
        {
            vertices.Add(new Vector3(0,profile[0].x,profile[0].w));vertices.Add(new Vector3(0,profile[profile.Length-1].x,profile[profile.Length-1].w));
            for(int j=0;j<columns;j++)triangles.AddRange(new[]{vertices.Count-2,j+1,j,vertices.Count-1,rows*(columns+1)+j,rows*(columns+1)+j+1});
        }
        var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();
        if(frontArc==0)
        {
            var normals=mesh.normals;
            for(int r=0;r<=rows;r++){int first=r*(columns+1),last=first+columns;var n=(normals[first]+normals[last]).normalized;normals[first]=normals[last]=n;}
            mesh.normals=normals;
        }
        mesh.RecalculateBounds();art.Assets.Add(mesh);
        return art.Part(name,parent,Vector3.zero,Vector3.one,mesh,color);
    }
}
