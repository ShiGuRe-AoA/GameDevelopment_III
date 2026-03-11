using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct ComplexTime
{
    public ComplexTime(Season season, int date, int hour, int minute)
    {
        Season = season;
        Date = date;
        Hour = hour;
        Minute = minute;
    }

    public Season Season;
    public int Date;
    public int Hour;
    public int Minute;
}
public enum Season
{
    Spring, Summer, Autumn, Winter
}
public class TimeManager : MonoBehaviour
{
    [Header("初始设置，后续从SO表读入")]
    [SerializeField] private int DateOfSeason = 28;
    [SerializeField] private int dayBeginHour;
    [SerializeField] private int dayEndHour;
    [SerializeField] private float minuteTransferRate;

    private int dayCount;
    private int minuteCount;
    private float realTimeCount;

    public bool IsPause { get; private set; }
    private float nextMintuteTick => (minuteCount + 1) * minuteTransferRate;

    private Season season;

    private int _date;
    private int date
    {
        get {  return _date; }
        set
        {
            if(value > DateOfSeason)
            {
                _date = 1;
                season++;
            }
            else
            {
                _date = value;
            }
        }
            
    }
    private int _hour;
    private int hour
    {
        get { return _hour; }
        set
        {
            if(value >= dayEndHour)
            {
                NextDay();
            }
            else
            {
                _hour = value;
            }
        }
    }
    private int _minute;
    private int minute
    {
        get { return _minute; }
        set
        {
            if(minute >= 60)
            {
                hour++;
            }
            else
            {
                minute = value;
            }
        }
    }


    private void Awake()
    {
        Init();
    }
    private void Update()
    {
        realTimeCount += Time.deltaTime;
        if(realTimeCount >= nextMintuteTick)
        {
            minuteCount++;
            minute++;
        }
    }


    public void Init()
    {
        dayCount = 0;
    }
    public void NextDay()
    {
        dayCount++;
        hour = dayBeginHour;
        minute = 0;
        realTimeCount = 0;
        //TODO: 存档
    }
    public int TransferTimeR2M(float realTime)
    {
        return (int)(realTime / minuteTransferRate);
    }
    public float TransferTimeM2R(int minute)
    {
        return minute * minuteTransferRate;
    }
    public ComplexTime GetComplexTime()
    {
        ComplexTime output = new ComplexTime(season,date,hour,minute);
        return output;
    }
    public float GetRealTime()
    {
        return realTimeCount;
    }
    public int GetMinuteCount()
    {
        return minuteCount;
    }
    public void PauseTime()
    {
        IsPause = true;
        Time.timeScale = 0f;
    }
    public void StartTime()
    {
        IsPause = false;
        Time.timeScale = 1f;
    }
}
