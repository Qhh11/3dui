using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private float playerSpeed = 30.0f;

    private CharacterController controller;
    private Vector3 playerVelocity;
    
    private InputManager inputManager;
    
    private Transform cameraTransform;

    private void Start()
    {
        inputManager = InputManager.Instance;
        controller = GetComponent<CharacterController>();
        cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        // Gravity
        if (controller.isGrounded)
        {
            playerVelocity.y = -0.5f; // 小力压住地面,防止浮起
        }
        else
        {
            playerVelocity.y += Physics.gravity.y * Time.deltaTime;
        }
        // Read input
        Vector2 movment = inputManager.GetLeftStick();
        Vector3 move = new Vector3(movment.x, 0, movment.y);
        move = cameraTransform.forward * move.z + cameraTransform.right * move.x;
        move.y = 0;
        move = Vector3.ClampMagnitude(move, 1f);

        // if (move != Vector3.zero)
        //     transform.forward = move;

        // Move
        Vector3 finalMove = move * playerSpeed + Vector3.up * playerVelocity.y;
        controller.Move(finalMove * Time.deltaTime);
    }
}

