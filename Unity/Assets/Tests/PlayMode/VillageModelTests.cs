using NUnit.Framework;
using UnityEngine;
using FishNet.Object;

public class VillageModelTests
{
    [TestCase("Player")][TestCase("Villager")][TestCase("Axe")][TestCase("Backpack")]
    [TestCase("Tree")][TestCase("Stump")][TestCase("Wood")][TestCase("Cottage")]
    [TestCase("Workshop")][TestCase("Lantern")][TestCase("Signpost")][TestCase("Details")]
    public void VisualAssetsHaveMeshesButCannotChangePhysicsOrNetworking(string name)
    {
        var prefab=Resources.Load<GameObject>("Village/"+name);
        Assert.That(prefab,Is.Not.Null,name);
        Assert.That(prefab.GetComponentsInChildren<Collider>(true),Is.Empty);
        Assert.That(prefab.GetComponentsInChildren<NetworkObject>(true),Is.Empty);
        var filters=prefab.GetComponentsInChildren<MeshFilter>(true);
        Assert.That(filters.Length,Is.GreaterThan(0));
        foreach(var filter in filters)
        {
            Assert.That(filter.sharedMesh,Is.Not.Null);
            Assert.That(filter.sharedMesh.vertexCount,Is.GreaterThan(0));
            Assert.That(filter.GetComponent<Renderer>().sharedMaterial,Is.Not.Null);
        }
    }
    [Test]
    public void ResidentHasNoAxeAndPlayerHasArticulatedLimbs()
    {
        var resident=Resources.Load<GameObject>("Village/Villager");
        foreach(var part in resident.GetComponentsInChildren<Transform>(true))
            Assert.That(part.name,Does.Not.Contain("Axe"));
        var player=Resources.Load<GameObject>("Village/Player");
        foreach(string bone in new[]{"LegL","LegR","ArmL","ArmR","Head"})
            Assert.That(player.transform.Find("Body/"+bone),Is.Not.Null,bone);
        Assert.That(player.transform.Find("Body/ArmR/Hand/HeldAxe"),Is.Not.Null);
    }
}
