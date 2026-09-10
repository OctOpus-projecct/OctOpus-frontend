using System.Collections.Generic;
using FishNet.Managing;
using NUnit.Framework;
using OctOpus.Shared;
using UnityEngine;

public class TreeSelectionTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private readonly Ray ray = new Ray(new Vector3(1000, 1000, 1000), Vector3.forward);
    private NetworkManager manager;

    [SetUp]
    public void Setup() { manager = NetworkFactory.Create(); }

    [TearDown]
    public void Cleanup()
    {
        foreach (GameObject item in objects) Object.DestroyImmediate(item);
        objects.Clear();
        Object.DestroyImmediate(manager.gameObject);
    }

    [Test]
    public void ChildCollider_SelectsParentTree()
    {
        NetworkTree tree = CreateTree(5);
        Physics.SyncTransforms();
        Assert.That(ClientBootstrap.FindTree(ray, 20), Is.SameAs(tree));
    }

    [Test]
    public void OverlappingTrees_SelectsNearest()
    {
        CreateTree(10);
        NetworkTree nearest = CreateTree(5);
        Physics.SyncTransforms();
        Assert.That(ClientBootstrap.FindTree(ray, 20), Is.SameAs(nearest));
    }

    [Test]
    public void TreeBeyondGroundDistance_IsNotSelected()
    {
        CreateTree(10);
        Physics.SyncTransforms();
        Assert.That(ClientBootstrap.FindTree(ray, 5), Is.Null);
    }

    private NetworkTree CreateTree(float distance)
    {
        var prefab = Resources.Load<GameObject>("OctOpusTree");
        Assert.That(prefab, Is.Not.Null);
        var root = Object.Instantiate(prefab, ray.GetPoint(distance), Quaternion.identity);
        objects.Add(root);
        NetworkTree tree = root.GetComponent<NetworkTree>();
        foreach (Collider collider in root.GetComponentsInChildren<Collider>()) collider.enabled = false;
        var child = new GameObject("Tree collider");
        child.transform.SetParent(root.transform, false);
        child.AddComponent<BoxCollider>();
        return tree;
    }
}
