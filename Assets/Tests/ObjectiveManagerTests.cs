using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class ObjectiveManagerTests
{
    private ObjectiveManager objectiveManager;
    private GameObject objManagerObject;
    private GameObject chair;
    private GameObject table;

    [SetUp]
    public void Setup()
    {
        objManagerObject = new GameObject("ObjectiveManager");
        objectiveManager = objManagerObject.AddComponent<ObjectiveManager>();

        chair = new GameObject("Chair");
        table = new GameObject("Table");

        objectiveManager.objectives = new List<Objective>
        {
            new Objective { targetObject = chair, requiredColor = Color.red },
            new Objective { targetObject = table, requiredColor = Color.blue }
        };
        objectiveManager.InitializeObjectives();
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(objManagerObject);
        Object.DestroyImmediate(chair);
        Object.DestroyImmediate(table);
    }

    [Test]
    public void ColorMatchUpdatesObjective()
    {
        objectiveManager.OnObjectColored(chair, Color.red);
        Assert.AreEqual(1, objectiveManager.CorrectColors);
        Assert.AreEqual(0, objectiveManager.IncorrectColors);
    }

    [Test]
    public void WrongColorCountsAsIncorrect()
    {
        objectiveManager.OnObjectColored(chair, Color.green);
        Assert.AreEqual(0, objectiveManager.CorrectColors);
        Assert.AreEqual(1, objectiveManager.IncorrectColors);
    }

    [Test]
    public void WinConditionTriggeredWhenAllCorrect()
    {
        bool won = false;
        objectiveManager.OnAllObjectivesCompleted += () => won = true;

        objectiveManager.OnObjectColored(chair, Color.red);
        objectiveManager.OnObjectColored(table, Color.blue);

        Assert.IsTrue(won);
    }
}
