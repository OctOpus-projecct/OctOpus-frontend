using System.Collections.Generic;
using UnityEngine;

// Editor-only authored mesh construction. All resulting geometry is baked as project assets.
public sealed class ArtMesh
{
    public readonly GameObject Root;
    public readonly List<Object> Assets = new List<Object>();
    private readonly Dictionary<string,Mesh> meshes=new Dictionary<string,Mesh>();
    private readonly Dictionary<Color,Material> materials=new Dictionary<Color,Material>();
    public ArtMesh(string name){Root=new GameObject(name);}
    public Transform Group(string name,Transform parent,Vector3 position)
    {var o=new GameObject(name).transform;o.SetParent(parent,false);o.localPosition=position;return o;}
    public Material Material(Color color)
    {
        if(materials.TryGetValue(color,out var m))return m;
        m=new Material(Shader.Find("Standard")){name="Palette_"+materials.Count,color=color};m.SetFloat("_Glossiness",.12f);
        materials.Add(color,m);Assets.Add(m);return m;
    }
    public GameObject Part(string name,Transform parent,Vector3 position,Vector3 scale,Mesh mesh,Color color)
    {
        var o=new GameObject(name);o.transform.SetParent(parent,false);o.transform.localPosition=position;o.transform.localScale=scale;
        o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterial=Material(color);return o;
    }
    public GameObject Ball(string name,Transform parent,Vector3 position,Vector3 scale,Color color,bool flat=false)
        =>Part(name,parent,position,scale,flat?FacetedStone():Shape(28,20,1,false),color);
    public GameObject Box(string name,Transform parent,Vector3 position,Vector3 scale,Color color)
        =>Part(name,parent,position,scale,RoundedBox(),color);
    public GameObject SoftBox(string name,Transform parent,Vector3 position,Vector3 scale,Color color)
        =>Part(name,parent,position,scale,Shape(28,20,.55f,false),color);
    private Mesh FacetedStone()
    {
        const string key="organic_icosphere";if(meshes.TryGetValue(key,out var existing))return existing;
        float h=(1+Mathf.Sqrt(5))/2;
        var v=new List<Vector3>{new Vector3(-1,h,0),new Vector3(1,h,0),new Vector3(-1,-h,0),new Vector3(1,-h,0),new Vector3(0,-1,h),new Vector3(0,1,h),new Vector3(0,-1,-h),new Vector3(0,1,-h),new Vector3(h,0,-1),new Vector3(h,0,1),new Vector3(-h,0,-1),new Vector3(-h,0,1)};
        int[] faces={0,11,5,0,5,1,0,1,7,0,7,10,0,10,11,1,5,9,5,11,4,11,10,2,10,7,6,7,1,8,3,9,4,3,4,2,3,2,6,3,6,8,3,8,9,4,9,5,2,4,11,6,2,10,8,6,7,9,8,1};
        for(int i=0;i<v.Count;i++)v[i]=v[i].normalized*.5f;
        var t=new List<int>();
        for(int i=0;i<faces.Length;i+=3)
        {
            int a=faces[i],b=faces[i+1],c=faces[i+2];
            int ab=v.Count;v.Add((v[a]+v[b]).normalized*.5f);
            int bc=v.Count;v.Add((v[b]+v[c]).normalized*.5f);
            int ca=v.Count;v.Add((v[c]+v[a]).normalized*.5f);
            t.AddRange(new[]{a,ab,ca,b,bc,ab,c,ca,bc,ab,bc,ca});
        }
        return Finish(key,v,t,true);
    }
    private Mesh RoundedBox()
    {
        const string key="beveled_box";
        if(meshes.TryGetValue(key,out var existing))return existing;
        var vertices=new List<Vector3>();var triangles=new List<int>();var normals=new List<Vector3>();
        const int steps=10;const float radius=.12f;
        foreach(var axis in new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back})
        {
            var u=Vector3.Cross(axis,Mathf.Abs(axis.y)>.5f?Vector3.forward:Vector3.up);
            var v=Vector3.Cross(axis,u);int offset=vertices.Count;
            for(int y=0;y<=steps;y++)for(int x=0;x<=steps;x++)
            {
                var point=axis*.5f+u*((float)x/steps-.5f)+v*((float)y/steps-.5f);
                var core=new Vector3(Mathf.Clamp(point.x,-.5f+radius,.5f-radius),Mathf.Clamp(point.y,-.5f+radius,.5f-radius),Mathf.Clamp(point.z,-.5f+radius,.5f-radius));
                var normal=(point-core).normalized;vertices.Add(core+normal*radius);normals.Add(normal);
            }
            for(int y=0;y<steps;y++)for(int x=0;x<steps;x++)
            {int a=offset+y*(steps+1)+x,b=a+steps+1;triangles.AddRange(new[]{a,a+1,b,b,a+1,b+1});}
        }
        var mesh=Finish(key,vertices,triangles,false);mesh.SetNormals(normals);return mesh;
    }
    private static float Power(float x,float p)=>Mathf.Sign(x)*Mathf.Pow(Mathf.Abs(x),p);
    private Mesh Shape(int sides,int rings,float power,bool flat)
    {
        string key=$"round_{sides}_{rings}_{power}_{flat}";if(meshes.TryGetValue(key,out var existing))return existing;
        var v=new List<Vector3>();var t=new List<int>();
        for(int y=0;y<=rings;y++)for(int x=0;x<=sides;x++)
        {float a=y*Mathf.PI/rings,b=x*2*Mathf.PI/sides;v.Add(new Vector3(Power(Mathf.Sin(a)*Mathf.Cos(b),power),Power(Mathf.Cos(a),power),Power(Mathf.Sin(a)*Mathf.Sin(b),power))*.5f);}
        for(int y=0;y<rings;y++)for(int x=0;x<sides;x++)
        {int a=y*(sides+1)+x,b=a+sides+1;t.AddRange(new[]{a,a+1,b,b,a+1,b+1});}
        return Finish(key,v,t,flat);
    }
    public GameObject Cylinder(string name,Transform parent,Vector3 position,Vector3 scale,Color color,float top=.9f,int sides=12)
    {
        string key=$"cylinder_{top}_{sides}";
        if(!meshes.TryGetValue(key,out var mesh))
        {
            var v=new List<Vector3>();var t=new List<int>();
            for(int i=0;i<sides;i++)
            {float a=i*2*Mathf.PI/sides;v.Add(new Vector3(Mathf.Cos(a)*.5f,-.5f,Mathf.Sin(a)*.5f));v.Add(new Vector3(Mathf.Cos(a)*.5f*top,.5f,Mathf.Sin(a)*.5f*top));}
            v.Add(Vector3.down*.5f);v.Add(Vector3.up*.5f);
            for(int i=0;i<sides;i++){int a=i*2,b=((i+1)%sides)*2;t.AddRange(new[]{a,a+1,b,b,a+1,b+1,sides*2,a,b,sides*2+1,b+1,a+1});}
            mesh=Finish(key,v,t,true);
        }
        return Part(name,parent,position,scale,mesh,color);
    }
    public GameObject Beam(string name,Transform parent,Vector3 a,Vector3 b,float width,Color color)
    {var o=Box(name,parent,(a+b)*.5f,new Vector3(width,(b-a).magnitude,width),color);o.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);return o;}
    // Convex polygon in XY extruded in Z, useful for roof silhouettes and axe blades.
    public GameObject Extrude(string name,Transform parent,Vector3 position,Vector2[] polygon,float depth,Color color)
    {
        var v=new List<Vector3>();var t=new List<int>();int n=polygon.Length;
        foreach(var p in polygon)v.Add(new Vector3(p.x,p.y,-depth*.5f));
        foreach(var p in polygon)v.Add(new Vector3(p.x,p.y,depth*.5f));
        for(int i=1;i<n-1;i++)t.AddRange(new[]{0,i+1,i,n,n+i,n+i+1});
        for(int i=0;i<n;i++){int b=(i+1)%n;t.AddRange(new[]{i,b,n+i,b,n+b,n+i});}
        return Part(name,parent,position,Vector3.one,Finish(name+Assets.Count,v,t,true),color);
    }
    private Mesh Finish(string key,List<Vector3> v,List<int> t,bool flat)
    {
        if(flat){var split=new List<Vector3>();foreach(int i in t)split.Add(v[i]);v=split;t=new List<int>();for(int i=0;i<v.Count;i++)t.Add(i);}
        var m=new Mesh{name=key};m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();
        meshes.Add(key,m);Assets.Add(m);return m;
    }
}

public static class VillageColors
{
    public static readonly Color Skin=new Color(.93f,.66f,.45f),Hair=new Color(.25f,.13f,.075f),
        Cream=new Color(.92f,.85f,.66f),Sage=new Color(.39f,.49f,.24f),DarkGreen=new Color(.22f,.29f,.15f),
        Wood=new Color(.43f,.25f,.12f),Tan=new Color(.64f,.43f,.23f),Gold=new Color(.75f,.58f,.28f),
        Steel=new Color(.51f,.57f,.59f),Leaf=new Color(.43f,.61f,.27f),LightLeaf=new Color(.61f,.73f,.34f),
        DarkLeaf=new Color(.28f,.47f,.27f),Stone=new Color(.55f,.56f,.50f),Glow=new Color(.34f,.88f,.85f);
}
