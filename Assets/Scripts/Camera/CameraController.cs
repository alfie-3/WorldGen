using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] float camMoveSpeed = 5;
    [SerializeField] float moveSpeedMult = 1.5f;

    // Update is called once per frame
    void Update()
    {
        Vector2 inputVector = new(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        Vector3 moveVector = new(inputVector.x, 0, inputVector.y);
        moveVector.Normalize();

        float mult = Input.GetKeyDown(KeyCode.LeftShift) ? moveSpeedMult : 1;

        transform.Translate(camMoveSpeed * moveSpeedMult * Time.deltaTime * mult * moveVector);
    }
}
