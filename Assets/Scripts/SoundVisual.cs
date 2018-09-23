using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoundVisual : MonoBehaviour
{
    const int SAMPLE_SIZE = 1024;

    public bool useMic;
    public AudioClip nonMicClip;
    public float soundValue = 0;
    public float highestRMS = 0;
    public float highestDb = 0;
    public float highestPitch = 0;
    public float visualModifier = 50;
    public float smoothSpeed = 10;
    public float maxVisualScale = 25;
    public float keepPercent = 0.5f;
    public float radius = 5f;

    public float backgroundIntensity;
    public float dbScale;
    public Material backgroundMaterial;
    public Color minColour;
    public Color maxColour;

    public float rmsValue;
    public float dbValue;
    public float pitchValue;


    AudioSource source;
    float[] samples;
    float[] spectrum;
    float sampleRate;

    string selectedMic;

    private void Start()
    {
        source = GetComponent<AudioSource>();
        samples = new float[SAMPLE_SIZE];
        spectrum = new float[SAMPLE_SIZE];
        sampleRate = AudioSettings.outputSampleRate;

        if (useMic)
        {
            if (Microphone.devices.Length > 0)
            {
                selectedMic = Microphone.devices[0].ToString();
                source.clip = Microphone.Start(selectedMic, true, 10, AudioSettings.outputSampleRate);
                while (!(Microphone.GetPosition(null) > 0)) { }
                source.Play();
            }
            else
            {
                Debug.Log("[ERROR] No mic detected");
            }
        }
        else
        {
            source.clip = nonMicClip;
            source.Play();
        }

        changeDanceCountdown = countdownTime;

        // SpawnLine();
        SpawnCircle();
    }

    public float countdownTime = 20;
    float changeDanceCountdown;
    public Image titlePanel;
    public Text titleText;
    private void Update()
    {
        if (titlePanel.color.a <= 0)
        {
            titlePanel.gameObject.SetActive(false);
            titleText.gameObject.SetActive(false);
        }
        else
        {
            titlePanel.color = new Color(titlePanel.color.r, titlePanel.color.g, titlePanel.color.b, titlePanel.color.a - Time.deltaTime / 2);
            titleText.color = new Color(titleText.color.r, titleText.color.g, titleText.color.b, titleText.color.a - Time.deltaTime / 2);
        }

        if (Input.GetKeyDown("escape"))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
      Application.Quit();
#endif
        }

        AnalyseSound();
        UpdateVisuals();
        UpdateBackground();

        if (highestRMS < rmsValue)
        {
            highestRMS = rmsValue;
        }

        if (highestDb < dbValue)
        {
            highestDb = dbValue;
        }

        if (highestPitch < pitchValue)
        {
            highestPitch = pitchValue;
        }

        float normRMS = rmsValue / 0.625f;

        float normDB = 0;
        if (dbValue > 0)
        {
            normDB = dbValue / 16f;
        }
        else if (dbValue < 0)
        {
            normDB = (dbValue / 54f) + 1;
        }

        float normPitch;
        if (pitchValue < 1500)
        {
            normPitch = pitchValue / 1500;
        }
        else
        {
            normPitch = pitchValue / highestPitch;
        }

        float currentSV = (normDB + normPitch) / 2;

        //soundValue = currentSV;

        if (currentSV > soundValue && currentSV > 0)
        {
            if (currentSV > 1)
            {
                soundValue = 1;
            }
            else
            {
                soundValue = currentSV;
            }
        }

        if (soundValue > 0)
        {
            soundValue -= Time.deltaTime / 5f;
        }

        changeDanceCountdown -= Time.deltaTime;
        if (changeDanceCountdown < 0)
        {
            fDancingController.runtimeAnimatorController = controllers[Random.Range(0, controllers.Length - 1)];
            mDancingController.runtimeAnimatorController = controllers[Random.Range(0, controllers.Length - 1)];
            changeDanceCountdown = countdownTime;
        }

        UpdateAnim(soundValue);
    }

    void AnalyseSound()
    {
        source.GetOutputData(samples, 0);

        // RMS Value
        int i = 0;
        float sum = 0;
        for (; i < SAMPLE_SIZE; i++)
        {
            sum += samples[i] * samples[i];
        }
        rmsValue = Mathf.Sqrt(sum / SAMPLE_SIZE);

        // dB Value
        dbValue = 20 * Mathf.Log10(rmsValue / 0.1f);

        // Sound Spectrum
        source.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        // Pitch Value
        float maxV = 0;
        var maxN = 0;
        for (i = 0; i < SAMPLE_SIZE; i++)
        {
            if (!(spectrum[i] > maxV) || !(spectrum[i] > 0.0f))
            {
                continue;
            }

            maxV = spectrum[i];
            maxN = i;
        }

        float freqN = maxN;
        if (maxN > 0 && maxN < SAMPLE_SIZE - 1)
        {
            var dL = spectrum[maxN - 1] / spectrum[maxN];
            var dR = spectrum[maxN + 1] / spectrum[maxN];
            freqN += 0.5f * (dR * dR - dL * dL);
        }
        pitchValue = freqN * (sampleRate / 2) / SAMPLE_SIZE;
    }



    // Cube Visualisation
    public Transform[] visualiseList;
    float[] visualScale;
    int amnVisual = 64;

    void SpawnLine()
    {
        visualScale = new float[amnVisual];
        visualiseList = new Transform[amnVisual];

        for (int i = 0; i < amnVisual; i++)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube) as GameObject;
            visualiseList[i] = go.transform;
            visualiseList[i].position = Vector3.right * i;
        }
    }

    void SpawnCircle()
    {
        visualScale = new float[amnVisual];
        visualiseList = new Transform[amnVisual];
        Vector3 centre = Vector3.zero;

        for (int i = 0; i < amnVisual; i++)
        {
            float ang = i * 1.0f / amnVisual;
            ang = ang * Mathf.PI * 2;

            float x = centre.x + Mathf.Cos(ang) * radius;
            float y = centre.y + Mathf.Sin(ang) * radius;

            Vector3 position = new Vector3(x, y, 4);
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube) as GameObject;
            go.transform.position = position;
            go.transform.rotation = Quaternion.LookRotation(Vector3.forward, position);
            visualiseList[i] = go.transform;
        }
    }

    void UpdateVisuals()
    {
        int visualIndex = 0;
        int spectrumIndex = 0;
        int averageSize = (int)((SAMPLE_SIZE * keepPercent) / amnVisual);

        while (visualIndex < amnVisual)
        {
            int j = 0;
            float sum = 0;
            while (j < averageSize)
            {
                sum += spectrum[spectrumIndex];
                spectrumIndex++;
                j++;
            }

            float scaleY = sum / averageSize * visualModifier;
            visualScale[visualIndex] -= Time.deltaTime * smoothSpeed;
            if (visualScale[visualIndex] < scaleY)
            {
                visualScale[visualIndex] = scaleY;
            }

            if (visualScale[visualIndex] > maxVisualScale)
            {
                visualScale[visualIndex] = maxVisualScale;
            }

            visualiseList[visualIndex].localScale = Vector3.one + Vector3.up * visualScale[visualIndex];
            visualIndex++;
        }
    }

    void UpdateBackground()
    {
        backgroundIntensity -= Time.deltaTime * 5;
        if (backgroundIntensity < (dbValue / dbScale)) // max
        {
            backgroundIntensity = dbValue / dbScale;
        }

        backgroundMaterial.color = Color.Lerp(maxColour, minColour, -backgroundIntensity);
    }


    public Animator fDancingController;
    public Animator mDancingController;
    public RuntimeAnimatorController[] controllers;
    void UpdateAnim(float value)
    {
        if (fActive)
        {
            fDancingController.SetFloat("DanceValue", value);
        }
        else if (mActive)
        {
            mDancingController.SetFloat("DanceValue", value);
        }
    }


    public GameObject fGO;
    public GameObject mGO;
    bool fActive = true;
    bool mActive = false;
    public void OnFButtonDown()
    {
        fGO.SetActive(true);
        fActive = true;

        mGO.SetActive(false);
        mActive = false;
    }
    public void OnMButtonDown()
    {
        mGO.SetActive(true);
        mActive = true;

        fGO.SetActive(false);
        fActive = false;
    }


}
