using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] float camMoveSpeed = 5;

    // Update is called once per frame
    void Update()
    {
        Vector2 inputVector = new(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        Vector3 moveVector = new(inputVector.x, 0, inputVector.y);
        transform.Translate(camMoveSpeed * Time.deltaTime * moveVector);
    }
}
