using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyboxRotationController : MonoBehaviour
{
    [SerializeField] float rotationSpeed = 0.01f;

    private void Update()
    {
        RenderSettings.skybox.SetFloat("_Rotation", Time.time * rotationSpeed); 
    }
}
