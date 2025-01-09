using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldCurveManager : MonoBehaviour
{
    [SerializeField] float worldCurveAmount = 0.0004f;

    private void Awake()
    {
        Shader.SetGlobalFloat("_WorldCurveMultiplier", worldCurveAmount);
    }

    private void OnValidate()
    {
        Shader.SetGlobalFloat("_WorldCurveMultiplier", worldCurveAmount);
    }
}
