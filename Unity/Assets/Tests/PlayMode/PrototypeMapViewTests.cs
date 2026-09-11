using System.Collections;
using NUnit.Framework;
using OctOpus.Shared;
using UnityEngine;
using UnityEngine.TestTools;

public class PrototypeMapViewTests
{
    private GameObject host;
    private GameObject ground;
    private Material originalMaterial;

    [SetUp]
    public void Setup()
    {
        host = new GameObject("Map view test");
        ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        originalMaterial = ground.GetComponent<Renderer>().sharedMaterial;
    }

    [TearDown]
    public void Cleanup()
    {
        Object.DestroyImmediate(host);
        Object.DestroyImmediate(ground);
    }

    [UnityTest]
    public IEnumerator RepeatedInitialization_CreatesOnlyOneNonBlockingView_AndRestoresGround()
    {
        var view = host.AddComponent<PrototypeMapView>();
        view.EnsureBuilt(ground, null);
        view.EnsureBuilt(ground, null);
        yield return null;
        Assert.That(host.transform.childCount, Is.EqualTo(1));
        Assert.That(host.GetComponentsInChildren<Collider>(), Is.Empty, "Scenery must not block movement or tree selection.");
        Assert.That(host.GetComponentsInChildren<NetworkTree>(), Is.Empty, "Only the server spawns resource trees.");
        Assert.That(ground.GetComponent<Collider>().enabled, Is.True);
        Physics.SyncTransforms();
        Assert.That(ground.GetComponent<Collider>().bounds.size.x, Is.EqualTo(WorldLayout.HalfExtent * 2).Within(.01f));
        Assert.That(ground.GetComponent<Collider>().bounds.size.z, Is.EqualTo(WorldLayout.HalfExtent * 2).Within(.01f));
        var corner = new Vector3(WorldLayout.HalfExtent - .1f, 10, WorldLayout.HalfExtent - .1f);
        Assert.That(ground.GetComponent<Collider>().Raycast(new Ray(corner, Vector3.down), out _, 20), Is.True,
            "The existing ground collider must accept clicks at the expanded map's far corner.");
        Object.Destroy(view);
        yield return null;
        yield return null;
        Assert.That(host.transform.childCount, Is.Zero);
        Assert.That(ground.GetComponent<Renderer>().sharedMaterial, Is.SameAs(originalMaterial));
        Assert.That(ground.transform.localScale, Is.EqualTo(Vector3.one));
    }

    [Test]
    public void FramedCamera_KeepsSharedLandmarksInsideMapViewport()
    {
        var cameraObject = new GameObject("Map test camera");
        cameraObject.transform.SetParent(host.transform);
        var camera = cameraObject.AddComponent<Camera>();
        var view = host.AddComponent<PrototypeMapView>();
        view.EnsureBuilt(ground, camera);
        view.FrameCamera(960, 600);
        Assert.That(camera.rect.xMin * 960, Is.GreaterThan(MovementInput.PanelRect.xMax));
        AssertVisible(camera, WorldLayout.SpawnCenter);
        AssertVisible(camera, WorldLayout.GuidePosition + Vector3.up * 2.8f);
        foreach (Vector3 tree in WorldLayout.TreePositions) AssertVisible(camera, tree);
        var guide = host.transform.Find("Prototype Map Scenery/Guide placeholder");
        Assert.That(guide, Is.Not.Null);
        Assert.That(guide.position.x, Is.EqualTo(WorldLayout.GuidePosition.x));
        Assert.That(guide.position.z, Is.EqualTo(WorldLayout.GuidePosition.z));
        Object.DestroyImmediate(view);
    }

    private static void AssertVisible(Camera camera, Vector3 point)
    {
        Vector3 viewport = camera.WorldToViewportPoint(point);
        Assert.That(viewport.z, Is.GreaterThan(0));
        Assert.That(viewport.x, Is.InRange(.02f, .98f));
        Assert.That(viewport.y, Is.InRange(.02f, .98f));
    }
}
