using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterPlane : MonoBehaviour
{
    Transform CamTrasform;

    private void Awake()
    {
        CamTrasform = Camera.main.transform;
    }

    private void Update()
    {
        transform.position = new(CamTrasform.position.x, transform.position.y, CamTrasform.position.z);
    }
}
