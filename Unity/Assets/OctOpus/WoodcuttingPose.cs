using System.Runtime.CompilerServices;
using UnityEngine;

// Presentation only: elapsed starts at an accepted strike, never at a local input request.
public static class WoodcuttingPose
{
    public const float Duration = .56f;
    public const float ImpactTime = .29f;
    public const float UpperArmLength = .28f;
    public const float ForearmLength = .26f;
    public const float LeftGripY = -.12f;
    private static readonly ConditionalWeakTable<Transform, Rig> Rigs = new ConditionalWeakTable<Transform, Rig>();
    private static readonly ConditionalWeakTable<Transform, Rig>.CreateValueCallback RigFactory = CreateRig;

    public static void Apply(Transform model, float elapsed, bool working, float stride = 0f)
    {
        if (model != null) Rigs.GetValue(model, RigFactory).Apply(elapsed, working, stride);
    }

    private static Rig CreateRig(Transform model) => new Rig(model);

    public sealed class Rig
    {
        private readonly Transform body, leftArm, rightArm, leftForearm, rightForearm, leftHand, rightHand;
        private readonly Transform leftGrip, rightGrip, supportGrip, relaxedHand;
        private readonly Transform model, leftLeg, rightLeg;
        private readonly Vector3 leftLegOrigin, rightLegOrigin, leftLegLocalOrigin, rightLegLocalOrigin;
        private readonly Quaternion leftLegRotation, rightLegRotation;
        private readonly Vector3 bodyOrigin;
        private static readonly Vector3 ReadyGrip = new Vector3(.03f, 1.10f, .28f);
        private static readonly Vector3 ReadyAxis = new Vector3(.20f, .87f, .44f);

        public Rig(Transform model)
        {
            this.model = model;
            body = model.Find("Body");
            leftLeg = body.Find("LegL"); rightLeg = body.Find("LegR");
            leftLegOrigin = model.InverseTransformPoint(leftLeg.position); rightLegOrigin = model.InverseTransformPoint(rightLeg.position);
            leftLegLocalOrigin = leftLeg.localPosition; rightLegLocalOrigin = rightLeg.localPosition;
            leftLegRotation = Quaternion.Inverse(model.rotation) * leftLeg.rotation;
            rightLegRotation = Quaternion.Inverse(model.rotation) * rightLeg.rotation;
            leftArm = body.Find("ArmL"); rightArm = body.Find("ArmR");
            leftForearm = leftArm.Find("Forearm"); rightForearm = rightArm.Find("Forearm");
            leftHand = leftArm.Find("Hand"); rightHand = rightArm.Find("Hand");
            leftGrip = leftHand.Find("GripAxis"); rightGrip = rightHand.Find("GripAxis");
            supportGrip = leftHand.Find("SupportGrip"); relaxedHand = leftHand.Find("Sculpted relaxed hand");
            bodyOrigin = body.localPosition;
        }

