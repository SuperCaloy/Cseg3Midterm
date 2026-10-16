using NUnit.Framework;
using UnityEngine;

public class EnemyAITests
{
    private GameObject enemyObj;
    private EnemyAI ai;
    private GameObject playerObj;

    [SetUp]
    public void SetUp()
    {
        enemyObj = new GameObject("Enemy");
        ai = enemyObj.AddComponent<EnemyAI>();
        enemyObj.transform.position = Vector3.zero;
        enemyObj.transform.forward = Vector3.forward;

        playerObj = new GameObject("Player");
        playerObj.tag = "Player";
        playerObj.transform.position = new Vector3(0, 0, 5f);
        playerObj.AddComponent<CapsuleCollider>();

        ai.player = playerObj.transform;
    }

    [TearDown]
    public void TearDown()
    {
        if (enemyObj != null) Object.DestroyImmediate(enemyObj);
        if (playerObj != null) Object.DestroyImmediate(playerObj);
    }

    [Test]
    public void ProximityDetection_StationaryPlayer_NotDetected()
    {
        ai.behaviorType = EnemyAI.AIBehaviorType.Proximity;
        ai.proximityRange = 10f;
        playerObj.transform.position = new Vector3(0, 0, 8f);
        ai.SetPlayerMovingForTesting(false);

        // Stationary player should not trigger proximity audio detection
        Assert.IsFalse(ai.CheckDetection());
    }

    [Test]
    public void ProximityDetection_MovingPlayer_Detected()
    {
        ai.behaviorType = EnemyAI.AIBehaviorType.Proximity;
        ai.proximityRange = 10f;
        playerObj.transform.position = new Vector3(0, 0, 8f);
        ai.SetPlayerMovingForTesting(true);

        // Actively moving player triggers proximity detection
        Assert.IsTrue(ai.CheckDetection());
    }

    [Test]
    public void ProximityDetection_MovingPlayer_OutsideRange_NotDetected()
    {
        ai.behaviorType = EnemyAI.AIBehaviorType.Proximity;
        ai.proximityRange = 10f;
        playerObj.transform.position = new Vector3(0, 0, 15f);
        ai.SetPlayerMovingForTesting(true);

        // Moving player beyond proximity range should not be detected
        Assert.IsFalse(ai.CheckDetection());
    }

    [Test]
    public void ReturnToPatrol_WhenPlayerLeavesRange()
    {
        ai.behaviorType = EnemyAI.AIBehaviorType.Proximity;
        ai.proximityRange = 10f;
        ai.isAlerted = true;

        playerObj.transform.position = new Vector3(0, 0, 8f);
        Assert.IsTrue(ai.IsPlayerInDetectionRange());

        // Player moves outside detection range
        playerObj.transform.position = new Vector3(0, 0, 15f);
        Assert.IsFalse(ai.IsPlayerInDetectionRange());
    }

    [Test]
    public void VisionCone_DetectsInsideConeAndRange()
    {
        ai.behaviorType = EnemyAI.AIBehaviorType.VisionCone;
        ai.visionConeAngle = 90f;
        ai.visionConeRange = 15f;
        playerObj.transform.position = new Vector3(0, 0, 10f); // Directly ahead
        Physics.SyncTransforms();

        Assert.IsTrue(ai.CheckDetection());
    }

    [Test]
    public void VisionCone_FailsBehindEnemy()
    {
        ai.behaviorType = EnemyAI.AIBehaviorType.VisionCone;
        ai.visionConeAngle = 90f;
        ai.visionConeRange = 15f;
        playerObj.transform.position = new Vector3(0, 0, -5f); // Directly behind
        Physics.SyncTransforms();

        Assert.IsFalse(ai.CheckDetection());
    }

    [Test]
    public void VisionCone_FailsBeyondRange()
    {
        ai.behaviorType = EnemyAI.AIBehaviorType.VisionCone;
        ai.visionConeAngle = 90f;
        ai.visionConeRange = 10f;
        playerObj.transform.position = new Vector3(0, 0, 20f); // Ahead but beyond range
        Physics.SyncTransforms();

        Assert.IsFalse(ai.CheckDetection());
    }

