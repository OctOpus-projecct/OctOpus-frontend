using System.Collections.Generic;
using OctOpus.Shared;
using UnityEngine;

/// <summary>Non-blocking scenery for the shared flat test map; never creates network resources.</summary>
public sealed class PrototypeMapView : MonoBehaviour
{
    private GameObject root;
    private readonly List<Material> materials = new List<Material>();
    private readonly List<Transform> labels = new List<Transform>();
    private Renderer groundRenderer;
    private Transform groundTransform;
    private Vector3 originalGroundPosition;
    private Quaternion originalGroundRotation;
    private Vector3 originalGroundScale;
    private Material originalGroundMaterial;
    private Camera mapCamera;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private Rect originalCameraRect;
    private bool originalOrthographic;
    private float originalSize;
    private float originalAspect;
    private Color originalBackground;
    private CameraClearFlags originalClearFlags;

    public void EnsureBuilt(GameObject groundObject, Camera camera)
    {
        if (root != null) return;
        root = new GameObject("Prototype Map Scenery");
        root.transform.SetParent(transform, false);
        var grass = MakeMaterial(new Color(.34f, .43f, .35f));
        var path = MakeMaterial(new Color(.66f, .65f, .51f));
        var clearing = MakeMaterial(new Color(.49f, .58f, .48f));
        var accent = MakeMaterial(new Color(.36f, .67f, .66f));
        var guide = MakeMaterial(new Color(.82f, .64f, .36f));
        var edge = MakeMaterial(new Color(.24f, .32f, .28f));
        if (groundObject != null)
        {
            groundTransform = groundObject.transform;
            originalGroundPosition = groundTransform.position;
            originalGroundRotation = groundTransform.rotation;
            originalGroundScale = groundTransform.localScale;
            groundTransform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            groundTransform.localScale = Vector3.one * (WorldLayout.HalfExtent / 5);
        }
        groundRenderer = groundObject == null ? null : groundObject.GetComponent<Renderer>();
        if (groundRenderer != null)
        {
            originalGroundMaterial = groundRenderer.sharedMaterial;
            groundRenderer.sharedMaterial = grass;
        }

        // Preserve the existing plane/collider identities; size their raycast area to the shared map.
        float extent = WorldLayout.HalfExtent;
        Primitive("Map rim north", PrimitiveType.Cube, new Vector3(0, -.04f, extent), new Vector3(extent * 2, .12f, .12f), edge);
        Primitive("Map rim south", PrimitiveType.Cube, new Vector3(0, -.04f, -extent), new Vector3(extent * 2, .12f, .12f), edge);
        Primitive("Map rim east", PrimitiveType.Cube, new Vector3(extent, -.04f, 0), new Vector3(.12f, .12f, extent * 2), edge);
        Primitive("Map rim west", PrimitiveType.Cube, new Vector3(-extent, -.04f, 0), new Vector3(.12f, .12f, extent * 2), edge);
        for (int i = 1; i < WorldLayout.PathPoints.Count; i++)
            Path(WorldLayout.PathPoints[i - 1], WorldLayout.PathPoints[i], path);
        Path(WorldLayout.SpawnCenter, WorldLayout.GuidePosition, path);
        Primitive("Spawn clearing", PrimitiveType.Cylinder, WorldLayout.SpawnCenter + Vector3.up * .025f,
            new Vector3(7, .02f, 5), clearing);
        Primitive("Spawn marker", PrimitiveType.Cylinder, WorldLayout.SpawnCenter + Vector3.up * .05f,
            new Vector3(1.1f, .015f, 1.1f), accent);
        Primitive("Guide base", PrimitiveType.Cylinder, WorldLayout.GuidePosition + Vector3.up * .055f,
            new Vector3(1.8f, .025f, 1.8f), clearing);
        Primitive("Guide placeholder", PrimitiveType.Capsule, WorldLayout.GuidePosition + Vector3.up,
            new Vector3(.65f, 1, .65f), guide);
        Label("SPAWN", WorldLayout.SpawnCenter + Vector3.up * .35f);
        Label("GUIDE\nplaceholder", WorldLayout.GuidePosition + Vector3.up * 2.8f);
        mapCamera = camera;
        if (mapCamera != null)
        {
            originalCameraPosition = camera.transform.position;
            originalCameraRotation = camera.transform.rotation;
            originalCameraRect = camera.rect;
            originalOrthographic = camera.orthographic;
            originalSize = camera.orthographicSize;
            originalAspect = camera.aspect;
            originalBackground = camera.backgroundColor;
            originalClearFlags = camera.clearFlags;
            FrameCamera(Screen.width, Screen.height);
        }
    }

