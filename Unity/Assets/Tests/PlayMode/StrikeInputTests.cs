using System;
using NUnit.Framework;
using UnityEngine;

public sealed class StrikeInputTests
{
    private string key;
    [SetUp] public void Setup() { key = "OctOpus.Test.Strike." + Guid.NewGuid().ToString("N"); }
    [TearDown] public void Cleanup() { PlayerPrefs.DeleteKey(key); }
    [Test] public void DefaultsToSpaceAndPersistsChangedKey()
    {
        var input = new StrikeInput(key);
        Assert.That(input.Key, Is.EqualTo(KeyCode.Space));
        input.SetKey(KeyCode.R, 10);
        Assert.That(new StrikeInput(key).Key, Is.EqualTo(KeyCode.R));
    }
    [TestCase(KeyCode.None)] [TestCase(KeyCode.Escape)] [TestCase(KeyCode.Mouse0)]
    public void InvalidBindingDoesNotReplaceSavedKey(KeyCode invalid)
    {
        var input = new StrikeInput(key);
        Assert.Throws<ArgumentOutOfRangeException>(() => input.SetKey(invalid, 1));
        Assert.That(new StrikeInput(key).Key, Is.EqualTo(KeyCode.Space));
    }
    [Test] public void CorruptPreferenceFallsBackToSpace()
    {
        PlayerPrefs.SetInt(key, int.MaxValue);
        Assert.That(new StrikeInput(key).Key, Is.EqualTo(KeyCode.Space));
    }
    [Test] public void FocusRebindAndTextEditingCannotSubmitHit()
    {
        var input = new StrikeInput(key);
        input.UpdateFocus(10);
        Assert.That(input.CanStrike(true, 11, false, false), Is.False);
        Assert.That(input.CanStrike(true, 12, false, false), Is.True);
        Assert.That(input.CanStrike(false, 12, false, false), Is.False);
        Assert.That(input.CanStrike(true, 12, true, false), Is.False);
        Assert.That(input.CanStrike(true, 12, false, true), Is.False);
        input.SetKey(KeyCode.R, 20);
        Assert.That(input.CanStrike(true, 21, false, false), Is.False);
        Assert.That(input.CanStrike(true, 22, false, false), Is.True);
    }
}
