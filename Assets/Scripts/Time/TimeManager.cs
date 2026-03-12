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
    Spring,
    Summer,
    Autumn,
    Winter
}

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("初始设置，后续从SO表读入")]
    [SerializeField] private int DateOfSeason = 28;
    [SerializeField] private int dayBeginHour;
    [SerializeField] private int dayEndHour;
    [SerializeField] private float minuteTransferRate = 1f;

    private int dayCount;
    private int minuteCount;
    private float realTimeCount;

    public bool IsPause { get; private set; }

    private float nextMintuteTick => (minuteCount + 1) * minuteTransferRate;

    private Season season;

    private int _date;
    private int date
    {
        get { return _date; }
        set
        {
            if (value > DateOfSeason)
            {
                _date = 1;
                NextSeason();
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
            if (value >= dayEndHour)
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
            if (value >= 60)
            {
                _minute = 0;
                hour++;
            }
            else
            {
                _minute = value;
            }
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // 如果你希望切场景后也保留，再打开这一行
        // DontDestroyOnLoad(gameObject);

        Init();
    }

    private void Update()
    {
        if (IsPause) return;

        realTimeCount += Time.deltaTime;

        if (realTimeCount >= nextMintuteTick)
        {
            minuteCount++;
            minute++;
        }
    }

    public void Init()
    {
        dayCount = 0;
        minuteCount = 0;
        realTimeCount = 0f;

        season = Season.Spring;
        date = 1;
        hour = dayBeginHour;
        minute = 0;
    }

    public void NextDay()
    {
        dayCount++;
        date++;
        _hour = dayBeginHour;
        _minute = 0;
        realTimeCount = 0f;
        // TODO: 存档
    }

    private void NextSeason()
    {
        if (season == Season.Winter)
        {
            season = Season.Spring;
        }
        else
        {
            season = (Season)((int)season + 1);
        }
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
        return new ComplexTime(season, date, hour, minute);
    }

    public float GetRealTime()
    {
        return realTimeCount;
    }

    public int GetMinuteCount()
    {
        return minuteCount;
    }

    public void PauseGame()
    {
        IsPause = true;
        Time.timeScale = 0f;
    }

    public void StartGame()
    {
        IsPause = false;
        Time.timeScale = 1f;
    }
}