using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
   [SerializeField] private float moveSpeed;
   [SerializeField] private float jumpForce;
   [SerializeField] private float moveSensitivity;
   [SerializeField] private Transform playerCamera;

   private PlayerInput _playerInput;
   private Rigidbody _rb;
   private Vector2 _moveInput;
   private bool _isGround;
   private Vector2 _lookInput;
   private float _xRotation = 0f;
   
   
   private void Start()
   {
      _playerInput = GetComponent<PlayerInput>();
      _rb = GetComponent<Rigidbody>();
      Cursor.lockState = CursorLockMode.Locked;
   }

   private void Update()
   {
      MovePlayer();
      if (_playerInput.actions["Jump"].triggered && _isGround)
      {
         Jump();
      }
   }
   private void LateUpdate()
   {
      _lookInput = _playerInput.actions["Look"].ReadValue<Vector2>();

      float mouseX = _lookInput.x * moveSensitivity;
      float mouseY = _lookInput.y * moveSensitivity;

      _xRotation -= mouseY;
      _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
      playerCamera.localRotation = Quaternion.Euler(_xRotation, 0,0);
      transform.Rotate(Vector3.up * mouseX);
   }

   private void MovePlayer()
   {
      _moveInput = _playerInput.actions["Move"].ReadValue<Vector2>();
      Vector3 movement = new Vector3(_moveInput.x, 0, _moveInput.y) * moveSpeed * Time.deltaTime;
      transform.Translate(movement);
   }

   private void Jump()
   {
      _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
   }

   private void OnCollisionEnter(Collision other)
   {
      if (other.gameObject.CompareTag("Ground"))
      {
         _isGround = true;
      }
   }

   private void OnCollisionExit(Collision other)
   {
      if (other.gameObject.CompareTag("Ground"))
      {
         _isGround = false;
      }
   }
}
