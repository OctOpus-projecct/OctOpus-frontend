using System.Collections.Generic;
using UnityEngine;

// A shaped scalp shell with a swept fringe, rather than a union of rounded hair volumes.
public static class HairSurface
{
    public static void Build(ArtMesh art,Transform parent)
    {
        const int columns=128,rows=56;
        var vertices=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
        for(int layer=0;layer<2;layer++)
        {
            float inset=layer==0?0:.022f;int start=vertices.Count;
            vertices.Add(new Vector3( .025f,.47f-inset,-.035f));colors.Add(new Color(.34f,.20f,.12f));
            for(int row=1;row<=rows;row++)for(int col=0;col<columns;col++)
            {
                float phi=col*Mathf.PI*2/columns;float signed=Mathf.DeltaAngle(0,phi*Mathf.Rad2Deg)*Mathf.Deg2Rad;
                float front=Mathf.Pow(Mathf.Max(0,Mathf.Cos(phi)),1.5f);
                float edge=2.12f-front*1.20f;
                // Unequal, diagonal points in the fringe leave a visible side part.
                edge+=front*(Peak(signed,-.48f,.19f)*.26f+Peak(signed,.12f,.19f)*.20f+Peak(signed,.58f,.16f)*.13f);
                float theta=edge*row/rows;
                float sweep=phi+.27f*Mathf.Sin(theta);
                float flow=7*(phi+.45f*Mathf.Cos(theta))+.18f*Mathf.Sin(phi*3);
                float ridge=Mathf.Pow(Mathf.Max(0,Mathf.Cos(flow)),4);
                float ripple=(.014f*ridge-.006f)*Mathf.Sin(theta)*Mathf.Sin(theta);
                float x=(.377f-inset+ripple)*Mathf.Sin(theta)*Mathf.Sin(sweep);
                float y=.11f+(.36f-inset+ripple)*Mathf.Cos(theta);
                float z=-.035f+(.350f-inset+ripple)*Mathf.Sin(theta)*Mathf.Cos(sweep);
                vertices.Add(new Vector3(x,y,z));
                float shade=.96f+.08f*ridge+.035f*Mathf.Cos(phi-1);
                colors.Add(new Color(.34f*shade,.20f*shade,.115f*shade));
            }
            for(int col=0;col<columns;col++)
            {
                int next=(col+1)%columns;Add(triangles,start,start+1+col,start+1+next,layer==1);
            }
            for(int row=0;row<rows-1;row++)for(int col=0;col<columns;col++)
            {
                int next=(col+1)%columns,a=start+1+row*columns+col,b=start+1+row*columns+next,c=a+columns,d=b+columns;
                Add(triangles,a,c,b,layer==1);Add(triangles,b,c,d,layer==1);
            }
        }
        int layerSize=1+rows*columns;
        for(int col=0;col<columns;col++)
        {
            int next=(col+1)%columns,a=1+(rows-1)*columns+col,b=1+(rows-1)*columns+next,c=a+layerSize,d=b+layerSize;
            Add(triangles,a,c,b,false);Add(triangles,b,c,d,false);
        }
        var mesh=new Mesh{name="Swept hair shell"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetColors(colors);mesh.RecalculateNormals();mesh.RecalculateBounds();art.Assets.Add(mesh);
        var part=art.Part("Continuous sculpted hair",parent,Vector3.zero,Vector3.one,mesh,Color.white);
        var material=new Material(Shader.Find("OctOpus/Painted Skin")){name="Chestnut hair",color=Color.white};art.Assets.Add(material);part.GetComponent<Renderer>().sharedMaterial=material;
    }
    private static float Peak(float angle,float center,float width)=>Mathf.Exp(-Mathf.Pow((angle-center)/width,2));
    private static void Add(List<int> triangles,int a,int b,int c,bool reverse){triangles.Add(a);triangles.Add(reverse?c:b);triangles.Add(reverse?b:c);}
}