    [Test]
    public void VisionCone_BlockedByWall()
    {
        ai.behaviorType = EnemyAI.AIBehaviorType.VisionCone;
        ai.visionConeAngle = 90f;
        ai.visionConeRange = 20f;
        playerObj.transform.position = new Vector3(0, 0, 10f);

        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position = new Vector3(0, 0.5f, 5f);
        wall.transform.localScale = new Vector3(10f, 5f, 1f);
        Physics.SyncTransforms();

        bool detected = ai.CheckDetection();
        Object.DestroyImmediate(wall);
        Physics.SyncTransforms();

        Assert.IsFalse(detected);
    }

    [Test]
    public void LineOfSight_DetectsClearPath()
    {
        ai.behaviorType = EnemyAI.AIBehaviorType.LineOfSight;
        ai.maxSightRange = 30f;
        playerObj.transform.position = new Vector3(0, 0, 10f);
        Physics.SyncTransforms();

        Assert.IsTrue(ai.CheckDetection());
    }

    [Test]
    public void LineOfSight_BlockedByWall()
    {
        ai.behaviorType = EnemyAI.AIBehaviorType.LineOfSight;
        ai.maxSightRange = 30f;
        playerObj.transform.position = new Vector3(0, 0, 10f);

        // Place a solid wall between enemy and player
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position = new Vector3(0, 0.5f, 5f);
        wall.transform.localScale = new Vector3(10f, 5f, 1f);
        Physics.SyncTransforms();

        bool detected = ai.CheckDetection();
        Object.DestroyImmediate(wall);
        Physics.SyncTransforms();

        Assert.IsFalse(detected);
    }

    [Test]
    public void LevelScaling_ProgressivelyIncreasesRanges()
    {
        // Level 1 scaling verification
        ai.ApplyLevelScaling(1);
        Assert.AreEqual(6f, ai.proximityRange, 0.01f);
        Assert.AreEqual(12f, ai.maxSightRange, 0.01f);
        Assert.AreEqual(6f, ai.minVisionConeRange, 0.01f);
        Assert.AreEqual(10f, ai.maxVisionConeRange, 0.01f);
        Assert.AreEqual(35f, ai.minVisionConeAngle, 0.01f);
        Assert.AreEqual(60f, ai.maxVisionConeAngle, 0.01f);

        // Level 6 scaling verification
        ai.ApplyLevelScaling(6);
        Assert.AreEqual(14f, ai.proximityRange, 0.01f);
        Assert.AreEqual(28f, ai.maxSightRange, 0.01f);
        Assert.AreEqual(16f, ai.minVisionConeRange, 0.01f);
        Assert.AreEqual(24f, ai.maxVisionConeRange, 0.01f);
        Assert.AreEqual(75f, ai.minVisionConeAngle, 0.01f);
        Assert.AreEqual(115f, ai.maxVisionConeAngle, 0.01f);
    }

    [Test]
    public void GizmoVisibility_CloseRangeAndObstacleOcclusion()
    {
        ai.gizmoCloseDistance = 8f;
        playerObj.transform.position = new Vector3(0, 0, 5f);
        Physics.SyncTransforms();

        // Close range (< 8m) should show gizmo
        Assert.IsTrue(ai.ShouldDrawGizmo());

        // Far distance (> 8m) with clear sightline should show gizmo
        playerObj.transform.position = new Vector3(0, 0, 15f);
        Physics.SyncTransforms();
        Assert.IsTrue(ai.ShouldDrawGizmo());

        // Far distance (> 8m) blocked by wall should hide gizmo
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position = new Vector3(0, 0.5f, 8f);
        wall.transform.localScale = new Vector3(10f, 5f, 1f);
        Physics.SyncTransforms();

        bool shouldDrawWithWall = ai.ShouldDrawGizmo();
        Object.DestroyImmediate(wall);
        Physics.SyncTransforms();

        Assert.IsFalse(shouldDrawWithWall);
    }
}
