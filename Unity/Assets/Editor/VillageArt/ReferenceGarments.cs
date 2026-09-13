using System;
using System.Collections.Generic;
using UnityEngine;

// Closed fabric patches, with thickness and a continuous outer contour.
public static class ReferenceGarments
{
    public static GameObject Patch(ArtMesh art,string name,Transform parent,Func<float,float,Vector3> surface,Vector3 inset,Color color,int rows=32,int columns=64,Func<float,float,Vector3> innerOffset=null)
    {
        var vertices=new List<Vector3>();var triangles=new List<int>();
        int count=(rows+1)*(columns+1);
        for(int layer=0;layer<2;layer++)
            for(int r=0;r<=rows;r++)for(int c=0;c<=columns;c++)
            {
                float t=(float)r/rows,u=(float)c/columns;
                vertices.Add(surface(t,u)+(innerOffset==null?inset:innerOffset(t,u))*layer);
            }
        for(int layer=0;layer<2;layer++)for(int r=0;r<rows;r++)for(int c=0;c<columns;c++)
        {
            int a=layer*count+r*(columns+1)+c,b=a+columns+1;
            Triangle(triangles,a,b,a+1,layer==1);Triangle(triangles,a+1,b,b+1,layer==1);
        }
        var boundary=new List<int>();
        for(int c=0;c<columns;c++)boundary.Add(c);
        for(int r=0;r<rows;r++)boundary.Add(r*(columns+1)+columns);
        for(int c=columns;c>0;c--)boundary.Add(rows*(columns+1)+c);
        for(int r=rows;r>0;r--)boundary.Add(r*(columns+1));
        for(int i=0;i<boundary.Count;i++)
        {
            int a=boundary[i],b=boundary[(i+1)%boundary.Count];
            Triangle(triangles,a,b,a+count,false);Triangle(triangles,b,b+count,a+count,false);
        }
        double volume=0;
        for(int i=0;i<triangles.Count;i+=3)volume+=Vector3.Dot(vertices[triangles[i]],Vector3.Cross(vertices[triangles[i+1]],vertices[triangles[i+2]]));
        if(volume<0)for(int i=0;i<triangles.Count;i+=3)(triangles[i+1],triangles[i+2])=(triangles[i+2],triangles[i+1]);
        var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();art.Assets.Add(mesh);
        return art.Part(name,parent,Vector3.zero,Vector3.one,mesh,color);
    }
    private static void Triangle(List<int> list,int a,int b,int c,bool reverse)
    {list.Add(a);list.Add(reverse?c:b);list.Add(reverse?b:c);}

    public static void Coat(ArtMesh art,Transform body,Color color)
    {
        Patch(art,"Open long coat",body,(t,u)=>{
            float y=Mathf.Lerp(.39f,1.24f,t);
            float gap=Mathf.Lerp(.82f,.40f,t);
            float angle=Mathf.Lerp(gap,Mathf.PI*2-gap,u);
            float waist=1-Mathf.Pow(2*t-1,2);
            float radius=Mathf.Lerp(.47f,.33f,t)-.04f*waist;
            float fold=.014f*Mathf.Sin(angle*9+t*2)*Mathf.Pow(1-t,2);
            return new Vector3(Mathf.Sin(angle)*(radius+fold),y+.035f*Mathf.Cos(angle*2)*(1-t),Mathf.Cos(angle)*(radius*.86f+fold)-.005f);
        },Vector3.zero,color,32,64,(t,u)=>{
            float gap=Mathf.Lerp(.82f,.40f,t),angle=Mathf.Lerp(gap,Mathf.PI*2-gap,u);
            return new Vector3(-Mathf.Sin(angle)*.014f,0,-Mathf.Cos(angle)*.014f);
        });
        foreach(int sign in new[]{-1,1})
        {
            // Lapels turn away from the shirt, broad at the collar and narrow at the waist.
            Patch(art,"Turned coat lapel",body,(t,u)=>{
                float y=Mathf.Lerp(.79f,1.22f,t),center=Mathf.Lerp(.14f,.21f,t);
                float width=.024f+.075f*Mathf.Sin(Mathf.PI*t*.7f);
                return new Vector3(sign*(center+(u-.5f)*width),y,.235f+.035f*Mathf.Sin(Mathf.PI*u));
            },Vector3.back*.014f,color*1.35f,16,8);
            for(int i=0;i<4;i++)art.Ball("Coat button",body,new Vector3(sign*(.225f+i*.045f),.92f-i*.145f,.19f-i*.016f),new Vector3(.035f,.027f,.018f),new Color(.16f,.17f,.18f));
        }
    }
    public static void Hat(ArtMesh art,Transform head)
    {
        var hat=art.Group("Wide brim hat",head,new Vector3(0,-.07f,-.015f));
        hat.localRotation=Quaternion.Euler(-3,0,-4);
        Color felt=new Color(.095f,.080f,.11f),band=new Color(.40f,.12f,.085f);
        Patch(art,"Curved felt brim",hat,(t,u)=>{
            float a=u*Mathf.PI*2,r=Mathf.Lerp(.28f,.66f,t);
            float y=.435f+.075f*Mathf.Pow(Mathf.Abs(Mathf.Sin(a)),3)*t*t-.038f*Mathf.Cos(a)*t;
            return new Vector3(Mathf.Sin(a)*r,y,Mathf.Cos(a)*r*.85f);
        },Vector3.down*.025f,felt);
        var crown=CharacterSurface.Form(art,"Creased hat crown",hat,new[]{
            new Vector4(.435f,.34f,.29f,0),new Vector4(.52f,.345f,.29f,0),
            new Vector4(.69f,.31f,.26f,-.015f),new Vector4(.77f,.29f,.24f,-.025f),
            new Vector4(.79f,.24f,.20f,-.025f),new Vector4(.79f,.025f,.025f,-.025f)},felt);
        var mesh=crown.GetComponent<MeshFilter>().sharedMesh;var points=mesh.vertices;
        for(int i=0;i<points.Length;i++)
        {
            float top=Mathf.Clamp01((points[i].y-.60f)/.19f);
            points[i].x-=top*.035f;
            points[i].y-=top*.028f*Mathf.Exp(-points[i].x*points[i].x/.015f);
        }
        mesh.vertices=points;mesh.RecalculateNormals();mesh.RecalculateBounds();
        CharacterSurface.Form(art,"Rust hat band",hat,new[]{new Vector4(.46f,.35f,.300f,0),new Vector4(.505f,.351f,.301f,0),new Vector4(.585f,.334f,.289f,-.008f)},band);
        art.SoftBox("Hat band keeper",hat,new Vector3(-.29f,.525f,.17f),new Vector3(.07f,.125f,.03f),felt);
    }
}