        public void Apply(float elapsed, bool working, float stride = 0f)
        {
            bool swinging = !float.IsNaN(elapsed) && elapsed >= 0f && elapsed < Duration;
            bool twoHanded = working || swinging;
            float step = float.IsNaN(stride) || float.IsInfinity(stride) ? 0f : Mathf.Clamp(stride, -23f, 23f);
            Vector3 grip = ReadyGrip, axis = ReadyAxis;
            float twist = 0f, bend = 0f, shift = 0f;
            if (swinging)
            {
                if (elapsed < .17f)
                    Blend(ReadyGrip, ReadyAxis, 0f, 0f, 0f,
                        new Vector3(.20f, 1.20f, .16f), new Vector3(.65f, .73f, -.21f), -16f, -3f, -.018f,
                        elapsed / .17f, out grip, out axis, out twist, out bend, out shift);
                else if (elapsed < ImpactTime)
                    Blend(new Vector3(.20f, 1.20f, .16f), new Vector3(.65f, .73f, -.21f), -16f, -3f, -.018f,
                        new Vector3(0f, 1.10f, .27f), new Vector3(.05f, .50f, .865f), 4f, 3f, .018f,
                        (elapsed - .17f) / (ImpactTime - .17f), out grip, out axis, out twist, out bend, out shift);
                else if (elapsed < .34f)
                    Blend(new Vector3(0f, 1.10f, .27f), new Vector3(.05f, .50f, .865f), 4f, 3f, .018f,
                        new Vector3(-.015f, 1.06f, .26f), new Vector3(-.10f, .43f, .90f), 7f, 4f, .023f,
                        (elapsed - ImpactTime) / (.34f - ImpactTime), out grip, out axis, out twist, out bend, out shift);
                else
                    Blend(new Vector3(-.015f, 1.06f, .26f), new Vector3(-.10f, .43f, .90f), 7f, 4f, .023f,
                        ReadyGrip, ReadyAxis, 0f, 0f, 0f,
                        (elapsed - .34f) / (Duration - .34f), out grip, out axis, out twist, out bend, out shift);
            }
            if (!twoHanded)
            {
                grip = new Vector3(.43f, .77f, .06f + step * .0012f);
                axis = new Vector3(0f, -.92f, .39f);
            }
            // Shift weight toward the trunk with the swing; network/root position is untouched.
            float forward = twoHanded ? .03f : 0f;
            if (swinging)
            {
                if (elapsed < .17f) forward = Mathf.Lerp(.03f, 0f, Mathf.SmoothStep(0f, 1f, elapsed / .17f));
                else if (elapsed < ImpactTime) forward = Mathf.Lerp(0f, .04f, Mathf.SmoothStep(0f, 1f, (elapsed - .17f) / (ImpactTime - .17f)));
                else if (elapsed < .34f) forward = Mathf.Lerp(.04f, .05f, Mathf.SmoothStep(0f, 1f, (elapsed - ImpactTime) / (.34f - ImpactTime)));
                else forward = Mathf.Lerp(.05f, .03f, Mathf.SmoothStep(0f, 1f, (elapsed - .34f) / (Duration - .34f)));
            }
            body.localPosition = bodyOrigin + new Vector3(shift, 0f, forward);
            body.localRotation = Quaternion.Euler(bend, twist, 0f);
            if (twoHanded)
            {
                // Torso weight shift pivots above planted feet instead of carrying the boots with it.
                leftLeg.SetPositionAndRotation(model.TransformPoint(leftLegOrigin), model.rotation * leftLegRotation);
                rightLeg.SetPositionAndRotation(model.TransformPoint(rightLegOrigin), model.rotation * rightLegRotation);
            }
            else
            {
                leftLeg.localPosition = leftLegLocalOrigin; rightLeg.localPosition = rightLegLocalOrigin;
                leftLeg.localRotation = Quaternion.Euler(step, 0f, 0f);
                rightLeg.localRotation = Quaternion.Euler(-step, 0f, 0f);
            }
            if (supportGrip != null) supportGrip.gameObject.SetActive(twoHanded);
            if (relaxedHand != null) relaxedHand.gameObject.SetActive(!twoHanded);
            Quaternion toolRotation = ToolRotation(axis);
            PoseArm(rightArm, rightForearm, rightHand, rightGrip, grip, toolRotation, 1f);
            if (twoHanded)
                PoseArm(leftArm, leftForearm, leftHand, leftGrip,
                    grip + toolRotation * Vector3.up * ((LeftGripY - .10f) * .78f),
                    toolRotation * Quaternion.Euler(0f, 180f, 0f), -1f);
            else
            {
                leftArm.localRotation = Quaternion.Euler(0f, 0f, -11f);
                leftForearm.localPosition = Vector3.down * UpperArmLength;
                leftForearm.localRotation = Quaternion.identity;
                leftHand.localPosition = Vector3.down * (UpperArmLength + ForearmLength);
                leftHand.localRotation = Quaternion.identity;
            }
        }

        private void PoseArm(Transform arm, Transform forearm, Transform hand, Transform gripAxis,
            Vector3 gripPosition, Quaternion gripRotation, float side)
        {
            Quaternion handRotation = gripRotation * Quaternion.Inverse(gripAxis.localRotation);
            Vector3 wrist = gripPosition - handRotation * gripAxis.localPosition;
            Vector3 shoulder = arm.localPosition;
            Vector3 delta = wrist - shoulder;
            float distance = Mathf.Clamp(delta.magnitude, .0201f, UpperArmLength + ForearmLength - .0001f);
            Vector3 direction = delta.normalized;
            // An outward elbow pole keeps the upper arms clear of the coat and chest.
            Vector3 pole = new Vector3(side, -.35f, -.15f);
            Vector3 bendDirection = Vector3.ProjectOnPlane(pole, direction).normalized;
            float along = (UpperArmLength * UpperArmLength - ForearmLength * ForearmLength + distance * distance) / (2f * distance);
            float height = Mathf.Sqrt(Mathf.Max(0f, UpperArmLength * UpperArmLength - along * along));
            Vector3 elbow = shoulder + direction * along + bendDirection * height;
            arm.localRotation = Quaternion.FromToRotation(Vector3.down, (elbow - shoulder).normalized);
            forearm.localPosition = Vector3.down * UpperArmLength;
            forearm.rotation = body.rotation * Quaternion.FromToRotation(Vector3.down, (wrist - elbow).normalized);
            hand.position = body.TransformPoint(wrist);
            hand.rotation = body.rotation * handRotation;
        }

        private static Quaternion ToolRotation(Vector3 axis)
        {
            Vector3 shaft = axis.normalized;
            Vector3 edge = Vector3.ProjectOnPlane(Vector3.forward, shaft).normalized;
            return Quaternion.LookRotation(Vector3.Cross(edge, shaft), shaft);
        }

        private static void Blend(Vector3 gripA, Vector3 axisA, float twistA, float bendA, float shiftA,
            Vector3 gripB, Vector3 axisB, float twistB, float bendB, float shiftB, float t,
            out Vector3 grip, out Vector3 axis, out float twist, out float bend, out float shift)
        {
            t = Mathf.SmoothStep(0f, 1f, t);
            grip = Vector3.Lerp(gripA, gripB, t); axis = Vector3.Slerp(axisA, axisB, t);
            twist = Mathf.Lerp(twistA, twistB, t); bend = Mathf.Lerp(bendA, bendB, t); shift = Mathf.Lerp(shiftA, shiftB, t);
        }
    }
}
