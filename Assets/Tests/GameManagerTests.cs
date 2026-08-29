using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class GameManagerTests
{
    private GameManager gameManager;
    private GameObject gameManagerObject;

    [SetUp]
    public void Setup()
    {
        gameManagerObject = new GameObject("GameManager");
        gameManager = gameManagerObject.AddComponent<GameManager>();
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(gameManagerObject);
    }

    [Test]
    public void TimerStartsAtMaxTime()
    {
        gameManager.maxTime = 60f;
        gameManager.StartGame();
        Assert.AreEqual(60f, gameManager.TimeLeft);
    }

    [UnityTest]
    public IEnumerator TimerCountsDown()
    {
        gameManager.maxTime = 60f;
        gameManager.StartGame();
        float initialTime = gameManager.TimeLeft;

        yield return null; // Wait one frame

        Assert.Less(gameManager.TimeLeft, initialTime);
    }

    [Test]
    public void TimerReachingZeroTriggersLose()
    {
        gameManager.maxTime = 0.01f;
        gameManager.StartGame();
        
        // Fast forward time logic or manually update
        gameManager.ForceUpdate(0.02f); 
        
        Assert.AreEqual(GameState.Lost, gameManager.CurrentState);
    }
}
