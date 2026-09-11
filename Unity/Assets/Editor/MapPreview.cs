using System.IO;
using OctOpus.Shared;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MapPreview
{
    // Graphics-enabled Editor only. This is a layout capture, not a networking verification.
    public static void Capture()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var root = new GameObject("Map layout preview");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
        RenderTexture target = null;
        Texture2D pixels = null;
        RenderTexture previous = RenderTexture.active;
        try
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.SetParent(root.transform);
            ground.transform.localScale = Vector3.one * (WorldLayout.HalfExtent / 5);
            var cameraObject = new GameObject("Preview camera");
            cameraObject.transform.SetParent(root.transform);
            var camera = cameraObject.AddComponent<Camera>();
            camera.scene = scene;
            var lightObject = new GameObject("Preview sun");
            lightObject.transform.SetParent(root.transform);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            var view = root.AddComponent<PrototypeMapView>();
            view.EnsureBuilt(ground, camera);

            // Copy only prefab meshes: no NetworkObject, RPCs, or extra live resource trees.
            var treePrefab = Resources.Load<GameObject>("OctOpusTree");
            if (treePrefab == null) throw new System.InvalidOperationException("OctOpusTree prefab is missing.");
            foreach (Vector3 position in WorldLayout.TreePositions)
            foreach (MeshFilter source in treePrefab.GetComponentsInChildren<MeshFilter>(true))
            {
                var item = new GameObject("Preview only: " + source.name);
                item.transform.SetParent(root.transform);
                item.transform.position = position + treePrefab.transform.InverseTransformPoint(source.transform.position);
                item.transform.rotation = source.transform.rotation;
                item.transform.localScale = source.transform.lossyScale;
                item.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
                var renderer = item.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = source.GetComponent<MeshRenderer>().sharedMaterials;
                var color = new MaterialPropertyBlock();
                color.SetColor("_Color", source.name == "Trunk" ? new Color(.4f, .25f, .12f) : new Color(.2f, .6f, .25f));
                renderer.SetPropertyBlock(color);
            }
            const int width = 1280, height = 800;
            target = new RenderTexture(width, height, 24);
            camera.targetTexture = target;
            view.FrameCamera(width, height);
            RenderTexture.active = target;
            GL.Clear(true, true, camera.backgroundColor);
            camera.Render();
            pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            pixels.Apply();
            Directory.CreateDirectory("TestResults");
            string path = Path.GetFullPath("TestResults/map-preview.png");
            File.WriteAllBytes(path, pixels.EncodeToPNG());
            Debug.Log("[OctOpus] Layout-only preview saved: " + path);
        }
        finally
        {
            RenderTexture.active = previous;
            Object.DestroyImmediate(root);
            if (pixels != null) Object.DestroyImmediate(pixels);
            if (target != null) Object.DestroyImmediate(target);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
