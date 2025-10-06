using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

public class PlayerController : MonoBehaviour
{ 
    [SerializeField] private Transform playerCamera;
    [SerializeField] private ParticleSystem jetpackParticles;
    [SerializeField] private AudioSource jetpackAudio;

    private Star.StarFactory starFactory;
    
    private FloatData _floatData;
    private PlayerInput _playerInput;
    private Rigidbody _rb;
    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private float _xRotation = 0f;
    
    private bool _isBoosting = false;
    private Vector3 _thrustDirection;
    public event Action OnTakeStar;

    [Inject]
    public void Construct(FloatData floatData)
    {
        _floatData = floatData;
    }
    
    private void Start()
    {
        _playerInput = GetComponent<PlayerInput>();
        _rb = GetComponent<Rigidbody>();
        
        _rb.linearDamping = 0.05f;
        _rb.angularDamping = 0.3f;
        _rb.useGravity = false;
        
        Cursor.lockState = CursorLockMode.Locked;
        _rb.linearVelocity = Vector3.forward * _floatData.startForce;
    }

    private void OnDestroy()
    {
        Cursor.lockState = CursorLockMode.None;
    }

    private void Update()
    {
        HandleJetpackThrust();
        //HandleCameraRotation();
        HandleStabilization();
        UpdateJetpackEffects();

        if (_playerInput.actions["Attack"].triggered)
        {
            OnTakeStar?.Invoke();
        }
    }
    
    

    private void HandleJetpackThrust()
    {
        _moveInput = _playerInput.actions["Move"].ReadValue<Vector2>();
        _isBoosting = _playerInput.actions["Jump"].IsPressed();


        Vector3 cameraForward = playerCamera.forward;
        Vector3 cameraRight = playerCamera.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        _thrustDirection = (cameraForward * _moveInput.y + cameraRight * _moveInput.x);

        float verticalThrust = 0f;
        if (_playerInput.actions["Jump"].IsPressed())
            verticalThrust = 1f;
        else if (_playerInput.actions["Crouch"].IsPressed())
            verticalThrust = -1f;

        _thrustDirection.y += verticalThrust;

        float currentThrust = _isBoosting ? _floatData.thrustForce * _floatData.boostMultiplier : _floatData.thrustForce;
        if (_thrustDirection.magnitude > 0.1f)
        {
            _rb.AddForce(_thrustDirection * currentThrust);
        }
    }

    private void HandleCameraRotation()
    {
        playerCamera.localRotation = Quaternion.Euler(_xRotation, 0, 0);
        
        HandleBodyTilt();
    }

    private void HandleBodyTilt()
    {
        if (_moveInput.x != 0)
        {
            float targetRoll = -_moveInput.x * 10f; 
            Quaternion targetTilt = Quaternion.Euler(0, transform.eulerAngles.y, targetRoll);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetTilt, 2f * Time.deltaTime);
        }
        else
        {
            Quaternion upright = Quaternion.Euler(0, transform.eulerAngles.y, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, upright, 3f * Time.deltaTime);
        }
    }

    private void HandleStabilization()
    {
        if (_lookInput.magnitude < 0.1f)
        {
            Vector3 currentAngularVelocity = _rb.angularVelocity;
            currentAngularVelocity = Vector3.Lerp(currentAngularVelocity, Vector3.zero, _floatData.stabilizerForce * Time.deltaTime);
            _rb.angularVelocity = currentAngularVelocity;
        }
        
        if (_thrustDirection.magnitude < 0.1f && _rb.linearVelocity.magnitude < 0.5f)
        {
            _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, Vector3.zero, _floatData.stabilizerForce * Time.deltaTime);
        }
    }

    private void UpdateJetpackEffects()
    {
        if (jetpackParticles != null)
        {
            var emission = jetpackParticles.emission;
            bool shouldEmit = _thrustDirection.magnitude > 0.1f;
            emission.enabled = shouldEmit;
            
            if (shouldEmit)
            {
                var main = jetpackParticles.main;
                main.startSpeed = _isBoosting ? 8f : 4f;
                main.startLifetime = _isBoosting ? 1f : 0.5f;
            }
        }
        
        if (jetpackAudio != null)
        {
            if (_thrustDirection.magnitude > 0.1f)
            {
                if (!jetpackAudio.isPlaying)
                    jetpackAudio.Play();
                    
                jetpackAudio.volume = Mathf.Lerp(jetpackAudio.volume, _isBoosting ? 1f : 0.6f, 5f * Time.deltaTime);
                jetpackAudio.pitch = Mathf.Lerp(jetpackAudio.pitch, _isBoosting ? 1.3f : 1f, 5f * Time.deltaTime);
            }
            else
            {
                jetpackAudio.volume = Mathf.Lerp(jetpackAudio.volume, 0f, 5f * Time.deltaTime);
                if (jetpackAudio.volume < 0.1f)
                    jetpackAudio.Stop();
            }
        }
    }
}