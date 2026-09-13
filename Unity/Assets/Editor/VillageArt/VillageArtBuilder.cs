using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class VillageArtBuilder
{
    public static readonly string[] Names = { "Player", "Villager", "Axe", "Backpack", "Tree", "Stump", "Wood", "Cottage", "Workshop", "Lantern", "Signpost", "Details" };
    [MenuItem("OctOpus/Build village models")]
    public static void Build()
    {
        Func<ArtMesh>[] factories = { CharacterModels.Player, CharacterModels.Villager, CharacterModels.Axe, CharacterModels.Backpack,
            EnvironmentModels.Tree, EnvironmentModels.Stump, EnvironmentModels.Wood, EnvironmentModels.Cottage,
            EnvironmentModels.Workshop, EnvironmentModels.Lantern, EnvironmentModels.Signpost, EnvironmentModels.Details };
        foreach (var factory in factories)
        {
            var art = factory();
            string folder = "Assets/Resources/Village/" + art.Root.name + "Data";
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            var saved = new Dictionary<UnityEngine.Object,UnityEngine.Object>();
            for (int i=0;i<art.Assets.Count;i++)
            {
                var source = art.Assets[i];
                string path = folder + "/" + i.ToString("D3") + "_" + source.GetType().Name + ".asset";
                var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (existing == null) { AssetDatabase.CreateAsset(source,path); existing=source; }
                else { EditorUtility.CopySerialized(source,existing); EditorUtility.SetDirty(existing); }
                saved[source]=existing;
            }
            foreach(var mesh in art.Root.GetComponentsInChildren<MeshFilter>(true)) mesh.sharedMesh=(Mesh)saved[mesh.sharedMesh];
            foreach(var renderer in art.Root.GetComponentsInChildren<MeshRenderer>(true)) renderer.sharedMaterial=(Material)saved[renderer.sharedMaterial];
            if(art.Root.name=="Axe" || art.Root.name=="Backpack")
            {
                var bounds=BoundsOf(art.Root);
                foreach(Transform child in art.Root.transform)child.localPosition-=Vector3.up*bounds.min.y;
            }
            PrefabUtility.SaveAsPrefabAsset(art.Root,"Assets/Resources/Village/"+art.Root.name+".prefab");
            UnityEngine.Object.DestroyImmediate(art.Root);
            foreach(var asset in art.Assets)if(!AssetDatabase.Contains(asset))UnityEngine.Object.DestroyImmediate(asset);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[OctOpus] Village models baked: 12");
    }
    public static Bounds BoundsOf(GameObject root)
    {
        var renderers=root.GetComponentsInChildren<Renderer>();
        var bounds=renderers[0].bounds;
        foreach(var r in renderers)bounds.Encapsulate(r.bounds);
        return bounds;
    }
    [MenuItem("OctOpus/Render village model previews")]
    public static void RenderPreviews()
    {
        if(!Application.isBatchMode && !UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../TestResults/village-models"));
        Directory.CreateDirectory(output);
        var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight=new Color(.65f,.63f,.58f);
        var light=new GameObject("Preview sunlight").AddComponent<Light>();
        light.type=LightType.Directional;light.shadows=LightShadows.Soft;light.shadowStrength=.4f;light.intensity=.9f;light.transform.rotation=Quaternion.Euler(45,-35,0);
        var camera=new GameObject("Preview camera").AddComponent<Camera>();
        camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.91f,.89f,.82f);
        camera.nearClipPlane=.01f;camera.farClipPlane=100;
        var sheet=new Texture2D(1600,1200,TextureFormat.RGB24,false);
        var portraits=new Texture2D(1600,800,TextureFormat.RGB24,false);
        for(int i=0;i<Names.Length;i++)
        {
            light.transform.rotation=Quaternion.Euler(40,(Names[i]=="Cottage" || Names[i]=="Workshop" || Names[i]=="Backpack")?-35:145,0);
            var model=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Village/"+Names[i]));
            var bounds=BoundsOf(model);
            camera.aspect=1;
            camera.transform.position=bounds.center+new Vector3(4,2.5f,(Names[i]=="Cottage" || Names[i]=="Workshop" || Names[i]=="Backpack")?-7:7).normalized*15;
            camera.transform.LookAt(bounds.center);
            float fit=0;
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
            {
                var corner=Quaternion.Inverse(camera.transform.rotation)*Vector3.Scale(bounds.extents,new Vector3(x,y,z));
                fit=Mathf.Max(fit,Mathf.Abs(corner.x),Mathf.Abs(corner.y));
            }
            camera.orthographicSize=fit*1.12f;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.transform.position=new Vector3(0,bounds.min.y-.012f,0);floor.transform.localScale=Vector3.one*20;
            var floorMaterial=new Material(Shader.Find("Standard"));floorMaterial.color=new Color(.64f,.61f,.54f);floorMaterial.SetFloat("_Glossiness",0);
            floor.GetComponent<Renderer>().sharedMaterial=floorMaterial;floor.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            if(i<2)
            {
                var portrait=Render(camera,800,800);
                portraits.SetPixels(i*800,0,800,800,portrait.GetPixels());
                UnityEngine.Object.DestroyImmediate(portrait);
            }
            var image=Render(camera,400,400);
            UnityEngine.Object.DestroyImmediate(floor);UnityEngine.Object.DestroyImmediate(floorMaterial);
            File.WriteAllBytes(Path.Combine(output,Names[i]+".png"),image.EncodeToPNG());
            sheet.SetPixels((i%4)*400,(2-i/4)*400,400,400,image.GetPixels());
            UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(model);
        }
        portraits.Apply();File.WriteAllBytes(Path.Combine(output,"characters.png"),portraits.EncodeToPNG());UnityEngine.Object.DestroyImmediate(portraits);
        sheet.Apply();File.WriteAllBytes(Path.Combine(output,"models.png"),sheet.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(sheet);
        RenderFaces(output,camera,light);
        RenderGripPoses(output,camera,light);
        RenderAdventurer(output,camera,light);
        RenderWoodcutting(output,camera,light);
        var map=new GameObject("Village");
        var ground=GameObject.CreatePrimitive(PrimitiveType.Plane);ground.transform.localScale=Vector3.one*2.4f;
        map.AddComponent<PrototypeMapView>().EnsureBuilt(ground,camera);
        foreach(var pos in OctOpus.Shared.WorldLayout.TreePositions)
            UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Village/Tree"),pos,Quaternion.identity);
        UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Village/Player"),OctOpus.Shared.WorldLayout.SpawnCenter,Quaternion.Euler(0,150,0));
        map.GetComponent<PrototypeMapView>().FrameCamera(1600,1000);
        var world=Render(camera,1600,1000);
        File.WriteAllBytes(Path.Combine(output,"village.png"),world.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(world);
        Debug.Log("[OctOpus] Actual mesh previews: "+output);
    }
    private static void RenderFaces(string output,Camera camera,Light light)
    {
        var image=new Texture2D(1600,800,TextureFormat.RGB24,false);
        camera.aspect=1;camera.orthographicSize=.68f;light.transform.rotation=Quaternion.Euler(35,155,0);
        for(int i=0;i<2;i++)
        {
            var model=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Village/"+(i==0?"Player":"Villager")));
            var center=model.transform.Find("Body/Head").position+Vector3.up*.07f;
            if(i==0){center+=Vector3.up*.18f;camera.orthographicSize=.94f;}
            else camera.orthographicSize=.68f;
            camera.transform.position=center+new Vector3(.25f,.10f,1).normalized*5;camera.transform.LookAt(center);
            var frame=Render(camera,800,800);image.SetPixels(i*800,0,800,800,frame.GetPixels());UnityEngine.Object.DestroyImmediate(frame);UnityEngine.Object.DestroyImmediate(model);
        }
        image.Apply();File.WriteAllBytes(Path.Combine(output,"faces.png"),image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
    }
    private static void RenderGripPoses(string output,Camera camera,Light light)
    {
        var model=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Village/Player"));
        var arm=model.transform.Find("Body/ArmR");var grip=arm.Find("Hand/GripAxis");
        var image=new Texture2D(1500,500,TextureFormat.RGB24,false);
        camera.aspect=1;camera.orthographicSize=.30f;light.transform.rotation=Quaternion.Euler(40,145,0);
        for(int i=0;i<3;i++)
        {
            WoodcuttingPose.Apply(model.transform,new[]{0f,.17f,WoodcuttingPose.ImpactTime}[i],true);
            camera.transform.position=grip.position+new Vector3(1,.25f,1).normalized*3;camera.transform.LookAt(grip.position);
            var frame=Render(camera,500,500);image.SetPixels(i*500,0,500,500,frame.GetPixels());UnityEngine.Object.DestroyImmediate(frame);
        }
        image.Apply();File.WriteAllBytes(Path.Combine(output,"axe-grip.png"),image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(model);
    }
    private static void RenderAdventurer(string output,Camera camera,Light light)
    {
        var image=new Texture2D(2100,1000,TextureFormat.RGB24,false);
        var model=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Village/Player"));
        var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);
        var floorMaterial=new Material(Shader.Find("Standard")){color=new Color(.86f,.84f,.77f)};
        floor.GetComponent<Renderer>().sharedMaterial=floorMaterial;
        camera.aspect=.7f;camera.orthographicSize=1.44f;
        var center=new Vector3(0,1.30f,0);
        for(int i=0;i<3;i++)
        {
            float yaw=new[]{0f,45f,180f}[i];
            model.transform.rotation=Quaternion.Euler(0,yaw,0);
            light.transform.rotation=Quaternion.Euler(35,145,0);
            camera.transform.position=center+new Vector3(0,.09f,1).normalized*8;camera.transform.LookAt(center);
            var frame=Render(camera,700,1000);image.SetPixels(i*700,0,700,1000,frame.GetPixels());UnityEngine.Object.DestroyImmediate(frame);
        }
        image.Apply();File.WriteAllBytes(Path.Combine(output,"adventurer.png"),image.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(model);UnityEngine.Object.DestroyImmediate(floor);UnityEngine.Object.DestroyImmediate(floorMaterial);
    }
    private static void RenderWoodcutting(string output,Camera camera,Light light)
    {
        var model=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Village/Player"));
        var tree=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Village/Tree"),new Vector3(0,0,OctOpus.Shared.TreeInteractionRules.StopDistance),Quaternion.identity);
        // Crop the crown only in the inspection view so both hands remain visible.
        tree.transform.Find("Canopy").gameObject.SetActive(false);
        var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);
        var material=new Material(Shader.Find("Standard")){color=new Color(.74f,.77f,.65f)};
        floor.GetComponent<Renderer>().sharedMaterial=material;
        light.transform.rotation=Quaternion.Euler(40,130,0);
        camera.aspect=1;camera.orthographicSize=1.45f;
        var center=new Vector3(0,1.15f,.28f);
        camera.transform.position=center+new Vector3(5,2.2f,4);camera.transform.LookAt(center);
        var sheet=new Texture2D(2000,500,TextureFormat.RGB24,false);
        for(int i=0;i<4;i++)
        {
            WoodcuttingPose.Apply(model.transform,new[]{0f,.17f,WoodcuttingPose.ImpactTime,.42f}[i],true);
            var frame=Render(camera,500,500);sheet.SetPixels(i*500,0,500,500,frame.GetPixels());UnityEngine.Object.DestroyImmediate(frame);
        }
        sheet.Apply();File.WriteAllBytes(Path.Combine(output,"woodcutting-poses.png"),sheet.EncodeToPNG());UnityEngine.Object.DestroyImmediate(sheet);
        string frames=Path.Combine(output,"woodcutting-frames");Directory.CreateDirectory(frames);
        for(int i=0;i<36;i++)
        {
            float time=i/30f-.20f;
            WoodcuttingPose.Apply(model.transform,time<0?float.PositiveInfinity:time,true);
            float hit=time-WoodcuttingPose.ImpactTime;
            tree.transform.localRotation=Quaternion.Euler(0,0,hit>=0 && hit<.4f?Mathf.Sin(hit*45)*(1-hit/.4f)*2:0);
            var frame=Render(camera,720,720);File.WriteAllBytes(Path.Combine(frames,i.ToString("D3")+".png"),frame.EncodeToPNG());UnityEngine.Object.DestroyImmediate(frame);
        }
        UnityEngine.Object.DestroyImmediate(model);UnityEngine.Object.DestroyImmediate(tree);UnityEngine.Object.DestroyImmediate(floor);UnityEngine.Object.DestroyImmediate(material);
    }
    private static Texture2D Render(Camera camera,int width,int height)
    {
        var target=new RenderTexture(width,height,24);
        var previous=RenderTexture.active;
        camera.targetTexture=target;camera.Render();RenderTexture.active=target;
        var image=new Texture2D(width,height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
        camera.targetTexture=null;RenderTexture.active=previous;target.Release();UnityEngine.Object.DestroyImmediate(target);
        return image;
    }
}
