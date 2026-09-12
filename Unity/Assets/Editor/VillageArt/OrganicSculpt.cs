using System;
using System.Collections.Generic;
using UnityEngine;

// Editor-only implicit sculpting. Overlapping volumes become a single welded surface.
public sealed class OrganicSculpt
{
    private readonly List<Func<Vector3,float>> volumes=new List<Func<Vector3,float>>();
    private readonly float blend;
    public Func<Vector3,float> Exclusion;
    public OrganicSculpt(float blend=.025f){this.blend=blend;}
    public void Ellipsoid(Vector3 center,Vector3 radius)
    {
        volumes.Add(p=>{var q=p-center;var a=new Vector3(q.x/radius.x,q.y/radius.y,q.z/radius.z);var b=new Vector3(q.x/(radius.x*radius.x),q.y/(radius.y*radius.y),q.z/(radius.z*radius.z));float k=a.magnitude;return k<.00001f?-Mathf.Min(radius.x,radius.y,radius.z):k*(k-1)/b.magnitude;});
    }
    public void Capsule(Vector3 a,Vector3 b,float ra,float rb)
    {
        var delta=b-a;float length=delta.sqrMagnitude;
        volumes.Add(p=>{float t=length<.000001f?0:Mathf.Clamp01(Vector3.Dot(p-a,delta)/length);return (p-a-delta*t).magnitude-Mathf.Lerp(ra,rb,t);});
    }
    public float Field(Vector3 p)
    {
        float value=100;
        foreach(var volume in volumes)
        {
            float next=volume(p);float h=Mathf.Max(blend-Mathf.Abs(value-next),0)/blend;
            value=Mathf.Min(value,next)-h*h*blend*.25f;
        }
        if(Exclusion==null)return value;
        float cavity=-Exclusion(p),radius=blend*.5f;
        float join=Mathf.Max(radius-Mathf.Abs(value-cavity),0)/radius;
        return Mathf.Max(value,cavity)+join*join*radius*.25f;
    }
    public GameObject Bake(ArtMesh art,string name,Transform parent,Bounds bounds,float spacing,Color color,Func<Vector3,Color> paint=null)
    {
        int nx=Mathf.CeilToInt(bounds.size.x/spacing)+1,ny=Mathf.CeilToInt(bounds.size.y/spacing)+1,nz=Mathf.CeilToInt(bounds.size.z/spacing)+1;
        var samples=new float[nx*ny*nz];var origin=bounds.min;
        Func<int,Vector3> position=id=>origin+new Vector3(id%nx,(id/nx)%ny,id/(nx*ny))*spacing;
        for(int i=0;i<samples.Length;i++)samples[i]=Field(position(i));
        var vertices=new List<Vector3>();var triangles=new List<int>();var edges=new Dictionary<ulong,int>();
        Func<int,int,int> cut=(a,b)=>{
            ulong key=((ulong)(uint)Mathf.Min(a,b)<<32)|(uint)Mathf.Max(a,b);
            if(edges.TryGetValue(key,out int found))return found;
            float t=samples[a]/(samples[a]-samples[b]);int index=vertices.Count;
            vertices.Add(Vector3.LerpUnclamped(position(a),position(b),t));edges.Add(key,index);return index;
        };
        Vector3 outward=Vector3.zero;
        Action<int,int,int> triangle=(a,b,c)=>{
            var n=Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]);
            if(n.sqrMagnitude==0)return;
            // The linear field in a tetrahedron points from its negative vertices to positive vertices.
            // This topological sign stays reliable even at tiny facets and sharp cavity joins.
            if(Vector3.Dot(n,outward)<0){int temp=b;b=c;c=temp;}
            triangles.Add(a);triangles.Add(b);triangles.Add(c);
        };
        int[,] tetra={{0,5,1,6},{0,1,2,6},{0,2,3,6},{0,3,7,6},{0,7,4,6},{0,4,5,6}};
        var cube=new int[8];var inside=new int[4];var outside=new int[4];
        for(int z=0;z<nz-1;z++)for(int y=0;y<ny-1;y++)for(int x=0;x<nx-1;x++)
        {
            int a=x+nx*(y+ny*z);cube[0]=a;cube[1]=a+1;cube[2]=a+nx+1;cube[3]=a+nx;cube[4]=a+nx*ny;cube[5]=cube[4]+1;cube[6]=cube[4]+nx+1;cube[7]=cube[4]+nx;
            for(int t=0;t<6;t++)
            {
                int ni=0,no=0;for(int j=0;j<4;j++){int id=cube[tetra[t,j]];if(samples[id]<0)inside[ni++]=id;else outside[no++]=id;}
                if(ni==0||ni==4)continue;
                Vector3 inner=Vector3.zero,outer=Vector3.zero;
                for(int j=0;j<ni;j++)inner+=position(inside[j]);
                for(int j=0;j<no;j++)outer+=position(outside[j]);
                outward=outer/no-inner/ni;
                if(ni==1)triangle(cut(inside[0],outside[0]),cut(inside[0],outside[1]),cut(inside[0],outside[2]));
                else if(ni==3)triangle(cut(outside[0],inside[0]),cut(outside[0],inside[1]),cut(outside[0],inside[2]));
                else{int p=cut(inside[0],outside[0]),q=cut(inside[0],outside[1]),r=cut(inside[1],outside[0]),s=cut(inside[1],outside[1]);triangle(p,q,r);triangle(q,s,r);}
            }
        }
        // Different tetrahedron edges can meet the same zero-valued grid vertex.
        // Weld those numerical duplicates before calculating normals or testing topology.
        var welded=new List<Vector3>();var map=new int[vertices.Count];var positions=new Dictionary<Vector3Int,int>();
        for(int i=0;i<vertices.Count;i++)
        {
            var p=vertices[i];var key=new Vector3Int(Mathf.RoundToInt(p.x*1000000),Mathf.RoundToInt(p.y*1000000),Mathf.RoundToInt(p.z*1000000));
            if(!positions.TryGetValue(key,out int id)){id=welded.Count;positions.Add(key,id);welded.Add(p);}
            map[i]=id;
        }
        var weldedTriangles=new List<int>();
        for(int i=0;i<triangles.Count;i+=3)
        {
            int a=map[triangles[i]],b=map[triangles[i+1]],c=map[triangles[i+2]];
            if(a==b || b==c || a==c)continue;
            weldedTriangles.Add(a);weldedTriangles.Add(b);weldedTriangles.Add(c);
        }
        vertices=welded;triangles=weldedTriangles;
        var mesh=new Mesh{name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);
        // Shade the welded surface from the sculpt field, so the sampling grid does not show on cheeks or fingers.
        var normals=new List<Vector3>();
        foreach(var p in vertices)
        {
            const float h=.0007f;
            var g=new Vector3(Field(p+Vector3.right*h)-Field(p-Vector3.right*h),Field(p+Vector3.up*h)-Field(p-Vector3.up*h),Field(p+Vector3.forward*h)-Field(p-Vector3.forward*h));
            normals.Add(g.sqrMagnitude>1e-20f?g/Mathf.Sqrt(g.sqrMagnitude):Vector3.up);
        }
        mesh.SetNormals(normals);mesh.RecalculateBounds();
        if(paint!=null){var colors=new List<Color>();foreach(var v in vertices)colors.Add(paint(v));mesh.SetColors(colors);}
        art.Assets.Add(mesh);return art.Part(name,parent,Vector3.zero,Vector3.one,mesh,color);
    }
}
