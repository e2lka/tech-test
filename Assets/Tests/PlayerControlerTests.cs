using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Platformer.Mechanics;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PlayerControlerTest : IPrebuildSetup, IPostBuildCleanup
{
    // The setup/teardown code is taken from the 12th sample of the Test Framework package.
    // I use it here to cleanly load a test scene in my runtime tests without affecting Build settings permanently.

    private InputTestFixture input = new InputTestFixture();
    
    private string originalScene;
    private const string k_SceneName = "Assets/Tests/TestScene.unity";
    
    public void Setup()
    {
#if UNITY_EDITOR
        if (EditorBuildSettings.scenes.Any(scene => scene.path == k_SceneName))
        {
            return;
        }
        
        var includedScenes = EditorBuildSettings.scenes.ToList();
        includedScenes.Add(new EditorBuildSettingsScene(k_SceneName, true));
        EditorBuildSettings.scenes = includedScenes.ToArray();
#endif
    }

    [UnitySetUp]
    public IEnumerator SetupBeforeTest()
    {
        input.Setup();

        originalScene = SceneManager.GetActiveScene().path;
        if (!File.Exists(k_SceneName))
        {
            Assert.Inconclusive("The path to the Scene is not correct. Set the correct path for the k_SceneName variable.");
        }
        SceneManager.LoadScene(k_SceneName);
        yield return null; // Skip a frame, allowing the scene to load.
    }
    
    [TearDown]
    public void TeardownAfterTest()
    {
        SceneManager.LoadScene(originalScene);
        
        input.TearDown();
    }

    public void Cleanup()
    {
#if UNITY_EDITOR
        EditorBuildSettings.scenes = EditorBuildSettings.scenes.Where(scene => scene.path != k_SceneName).ToArray();
#endif
    }

    /// <summary>
    /// Ensures that a Player go through all expected JumpState when StartJumping is called.
    /// </summary>
    [UnityTest]
    public IEnumerator StartJumping_JumpStateAreCorrect()
    {
        var go = GameObject.Find("Player");
        var controller = go.GetComponent<PlayerController>();
        
        while (!controller.IsGrounded) // Ensure we start from the ground.
            yield return null;

        Assert.AreEqual(PlayerController.JumpState.Grounded, controller.jumpState);

        controller.StartJumping();
        
        Assert.AreEqual(PlayerController.JumpState.PrepareToJump, controller.jumpState);

        yield return null;
        
        Assert.AreEqual(PlayerController.JumpState.Jumping, controller.jumpState);
        
        yield return null;
        
        Assert.AreEqual(PlayerController.JumpState.InFlight, controller.jumpState);
        
        yield return new WaitUntil(() => controller.IsGrounded);
        
        Assert.AreEqual(PlayerController.JumpState.Landed, controller.jumpState);

        yield return null;
        
        Assert.AreEqual(PlayerController.JumpState.Grounded, controller.jumpState);
    }

    /// <summary>
    /// Ensures that a Player velocity start diminishing when StopJumping is called.
    /// </summary>
    [UnityTest]
    public IEnumerator StopJumping_VelocityIsDiminishing()
    {
        var go = GameObject.Find("Player");
        var controller = go.GetComponent<PlayerController>();
        
        while (!controller.IsGrounded) // Ensure we start from the ground.
            yield return null;
        
        controller.StartJumping();
        yield return null;

        controller.StopJumping();
        var velocityY = Math.Abs(controller.velocity.y);

        // NOTE: I don't know why I have to wait 2 frames for it to work. If I have more time, I'll investigate that
        //       more closely.
        yield return null;
        yield return null;

        // Player is slowing down, in preparation for landing.
        Assert.Greater(velocityY, Math.Abs(controller.velocity.y));
    }
    
    // Ideally I would also add tests that ensure that keyboard spacebar and gamepad buttonsouth trigger a jump.
    // This would ensure that any unwanted modification of the input system can be catched

    [UnityTest]
    public IEnumerator HitKeyboardSpaceBar_TriggerJump()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        var go = GameObject.Find("Player");
        var controller = go.GetComponent<PlayerController>();
        
        while (!controller.IsGrounded) // Ensure we start from the ground.
            yield return null;
        
        Assert.AreEqual(PlayerController.JumpState.Grounded, controller.jumpState);
    
        input.Press(keyboard.spaceKey);
        yield return null;
        
        Assert.AreEqual(PlayerController.JumpState.Jumping, controller.jumpState);
        
        input.Release(keyboard.spaceKey);
    }

    [UnityTest]
    public IEnumerator HitGamepadButtonSouth_TriggerJump()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        var go = GameObject.Find("Player");
        var controller = go.GetComponent<PlayerController>();
        
        while (!controller.IsGrounded) // Ensure we start from the ground.
            yield return null;
        
        Assert.AreEqual(PlayerController.JumpState.Grounded, controller.jumpState);
    
        input.Press(gamepad.buttonSouth);
        yield return null;
        
        Assert.AreEqual(PlayerController.JumpState.Jumping, controller.jumpState);
        
        input.Release(gamepad.buttonSouth);
    }
}


// THOUGHT PROCESS #1
// Write one test to cover the player jumping: start from Grounded then go through PrepareToJump, Jumping,
// InFlight, Landed.
// (Should it be one test or multiple tests for each jumping state? Look for what's common to do)
//
// To be able to do that, I need to:
// - Load the scene (maybe I can load/create a test scene that is as minimal as possible for my test, just the
//   player and a ground for example
// - Set a starting state for the player (Grounded in this case)
// - Simulate a press on space bar (or I guess a signal that is not device specific ideally)
//      - Guess: That might be the part that requires a refactor of PlayerController
// - Skip frame (yield return null) to validate that the jump state evolve and go through each state

// THOUGHT PROCESS #2
// Because I don't entirely know what a good "Update" function should look like, my idea is to start trying
// to write the test that is asked, and deduce what could/should change in the code to make my life easier
// (almost some kind of Test Driven Development approach, where I set my test's expectation first and then I'll
// tweak the PlayerController code to make it work).
