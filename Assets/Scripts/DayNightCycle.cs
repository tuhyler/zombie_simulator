using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class DayNightCycle : MonoBehaviour
{
    [SerializeField]
    private Light directionalLight;

    //[SerializeField]
    //private DayNightPresetSO preset;

    public float speed = 0.1f;
    private bool day = true;
    
    [SerializeField, Range(0, 24)]
    private float timeODay;

    [SerializeField]
    private Gradient ambientColor, directionalColor, fogColor;
    [SerializeField]
    private AnimationCurve fogDensity;
    [SerializeField]
    private ParticleSystem globalFloaties;

    private void Update()
    {
        if (Application.isPlaying)
        {
            if (day)
                timeODay += Time.deltaTime * speed;
            else
                timeODay += Time.deltaTime * speed * 3; //night is 3 times as fast as day
            timeODay %= 24; //Clamp between 0-24
            UpdateLighting(timeODay * 0.041667f); //divide by 24
            //else
            //    SetFloatieCount(1f, Color.yellow);

            if (Mathf.RoundToInt(timeODay) == 18)
            {
                day = false;
                SetFloatieCount(2.5f, new Color(1f, 1f, 0.666f));
            }
            else if (Mathf.RoundToInt(timeODay) == 6)
            {
                day = true;
                SetFloatieCount(0.5f, Color.white);
            }
        }
        else
        {
            UpdateLighting(timeODay / 24f);
        }
    }

    private void UpdateLighting(float timePercent)
    {
        RenderSettings.ambientLight = ambientColor.Evaluate(timePercent);
        RenderSettings.fogColor = fogColor.Evaluate(timePercent);
        RenderSettings.fogDensity = fogDensity.Evaluate(timePercent);

        directionalLight.color = directionalColor.Evaluate(timePercent);
        //if (timeODay < 18.1)
        directionalLight.transform.localRotation = Quaternion.Euler(timePercent * 360 - 90f, 75, 0);
        //else
        //    directionalLight.transform.localRotation = Quaternion.Euler(-90f, 75, 0);
    }

    private void SetFloatieCount(float count, Color floatieColor)
    {
        var emission = globalFloaties.emission;
        emission.rateOverTime = count;

        var main = globalFloaties.main;
        main.startColor = floatieColor;
    }
    //public float time;
    //[HideInInspector] public float currenttime;
    //public Transform SunTransform;
    //public Light Sun;
    //public int days;

    //public float intensity;
    //public Color fogday = Color.gray;
    //public Color fognight = Color.black;

    ////private string pmAm;
    ////private string displayTime;
    //private int hours = 0;
    //private int minutes = 0;

    //[Tooltip("The Amount Of 24hour Periods in Each Day, Default is 4")]
    //public int timeMultiplier = 4;
    //[HideInInspector] public int speed;

    //private void Start()
    //{
    //    currenttime = time;
    //    speed = timeMultiplier * 24;
    //}

    //void Update()
    //{
    //    ChangeTime();
    //}

    //public void ChangeTime()
    //{
    //    time += Time.deltaTime * speed;

    //    setTime();

    //    float sunRiseSet_delay;

    //    if (hours < 12)
    //    {
    //        sunRiseSet_delay = 1.15f; // sun will rise about 7am
    //    }
    //    else
    //    {
    //        sunRiseSet_delay = 1.65f; // sun will set after 7pm
    //    }

    //    SunTransform.rotation = Quaternion.Euler(new Vector3((time - (21600 * sunRiseSet_delay)) / 86400 * 360, 30, 0));
    //    if (time > 43200)
    //    {
    //        intensity = 1 - (43200 - time) / 43200;
    //    }
    //    else
    //    {
    //        intensity = 1 - ((43200 - time) / 43200 * -1);
    //    }

    //    RenderSettings.fogColor = Color.Lerp(fognight, fogday, intensity * intensity);

    //    Sun.intensity = intensity;
    //}

    //private void setTime()
    //{
    //    //if (hours >= 24)
    //    //{
    //    //    days += 1;
    //    //    time = 0;
    //    //    currenttime = 0;
    //    //    hours = 0;
    //    //}

    //    if ((time - currenttime) >= 60) // every 60 seconds passed, minute will increment by 1
    //    {
    //        currenttime = time;

    //        if (minutes < 59)
    //        {
    //            minutes++;
    //        }
    //        else // every 60 minutes hour will increment by 1
    //        {
    //            minutes = 0;
    //            hours++;
    //        }
    //    }

    //    //if (hours < 12 && minutes < 60) // dispolay time as AM 
    //    //{
    //    //    pmAm = " am";
    //    //    displayTime = hours.ToString("00") + ":" + minutes.ToString("00") + pmAm;
    //    //}
    //    //else // display time as PM
    //    //{
    //    //    pmAm = " pm";
    //    //    if (hours == 12) // This makes midday 12pm instead of 00pm
    //    //    {
    //    //        displayTime = hours.ToString("00") + ":" + minutes.ToString("00") + pmAm;
    //    //    }
    //    //    else // this converts PM into 12hr time while maintaing 24hr time in the background
    //    //    {
    //    //        displayTime = (hours - 12).ToString("00") + ":" + minutes.ToString("00") + pmAm;
    //    //    }
    //    //}
    //}



}
