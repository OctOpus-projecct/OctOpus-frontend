using NUnit.Framework;
using UnityEngine;

public class WoodcuttingPoseTests
{
    [Test]
    public void BothFeetStayPlantedWhileTheTorsoDrivesTheStrike()
    {
        var model = Object.Instantiate(Resources.Load<GameObject>("Village/Player"));
        try
        {
            model.transform.SetPositionAndRotation(new Vector3(-2f, 1f, 5f), Quaternion.Euler(0f, 65f, 0f));
            WoodcuttingPose.Apply(model.transform, float.PositiveInfinity, false);
            var left = model.transform.Find("Body/LegL"); var right = model.transform.Find("Body/LegR");
            Vector3 leftPosition = left.position, rightPosition = right.position;
            Quaternion leftRotation = left.rotation, rightRotation = right.rotation;
            for (int frame = 0; frame <= 112; frame++)
            {
                WoodcuttingPose.Apply(model.transform, frame * WoodcuttingPose.Duration / 112f, true, 23f);
                Assert.That(Vector3.Distance(left.position, leftPosition), Is.LessThan(.0001f));
                Assert.That(Vector3.Distance(right.position, rightPosition), Is.LessThan(.0001f));
                Assert.That(Quaternion.Angle(left.rotation, leftRotation), Is.LessThan(.05f));
                Assert.That(Quaternion.Angle(right.rotation, rightRotation), Is.LessThan(.05f));
            }
        }
        finally { Object.DestroyImmediate(model); }
    }

    [TestCase(.10f)]
    [TestCase(.25f)]
    [TestCase(.33f)]
    public void CancellingAtAnyStrikePhaseRestoresTheEntireIdlePose(float elapsed)
    {
        var model = Object.Instantiate(Resources.Load<GameObject>("Village/Player"));
        try
        {
            WoodcuttingPose.Apply(model.transform, float.PositiveInfinity, false);
            var parts = model.GetComponentsInChildren<Transform>(true);
            var positions = new Vector3[parts.Length]; var rotations = new Quaternion[parts.Length];
            for (int i = 0; i < parts.Length; i++) { positions[i] = parts[i].localPosition; rotations[i] = parts[i].localRotation; }
            WoodcuttingPose.Apply(model.transform, elapsed, true);
            // Cancellation clears the accepted strike timestamp in CharacterModelView.
            WoodcuttingPose.Apply(model.transform, float.PositiveInfinity, false);
            for (int i = 0; i < parts.Length; i++)
            {
                Assert.That(Vector3.Distance(parts[i].localPosition, positions[i]), Is.LessThan(.0001f), parts[i].name);
                Assert.That(Quaternion.Angle(parts[i].localRotation, rotations[i]), Is.LessThan(.05f), parts[i].name);
            }
        }
        finally { Object.DestroyImmediate(model); }
    }

    [TestCase(-23f)]
    [TestCase(0f)]
    [TestCase(23f)]
    public void WalkingKeepsIdleAxeAnchoredAndBothWristsConnected(float stride)
    {
        var model = Object.Instantiate(Resources.Load<GameObject>("Village/Player"));
        try
        {
            WoodcuttingPose.Apply(model.transform, float.PositiveInfinity, false, stride);
            var arm = model.transform.Find("Body/ArmR");
            var axe = arm.Find("Hand/HeldAxe"); var grip = arm.Find("Hand/GripAxis");
            Assert.That(Vector3.Distance(axe.TransformPoint(Vector3.up * .10f), grip.position), Is.LessThan(.0001f));
            Assert.That(Vector3.Dot(axe.up, grip.up), Is.GreaterThan(.9999f));
            AssertJoined(arm); AssertJoined(model.transform.Find("Body/ArmL"));
            Assert.That(Quaternion.Angle(model.transform.Find("Body/LegL").localRotation, Quaternion.Euler(stride, 0f, 0f)), Is.LessThan(.05f));
            Assert.That(Quaternion.Angle(model.transform.Find("Body/LegR").localRotation, Quaternion.Euler(-stride, 0f, 0f)), Is.LessThan(.05f));
            Assert.That(model.transform.Find("Body/ArmL/Hand/SupportGrip").gameObject.activeSelf, Is.False);
        }
        finally { Object.DestroyImmediate(model); }
    }