    public void FrameCamera(int width, int height)
    {
        if (mapCamera == null) return;
        float left = Mathf.Min(MovementInput.PanelRect.xMax + 20, width * .45f);
        mapCamera.rect = new Rect(left / width, 0, 1 - left / width, 1);
        mapCamera.transform.SetPositionAndRotation(new Vector3(18, 26, -25), Quaternion.identity);
        mapCamera.transform.LookAt(Vector3.zero);
        mapCamera.orthographic = true;
        mapCamera.clearFlags = CameraClearFlags.SolidColor;
        mapCamera.backgroundColor = new Color(.12f, .17f, .19f);
        float aspect = (width - left) / height;
        mapCamera.aspect = aspect;
        float size = 0;
        for (int x = -1; x <= 1; x += 2)
        for (int z = -1; z <= 1; z += 2)
        {
            Vector3 corner = Quaternion.Inverse(mapCamera.transform.rotation) *
                new Vector3(x * WorldLayout.HalfExtent, 0, z * WorldLayout.HalfExtent);
            size = Mathf.Max(size, Mathf.Abs(corner.y), Mathf.Abs(corner.x) / aspect);
        }
        mapCamera.orthographicSize = size + 1;
        foreach (Transform label in labels) label.rotation = mapCamera.transform.rotation;
    }

    private void LateUpdate() { FrameCamera(Screen.width, Screen.height); }

    private void Path(Vector3 start, Vector3 end, Material material)
    {
        Vector3 delta = end - start;
        GameObject path = Primitive("Walkable path", PrimitiveType.Cube, (start + end) * .5f + Vector3.up * .012f,
            new Vector3(1.6f, .02f, delta.magnitude + .8f), material);
        path.transform.rotation = Quaternion.LookRotation(delta);
    }

    private GameObject Primitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
    {
        GameObject item = GameObject.CreatePrimitive(type);
        item.name = name;
        item.transform.SetParent(root.transform, false);
        item.transform.position = position;
        item.transform.localScale = scale;
        Collider collider = item.GetComponent<Collider>();
        collider.enabled = false;
        Release(collider);
        item.GetComponent<Renderer>().sharedMaterial = material;
        return item;
    }

    private void Label(string text, Vector3 position)
    {
        var item = new GameObject(text.Replace('\n', ' '));
        item.transform.SetParent(root.transform, false);
        item.transform.position = position;
        TextMesh label = item.AddComponent<TextMesh>();
        label.text = text;
        label.fontSize = 48;
        label.characterSize = .085f;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = new Color(.94f, .95f, .86f);
        labels.Add(item.transform);
    }

    private Material MakeMaterial(Color color)
    {
        var material = new Material(Shader.Find("Standard"));
        material.color = color;
        material.SetFloat("_Glossiness", 0);
        materials.Add(material);
        return material;
    }

    private void OnDestroy()
    {
        if (groundTransform != null)
        {
            groundTransform.SetPositionAndRotation(originalGroundPosition, originalGroundRotation);
            groundTransform.localScale = originalGroundScale;
        }
        if (groundRenderer != null) groundRenderer.sharedMaterial = originalGroundMaterial;
        if (mapCamera != null)
        {
            mapCamera.transform.SetPositionAndRotation(originalCameraPosition, originalCameraRotation);
            mapCamera.rect = originalCameraRect;
            mapCamera.orthographic = originalOrthographic;
            mapCamera.orthographicSize = originalSize;
            mapCamera.aspect = originalAspect;
            mapCamera.backgroundColor = originalBackground;
            mapCamera.clearFlags = originalClearFlags;
        }
        if (root != null) Release(root);
        foreach (Material material in materials) Release(material);
    }

    private static void Release(Object item)
    {
        if (Application.isPlaying) Destroy(item);
        else DestroyImmediate(item);
    }
}
