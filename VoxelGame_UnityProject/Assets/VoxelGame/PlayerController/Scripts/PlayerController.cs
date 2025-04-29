using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using VoxelGame.Voxel;
using VoxelGame.Input;
using VoxelGame.Core;
using Zenject;

namespace VoxelGame.Player
{
    [RequireComponent(typeof(CharacterBoxController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Move")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float rotationSmoothTime = 0.15f;
        [SerializeField] private float rotationSensetivity = 1f;
        [SerializeField] private float speedChangeRate = 10f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -15f;
        [SerializeField] private float jumpTimeout = 0.5f;
        [SerializeField] private float fallTimeout = 0.15f;
        
        [Header("Camera")]
        [SerializeField] private Transform cameraTarget;
        [SerializeField] private float cameraFov = 70f;
        [SerializeField] private float cameraFovSprint = 80f;
        [SerializeField] private float topClamp = 85f;
        [SerializeField] private float bottomClamp = -85f;
        [SerializeField] private float cameraAngle = 0f;
        [SerializeField] private bool lockCameraPosition = false;
        [SerializeField] private float cameraWalkOffsetMultiplier = 1f;
        [SerializeField] private Camera Camera;

        [Header("Footsteps")]
        [SerializeField] private RandomSound rndSoundFootsteps;

        [Header("Actions")]

        // camera target walk offset
        private float cameraAmplitudeGain;

        // cinemachine
        private float targetYaw;
        private float targetPitch;

        // player
        private float speed;
        private float targetSpeed;
        private float targetRotation = 0.0f;
        private float rotationVelocity;
        private float verticalVelocity;
        private float terminalVelocity = 53.0f;
        private bool runMode;

        // timeout deltatime
        private float jumpTimeoutDelta;
        private float fallTimeoutDelta;

        private const float threshold = 0.01f;

        private CharacterBoxController controller;
        private Transform mainCamera;
        private PlayerAction action;
        private HandController handController;

        [Inject]
        private readonly Inputs input;

        private void Start()
        {
            if (mainCamera == null)
            {
                mainCamera = GameObject.FindGameObjectWithTag("MainCamera").transform;

                if (Camera == null)
                {
                    Camera = mainCamera.GetComponent<Camera>();
                }
            }
            
            handController = Camera.GetComponentInChildren<HandController>();
            handController.enabled = true;

            if (Camera.transform != cameraTarget)
            {
                Camera.transform.parent = cameraTarget;
                Camera.transform.localPosition = Vector3.zero;
                Camera.transform.localRotation = Quaternion.identity;
            }

            targetYaw = cameraTarget.rotation.eulerAngles.y;
            controller = GetComponent<CharacterBoxController>();
            action = GetComponent<PlayerAction>();
            action.SetCamera(mainCamera);
            action.SetHand(handController);

            jumpTimeoutDelta = jumpTimeout;
            fallTimeoutDelta = fallTimeout;

            input.PlayerInputs.Fire.OnStarted.AddListener(() =>
            {
                if (!input.IsPointerOverUI)
                {
                    action.Dig();
                }
            });

            input.PlayerInputs.FireAlt.OnStarted.AddListener(() =>
            {
                if (!input.IsPointerOverUI)
                {
                    action.Build();
                }
            });
        }

        private void Update()
        {
            JumpAndGravity();
            Move();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void JumpAndGravity()
        {
            if (controller.IsGrounded)
            {
                fallTimeoutDelta = fallTimeout;

                if (verticalVelocity < 0.0f)
                {
                    verticalVelocity = -2f;
                }

                // Jump
                if (input.PlayerInputs.Jump.IsPressed && jumpTimeoutDelta <= 0.0f)
                {
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    
                    handController.HandOffset = Vector2.SmoothDamp(
                        handController.HandOffset,
                        handController.HandOffset - Vector2.up*0.1f, ref handVel,
                        Time.deltaTime * 5f);
                }

                // jump timeout
                if (jumpTimeoutDelta >= 0.0f)
                {
                    jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                jumpTimeoutDelta = jumpTimeout;

                if (fallTimeoutDelta >= 0.0f)
                {
                    fallTimeoutDelta -= Time.deltaTime;
                }
            }

            if (verticalVelocity < terminalVelocity)
            {
                verticalVelocity += gravity * Time.deltaTime;
            }
        }

        private void Move()
        {
            Vector2 moveInput = input.PlayerInputs.Move.Value;
            float inputMagnitude = moveInput.magnitude;

            if (inputMagnitude > 0.5f && input.PlayerInputs.Run.IsPressed && controller.IsGrounded)
                runMode = true;

            if (inputMagnitude < 0.25f || speed + speed < moveSpeed)
                runMode = false;

            Camera.fieldOfView = Mathf.Lerp(Camera.fieldOfView, runMode ? cameraFovSprint : cameraFov, Time.deltaTime * 2f);
            float inputSpeed = 0.0f;

            if (moveInput != Vector2.zero)
            {
                inputSpeed = runMode ? sprintSpeed : moveSpeed;
            }

            const float speedOffset = 0.1f;

            if (targetSpeed < inputSpeed - speedOffset ||
                targetSpeed > inputSpeed + speedOffset)
            {
                targetSpeed = Mathf.Lerp(targetSpeed, inputSpeed * inputMagnitude,
                    Time.deltaTime * speedChangeRate);

                targetSpeed = Mathf.Round(targetSpeed * 1000f) / 1000f;
            }
            else
            {
                targetSpeed = inputSpeed;
            }

            Vector3 inputDirection = new Vector3(moveInput.x, 0.0f, moveInput.y).normalized;

            if (moveInput != Vector2.zero)
            {
                targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref rotationVelocity,
                    rotationSmoothTime);

                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            Vector3 targetDirection = Quaternion.Euler(0.0f, targetRotation, 0.0f) * Vector3.forward;

            controller.Move(targetDirection.normalized * (targetSpeed * Time.deltaTime) +
                             new Vector3(0.0f, verticalVelocity * Time.deltaTime, 0.0f));

            // cancel jump if block above
            if (controller.HitHead)
            {
                verticalVelocity = Mathf.Min(verticalVelocity, 0f);
            }

            speed = Magnitude(controller.VelocityUnscaled.x, controller.VelocityUnscaled.z) / Time.deltaTime;
            
            if (speed > 0.5f && controller.IsGrounded)
            {
                rndSoundFootsteps.PlayRandomSound(2.75f / speed);
                handController.HandSwaySpeed = speed / 10f;
            }
        }

        private float Magnitude(float x, float y)
        {
            return Mathf.Sqrt(x * x + y * y);
        }

        private Vector2 handVel;
        
        private void CameraRotation()
        {
            Vector2 lookInput = input.PlayerInputs.Look.Value;

            float targetYawDelta = 0;
            float targetPitchDelta = 0;

            if (lookInput.sqrMagnitude >= threshold && !lockCameraPosition)
            {
                float deltaTimeMultiplier = input.IsCurrentPointerMouse || input.IsCurrentPointerTouchpad ? 1.0f : Time.deltaTime * 180f;

                targetYawDelta = lookInput.x * deltaTimeMultiplier * rotationSensetivity;
                targetPitchDelta = lookInput.y * deltaTimeMultiplier * rotationSensetivity;
                
                targetYaw += targetYawDelta;
                targetPitch += targetPitchDelta;
            }

            handController.HandOffset = Vector2.SmoothDamp(
                handController.HandOffset,
                new Vector2(-targetYawDelta, targetPitchDelta) * cameraWalkOffsetMultiplier, ref handVel,
                Time.deltaTime * 5f);

            targetYaw = ClampAngle(targetYaw, float.MinValue, float.MaxValue);
            targetPitch = ClampAngle(targetPitch, bottomClamp, topClamp);

            cameraTarget.rotation = Quaternion.Euler(
                targetPitch + cameraAngle,
                targetYaw,
                0.0f);
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }
    }
}