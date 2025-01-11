using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class WorldGenerationEvents
{
    public static bool IsGenerating = false;

    public static void Generate()
    {
        if (IsGenerating)
        {
            Regenerate.Invoke();
            GC.Collect();
        }
        else
        {
            BeginGeneration.Invoke();
            IsGenerating = true;
        }
    }

    public static Action BeginGeneration = delegate { };
    public static Action Regenerate = delegate { };
}
