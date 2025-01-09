using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoundsAdjuster : MonoBehaviour
{
    [SerializeField] Vector3 newBoundsSize = Vector3.zero;

    // Start is called before the first frame update
    void Start()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        Bounds bounds = new Bounds(transform.position, newBoundsSize);

        meshRenderer.bounds = bounds;
    }
}