    [Test]
    public void BothHandsHoldTheShaftAndArmJointsStayConnectedThroughoutStrike()
    {
        var model = Object.Instantiate(Resources.Load<GameObject>("Village/Player"));
        try
        {
            model.transform.SetPositionAndRotation(new Vector3(3f, 2f, -4f), Quaternion.Euler(0f, 137f, 0f));
            var body = model.transform.Find("Body");
            var axe = body.Find("ArmR/Hand/HeldAxe");
            var rightGrip = body.Find("ArmR/Hand/GripAxis");
            var leftGrip = body.Find("ArmL/Hand/GripAxis");
            for (int frame = 0; frame <= 112; frame++)
            {
                float elapsed = frame * WoodcuttingPose.Duration / 112f;
                WoodcuttingPose.Apply(model.transform, elapsed, true);
                Assert.That(Vector3.Distance(axe.TransformPoint(Vector3.up * .10f), rightGrip.position), Is.LessThan(.0001f), "Right grip at " + elapsed);
                Assert.That(Vector3.Distance(axe.TransformPoint(Vector3.up * WoodcuttingPose.LeftGripY), leftGrip.position), Is.LessThan(.0001f), "Left grip at " + elapsed);
                Assert.That(Vector3.Dot(axe.up, rightGrip.up), Is.GreaterThan(.9999f));
                Assert.That(Vector3.Dot(axe.up, leftGrip.up), Is.GreaterThan(.9999f));
                Assert.That(Vector3.Dot(rightGrip.right, leftGrip.right), Is.LessThan(-.9999f), "Opposing palms");
                AssertJoined(body.Find("ArmR")); AssertJoined(body.Find("ArmL"));
                foreach (var part in model.GetComponentsInChildren<Transform>(true))
                {
                    Assert.That(float.IsNaN(part.position.sqrMagnitude) || float.IsInfinity(part.position.sqrMagnitude), Is.False, part.name);
                    Assert.That(float.IsNaN(part.rotation.x) || float.IsInfinity(part.rotation.x), Is.False, part.name);
                }
                Assert.That(body.Find("ArmL/Hand/SupportGrip").gameObject.activeSelf, Is.True);
                Assert.That(body.Find("ArmL/Hand/Sculpted relaxed hand").gameObject.activeSelf, Is.False);
            }
        }
        finally { Object.DestroyImmediate(model); }
    }

    [Test]
    public void CompletedAndInterruptedCyclesReturnToTheSameIdlePoseWithoutDrift()
    {
        var model = Object.Instantiate(Resources.Load<GameObject>("Village/Player"));
        try
        {
            WoodcuttingPose.Apply(model.transform, float.PositiveInfinity, false);
            var parts = model.GetComponentsInChildren<Transform>(true);
            var positions = new Vector3[parts.Length]; var rotations = new Quaternion[parts.Length];
            for (int i = 0; i < parts.Length; i++) { positions[i] = parts[i].localPosition; rotations[i] = parts[i].localRotation; }
            for (int cycle = 0; cycle < 8; cycle++)
            {
                for (int frame = 0; frame < 56; frame++) WoodcuttingPose.Apply(model.transform, frame * .01f, true);
                WoodcuttingPose.Apply(model.transform, float.PositiveInfinity, false);
                for (int i = 0; i < parts.Length; i++)
                {
                    Assert.That(Vector3.Distance(parts[i].localPosition, positions[i]), Is.LessThan(.00001f), parts[i].name);
                    Assert.That(Quaternion.Angle(parts[i].localRotation, rotations[i]), Is.LessThan(.05f), parts[i].name);
                }
                AssertJoined(model.transform.Find("Body/ArmL")); AssertJoined(model.transform.Find("Body/ArmR"));
                Assert.That(model.transform.Find("Body/ArmL/Hand/SupportGrip").gameObject.activeSelf, Is.False);
                Assert.That(model.transform.Find("Body/ArmL/Hand/Sculpted relaxed hand").gameObject.activeSelf, Is.True);
            }
        }
        finally { Object.DestroyImmediate(model); }
    }

    [Test]
    public void ImpactIsDistinctFromWindupAndEndsInReadyPose()
    {
        var model = Object.Instantiate(Resources.Load<GameObject>("Village/Player"));
        try
        {
            var axe = model.transform.Find("Body/ArmR/Hand/HeldAxe");
            var edge = axe.Find("Cutting edge").GetComponent<Renderer>();
            WoodcuttingPose.Apply(model.transform, float.PositiveInfinity, true);
            Vector3 readyPosition = axe.position; Quaternion readyRotation = axe.rotation;
            WoodcuttingPose.Apply(model.transform, .17f, true);
            Vector3 windup = edge.bounds.center;
            WoodcuttingPose.Apply(model.transform, WoodcuttingPose.ImpactTime, true);
            Vector3 impact = edge.bounds.center;
            Assert.That(impact.z - windup.z, Is.GreaterThan(.30f), "Axe head travels toward the tree");
            Assert.That(windup.y - impact.y, Is.GreaterThan(.05f), "Axe head descends into the strike");
            WoodcuttingPose.Apply(model.transform, WoodcuttingPose.Duration, true);
            Assert.That(Vector3.Distance(axe.position, readyPosition), Is.LessThan(.00001f));
            Assert.That(Quaternion.Angle(axe.rotation, readyRotation), Is.LessThan(.05f));
        }
        finally { Object.DestroyImmediate(model); }
    }

    private static void AssertJoined(Transform arm)
    {
        var forearm = arm.Find("Forearm"); var hand = arm.Find("Hand");
        Assert.That(Vector3.Distance(arm.TransformPoint(Vector3.down * WoodcuttingPose.UpperArmLength), forearm.position), Is.LessThan(.0001f), "Elbow joins upper arm");
        Assert.That(Vector3.Distance(forearm.TransformPoint(Vector3.down * WoodcuttingPose.ForearmLength), hand.position), Is.LessThan(.0001f), "Wrist joins forearm");
        Assert.That(Vector3.Distance(arm.position, hand.position), Is.LessThanOrEqualTo(WoodcuttingPose.UpperArmLength + WoodcuttingPose.ForearmLength + .0001f));
    }
}
