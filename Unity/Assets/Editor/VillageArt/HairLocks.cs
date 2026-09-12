using System.Collections.Generic;
using UnityEngine;

// Broad, flattened locks follow authored curves and narrow into swept tips.
public static class HairLocks
{
    public static void Build(ArtMesh art,Transform head)
    {
        var root=art.Group("Layered hair locks",head,Vector3.zero);
        // Back and side layers sit below the crown layer.
        for(int i=0;i<11;i++)
        {
            float phi=.95f+i*(Mathf.PI*2-1.9f)/10;
            var radial=new Vector3(Mathf.Sin(phi),0,Mathf.Cos(phi));
            var tangent=new Vector3(Mathf.Cos(phi),0,-Mathf.Sin(phi));
            Lock(art,root,"Lower side lock",radial*.22f+Vector3.up*.34f,
                radial*.44f+Vector3.up*.23f+tangent*.035f,
                radial*.37f+Vector3.up*-.055f+tangent*.075f,.105f,.038f,i);
            Lock(art,root,"Crown lock",new Vector3(.045f,.465f,-.075f),
                radial*.32f+Vector3.up*.50f,
                radial*.415f+Vector3.up*.13f+tangent*.045f,.112f,.047f,i+2);
        }
        Lock(art,root,"Left swept fringe",new Vector3(.06f,.45f,.015f),new Vector3(-.27f,.48f,.33f),new Vector3(-.35f,.07f,.22f),.112f,.045f,1);
        Lock(art,root,"Left fringe",new Vector3(.13f,.46f,.02f),new Vector3(-.07f,.45f,.44f),new Vector3(-.235f,.125f,.33f),.108f,.048f,3);
        Lock(art,root,"Center swept fringe",new Vector3(.17f,.46f,.015f),new Vector3(.16f,.43f,.46f),new Vector3(-.055f,.18f,.367f),.105f,.045f,2);
        Lock(art,root,"Right fringe",new Vector3(.18f,.445f,.015f),new Vector3(.35f,.37f,.35f),new Vector3(.24f,.15f,.31f),.095f,.042f,4);
        Lock(art,root,"Right temple",new Vector3(.22f,.37f,-.01f),new Vector3(.44f,.27f,.21f),new Vector3(.35f,.015f,.19f),.083f,.035f,0);
        Lock(art,root,"Crown flick",new Vector3(.025f,.44f,-.06f),new Vector3(.035f,.565f,-.11f),new Vector3(.15f,.52f,-.10f),.060f,.032f,3);
    }
    private static void Lock(ArtMesh art,Transform parent,string name,Vector3 a,Vector3 b,Vector3 c,float width,float depth,int shade)
    {
        const int rows=24,columns=16;
        var v=new List<Vector3>();var triangles=new List<int>();
        v.Add(a);
        for(int r=1;r<rows;r++)
        {
            float t=(float)r/rows;
            var center=(1-t)*(1-t)*a+2*t*(1-t)*b+t*t*c;
            var along=(2*(1-t)*(b-a)+2*t*(c-b)).normalized;
            var outward=(center-new Vector3(0,.08f,-.035f)).normalized;
            var across=Vector3.Cross(along,outward).normalized;
            outward=Vector3.Cross(across,along).normalized;
            float profile=Mathf.Pow(Mathf.Sin(Mathf.PI*t),.7f)*(1.12f-.42f*t);
            for(int col=0;col<columns;col++)
            {
                float angle=col*Mathf.PI*2/columns;
                v.Add(center+across*(Mathf.Cos(angle)*width*profile)+outward*(Mathf.Sin(angle)*depth*profile));
            }
        }
        int tip=v.Count;v.Add(c);
        for(int col=0;col<columns;col++)
        {
            int next=(col+1)%columns;
            triangles.AddRange(new[]{0,1+col,1+next});
            for(int row=0;row<rows-2;row++)
            {
                int x=1+row*columns+col,y=1+row*columns+next;
                triangles.AddRange(new[]{x,x+columns,y,y,x+columns,y+columns});
            }
            int last=1+(rows-2)*columns;
            triangles.AddRange(new[]{tip,last+next,last+col});
        }
        // Keep all faces outward for the chosen curve frame.
        double volume=0;for(int i=0;i<triangles.Count;i+=3)volume+=Vector3.Dot(v[triangles[i]],Vector3.Cross(v[triangles[i+1]],v[triangles[i+2]]));
        if(volume<0)for(int i=0;i<triangles.Count;i+=3)(triangles[i+1],triangles[i+2])=(triangles[i+2],triangles[i+1]);
        var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();art.Assets.Add(mesh);
        float tone=1+(shade%5-2)*.026f;
        art.Part(name,parent,Vector3.zero,Vector3.one,mesh,new Color(.32f*tone,.18f*tone,.10f*tone));
    }
}
