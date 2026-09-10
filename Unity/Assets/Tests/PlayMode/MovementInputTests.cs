using System;
using NUnit.Framework;
using UnityEngine;

public class MovementInputTests
{
    private string key;

    [SetUp]
    public void Setup() { key = "OctOpus.Test.Move." + Guid.NewGuid().ToString("N"); }

    [TearDown]
    public void Cleanup() { PlayerPrefs.DeleteKey(key); }

    [Test]
    public void NewSettings_DefaultToLeftButton()
    {
        Assert.That(new MovementInput(key).Button, Is.EqualTo(0));
    }

    [TestCase(1)]
    [TestCase(2)]
    public void ChangedButton_SurvivesNewSettingsInstance(int button)
    {
        new MovementInput(key).SetButton(button);
        Assert.That(new MovementInput(key).Button, Is.EqualTo(button));
    }

    [Test]
    public void CorruptedPreference_FallsBackToLeftButton()
    {
        PlayerPrefs.SetInt(key, 9);
        Assert.That(new MovementInput(key).Button, Is.EqualTo(0));
    }

    [TestCase(-1)]
    [TestCase(3)]
    public void UnsupportedButton_DoesNotReplaceSavedBinding(int button)
    {
        var input = new MovementInput(key);
        input.SetButton(1);
        Assert.Throws<ArgumentOutOfRangeException>(() => input.SetButton(button));
        Assert.That(new MovementInput(key).Button, Is.EqualTo(1));
    }

    [TestCase(30, 30)]
    [TestCase(350, 490)]
    public void WholePanel_BlocksWorldInput(float x, float y)
    {
        var input = new MovementInput(key);
        input.UpdateFocus(true, 10);
        Assert.That(input.CanMove(new Vector2(x, 600 - y), 600, true, 20), Is.False);
        Assert.That(input.CanMove(new Vector2(500, 300), 600, true, 20), Is.True);
    }

    [Test]
    public void FocusClickAndFollowingFrame_AreBlocked_ThenMovementResumes()
    {
        var input = new MovementInput(key);
        var point = new Vector2(500, 300);
        input.UpdateFocus(false, 10);
        Assert.That(input.CanMove(point, 600, false, 11), Is.False);
        input.UpdateFocus(true, 12);
        Assert.That(input.CanMove(point, 600, true, 12), Is.False);
        Assert.That(input.CanMove(point, 600, true, 13), Is.False);
        Assert.That(input.CanMove(point, 600, true, 14), Is.True);
    }
}
