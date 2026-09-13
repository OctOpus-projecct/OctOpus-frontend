using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class CharacterGripTests
{
    [TestCase("Body/Open long coat")]
    [TestCase("Body/Head/Wide brim hat/Curved felt brim")]
    [TestCase("Body/Head/Sculpted face")]
    [TestCase("Body/Head/Continuous sculpted hair")]
    [TestCase("Body/ArmR/Hand/Sculpted gripping hand")]
    public void SculptedPartsAreOneConnectedSurface(string path)
    {
        var part=Resources.Load<GameObject>("Village/Player").transform.Find(path);
        Assert.That(part,Is.Not.Null,path);
        AssertClosedSurface(part.GetComponent<MeshFilter>().sharedMesh);
    }
    [TestCase("Player")]
    [TestCase("Villager")]
    public void HairLocksHaveClosedOutwardSurfaces(string model)
    {
        var hair=Resources.Load<GameObject>("Village/"+model).transform.Find("Body/Head/Layered hair locks");
        Assert.That(hair,Is.Not.Null);
        var meshes=hair.GetComponentsInChildren<MeshFilter>();
        Assert.That(meshes.Length,Is.GreaterThan(0));
        foreach(var part in meshes)AssertClosedSurface(part.sharedMesh);
    }
    private static void AssertClosedSurface(Mesh mesh)
    {
        var points=mesh.vertices;var faces=mesh.triangles;
        var balance=new Dictionary<ulong,int>();var uses=new Dictionary<ulong,int>();double volume=0;
        for(int i=0;i<faces.Length;i+=3)
        {
            int a=faces[i],b=faces[i+1],c=faces[i+2];
            volume+=Vector3.Dot(points[a],Vector3.Cross(points[b],points[c]))/6.0;
            int[] corners={a,b,c};
            for(int e=0;e<3;e++)
            {
                int from=corners[e],to=corners[(e+1)%3];ulong key=((ulong)(uint)Mathf.Min(from,to)<<32)|(uint)Mathf.Max(from,to);
                uses.TryGetValue(key,out int count);balance.TryGetValue(key,out int direction);
                uses[key]=count+1;balance[key]=direction+(from<to?1:-1);
            }
        }
        int badEdges=0;foreach(var edge in uses)if(edge.Value!=2 || balance[edge.Key]!=0)badEdges++;
        Assert.That(badEdges,Is.EqualTo(0),"The sculpt must be closed and neighboring faces must have consistent winding");
        Assert.That(volume,Is.GreaterThan(0),"The closed surface must face outward");
        var parent=new int[mesh.vertexCount];for(int i=0;i<parent.Length;i++)parent[i]=i;
        System.Func<int,int> root=null;root=i=>parent[i]==i?i:parent[i]=root(parent[i]);
        var triangles=mesh.triangles;var used=new HashSet<int>();
        for(int i=0;i<triangles.Length;i+=3)
        {
            int a=triangles[i],b=triangles[i+1],c=triangles[i+2];parent[root(b)]=root(a);parent[root(c)]=root(a);used.Add(a);used.Add(b);used.Add(c);
        }
        var components=new HashSet<int>();foreach(int vertex in used)components.Add(root(vertex));
        Assert.That(components.Count,Is.EqualTo(1),"Separate floating sculpt fragments");
    }
    [Test]
    public void CoatLiningStaysInsideTheOuterShell()
    {
        var mesh=Resources.Load<GameObject>("Village/Player").transform.Find("Body/Open long coat").GetComponent<MeshFilter>().sharedMesh;
        var points=mesh.vertices;int layerSize=points.Length/2;
        for(int i=0;i<layerSize;i++)
        {
            var outward=new Vector3(points[i].x,0,(points[i].z+.005f)/(.86f*.86f)).normalized;
            Assert.That(Vector3.Dot(points[i+layerSize]-points[i],outward),Is.LessThan(-.005f),"Lining must move inward around the entire coat, including sides and back");
        }
    }
    [Test]
    public void ShaftHasClearanceInsideGripAndRemainsAnchoredDuringSwing()
    {
        var model=Object.Instantiate(Resources.Load<GameObject>("Village/Player"));
        try
        {
            var arm=model.transform.Find("Body/ArmR");var hand=arm.Find("Hand");var axe=hand.Find("HeldAxe");var grip=hand.Find("GripAxis");
            var skin=hand.Find("Sculpted gripping hand").GetComponent<MeshFilter>();
            float minimum=float.PositiveInfinity;int contactVertices=0;
            foreach(var vertex in skin.sharedMesh.vertices)
            {
                var point=grip.InverseTransformPoint(skin.transform.TransformPoint(vertex));
                if(Mathf.Abs(point.y)>.065f)continue;
                float radius=new Vector2(point.x,point.z).magnitude;
                minimum=Mathf.Min(minimum,radius);if(radius<.040f)contactVertices++;
            }
            // Compare actual transformed wood and tilted wrap geometry with the sculpted skin channel.
                        float toolRadius=0;
            foreach(var part in axe.GetComponentsInChildren<MeshFilter>())foreach(var vertex in part.sharedMesh.vertices)
            {
                var point=grip.InverseTransformPoint(part.transform.TransformPoint(vertex));
                if(Mathf.Abs(point.y)<=.070f)toolRadius=Mathf.Max(toolRadius,new Vector2(point.x,point.z).magnitude);
            }
            Assert.That(minimum,Is.GreaterThan(toolRadius+.0005f),"Skin must clear the tilted wrap as well as the wood shaft");
            Assert.That(contactVertices,Is.GreaterThan(20));
            foreach(float angle in new[]{0f,-55f,-110f})
            {
                arm.localRotation=Quaternion.Euler(angle,0,11);
                Assert.That(Vector3.Distance(axe.TransformPoint(Vector3.up*.10f),grip.position),Is.LessThan(.0001f));
                Assert.That(Vector3.Dot(axe.up,grip.up),Is.GreaterThan(.9999f));
            }
            Assert.That((axe.up.x),Is.GreaterThan(0),"Blade should point outward from the right hand");
        }
        finally{Object.DestroyImmediate(model);}
    }
}
