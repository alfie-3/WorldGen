using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityContainer : MonoBehaviour
{
    [field: SerializeField] public EntityData EntityData {  get; private set; }
}
