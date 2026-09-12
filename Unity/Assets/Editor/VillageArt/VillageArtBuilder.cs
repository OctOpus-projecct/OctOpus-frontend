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
            foreach(var mesh in art.Root.GetComponentsInChildren<MeshFilter>()) mesh.sharedMesh=(Mesh)saved[mesh.sharedMesh];
            foreach(var renderer in art.Root.GetComponentsInChildren<MeshRenderer>()) renderer.sharedMaterial=(Material)saved[renderer.sharedMaterial];
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
