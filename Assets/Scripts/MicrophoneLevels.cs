using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class MicrophoneLevels : MonoBehaviour
{
    public string selectedMic;
    public int sampleDataLength = 1024;

    AudioSource micSource;
    float clipLoudness;
    float[] clipSampleData;

    private void Start()
    {
        micSource = GetComponent<AudioSource>();

        clipSampleData = new float[sampleDataLength];

        if (Microphone.devices.Length > 0)
        {
            selectedMic = Microphone.devices[0].ToString();
            micSource.clip = Microphone.Start(selectedMic, true, 10, AudioSettings.outputSampleRate);
        }
        else
        {
            Debug.Log("[ERROR] No mic detected");
        }
    }

    private void Update()
    {
        micSource.clip.GetData(clipSampleData, micSource.timeSamples);

        foreach (var sample in clipSampleData)
        {
            clipLoudness += Mathf.Abs(sample);
        }

        clipLoudness /= sampleDataLength;

        Debug.Log(clipLoudness);
    }
}
