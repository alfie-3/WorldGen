using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="New Static Entity", menuName = "Entities/Static")]
public class StaticEntity : EntityData
{
    [field: SerializeField] public GameObject EntityPrefab { get; private set; }
}
