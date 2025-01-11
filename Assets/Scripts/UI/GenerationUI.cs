using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
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

        using var algo = SHA1.Create();
        var hash = BitConverter.ToInt32(algo.ComputeHash(Encoding.UTF8.GetBytes(inputField.text)));
        System.Random rand = new System.Random(hash);
        seed = rand.Next();

        SO_FastNoiseLiteGenerator.SetSeed(seed);

        WorldGenerationEvents.Generate();
        canvas.SetActive(false);
    }
}
