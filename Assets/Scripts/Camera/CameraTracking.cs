using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraTracking : MonoBehaviour
{
    private void Start()
    {
        CameraController.playerTransform = transform;
    }
}
