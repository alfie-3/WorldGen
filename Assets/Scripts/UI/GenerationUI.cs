using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GenerationUI : MonoBehaviour
{
    [SerializeField] TMP_InputField inputField;
    [SerializeField] GameObject canvas;

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            canvas.SetActive(!canvas.activeInHierarchy);
        }
    }

    public void Generate()
    {
        if (inputField.text == string.Empty) return;

        int seed;

        try
        {
            seed = Int32.Parse(inputField.text);
        }
        catch
        {
            Debug.LogWarning("Could not parse string as int");
            return;
        }

        SO_FastNoiseLiteGenerator.SetSeed(seed);
        WorldGenerationEvents.Generate();
        canvas.SetActive(false);
    }
}
