using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

public class SimpleMovementTest
{
  private GameObject _playerObject;
  private Rigidbody _playerRigidbody;

  [SetUp]
  public void Setup()
  {
    _playerObject = new GameObject("TestPlayer");
    _playerRigidbody = _playerObject.AddComponent<Rigidbody>();
    _playerRigidbody.useGravity = false;
  }

  [TearDown]
  public void Teardown()
  {
    Object.DestroyImmediate(_playerObject);
  }

  [UnityTest]
  public IEnumerator Player_Moves_When_Force_Applied()
  {
    Vector3 startPosition = _playerObject.transform.position;
    _playerRigidbody.AddForce(Vector3.forward * 10f);
    yield return new WaitForSeconds(0.5f);
    Vector3 endPosition = _playerObject.transform.position;
    Assert.Greater(endPosition.z, startPosition.z, "Player should move forward");
  }

  [Test]
  public void Player_Has_Rigidbody()
  {
    Assert.IsNotNull(_playerRigidbody, "Player should have Rigidbody");
  }
}