using UnityEngine;

public enum Season
{
    Spring,
    Summer,
    Autumn,
    Winter
}

[System.Serializable]
public class ComplexTime
{
    public Season Season;
    public int Date;
    public int Hour;
    public int Minute;

    public ComplexTime()
    {
    }

    public ComplexTime(Season season, int date, int hour, int minute)
    {
        Season = season;
        Date = date;
        Hour = hour;
        Minute = minute;
    }

    public ComplexTime Copy()
    {
        return new ComplexTime(Season, Date, Hour, Minute);
    }

    public void AddMinute(int dayEndHour, int dayBeginHour, int dateOfSeason)
    {
        Minute++;

        if (Minute >= 60)
        {
            Minute = 0;
            Hour++;
        }

        if (Hour >= dayEndHour)
        {
            NextDay(dayBeginHour, dateOfSeason);
        }
    }

    public void NextDay(int dayBeginHour, int dateOfSeason)
    {
        Date++;
        Hour = dayBeginHour;
        Minute = 0;

        if (Date > dateOfSeason)
        {
            Date = 1;
            NextSeason();
        }
    }

    private void NextSeason()
    {
        Season = Season == Season.Winter
            ? Season.Spring
            : Season + 1;
    }
}
public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("初始设置，后续从SO表读入")]
    [SerializeField] private int DateOfSeason = 28;
    [SerializeField] public int dayBeginHour { get; private set; }
    [SerializeField] private int dayEndHour = 24;
    [SerializeField] private float minuteTransferRate = 1f;

    private ComplexTime currentTime;

    private int dayCount;
    private int minuteCount;
    private float realTimeCount;

    public bool IsPause { get; private set; }

    private float nextMinuteTick => (minuteCount + 1) * minuteTransferRate;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Init();
    }

    private void Update()
    {
        if (IsPause) return;

        float deltaTime = Time.deltaTime;

        realTimeCount += deltaTime;
        TickUpdate(deltaTime);

        if (realTimeCount >= nextMinuteTick)
        {
            MinuteUpdate();

            minuteCount++;

            int oldDate = currentTime.Date;
            Season oldSeason = currentTime.Season;

            currentTime.AddMinute(dayEndHour, dayBeginHour, DateOfSeason);

            // 如果 AddMinute 导致跨天，则触发日期更新
            if (currentTime.Date != oldDate || currentTime.Season != oldSeason)
            {
                OnNextDay();
            }
        }
    }

    public void Init()
    {
        dayCount = 0;
        minuteCount = 0;
        realTimeCount = 0f;

        currentTime = new ComplexTime(
            Season.Spring,
            1,
            dayBeginHour,
            0
        );
    }

    public void NextDay()
    {
        currentTime.NextDay(dayBeginHour, DateOfSeason);
        OnNextDay();
    }

    private void OnNextDay()
    {
        dayCount++;
        realTimeCount = 0f;

        DateUpdate();

        // TODO: 存档
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
        return currentTime.Copy();
    }

    public float GetRealTime()
    {
        return realTimeCount;
    }

    public float GetMinuteCount()
    {
        return minuteCount;
    }

    /// <summary>
    /// 计算 BTime - Atime 的时间差，单位：游戏分钟。
    /// </summary>
    public float TimeDistant(ComplexTime Atime, ComplexTime BTime)
    {
        float dateDistant =
            (BTime.Season - Atime.Season) * DateOfSeason +
            (BTime.Date - Atime.Date);

        float hourDistant = BTime.Hour - Atime.Hour;
        float minuteDistant = BTime.Minute - Atime.Minute;

        return (dateDistant * 24 + hourDistant) * 60 + minuteDistant;
    }

    /// <summary>
    /// 计算单日内 Ahour 到 Bhour 的时间差，单位：游戏分钟。
    /// 如果 Bhour 小于 Ahour，则视为跨天。
    /// </summary>
    public float TimeDistant(int Ahour, int Bhour)
    {
        if (Bhour < Ahour)
        {
            Bhour += 24;
        }

        return (Bhour - Ahour) * 60;
    }

    /// <summary>
    /// 计算 Atime 到当前时间的时间差，单位：游戏分钟。
    /// </summary>
    public float TimeDistToNow(ComplexTime Atime)
    {
        return TimeDistant(Atime, GetComplexTime());
    }

    /// <summary>
    /// 计算 Atime 到下一天开始时的时间差，单位：游戏分钟。
    /// </summary>
    public float TimeDistToNextDay(ComplexTime Atime)
    {
        ComplexTime nextDayTime = Atime.Copy();
        nextDayTime.NextDay(dayBeginHour, DateOfSeason);

        return TimeDistant(Atime, nextDayTime);
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
    public float TickToMinuteFloat(float tickTime)
    {
        if (minuteTransferRate <= 0f)
            return 0f;

        return tickTime / minuteTransferRate;
    }

    public void TickUpdate(float deltaTime)
    {
        foreach (var pair in WorldState.Instance.Entitys)
        {
            pair.Value.OnTickUpdate(deltaTime);
        }
    }

    public void MinuteUpdate()
    {
        foreach (var pair in WorldState.Instance.Entitys)
        {
            pair.Value.OnMinuteUpdate();
        }
    }

    public void DateUpdate()
    {
        foreach (var pair in WorldState.Instance.Entitys)
        {
            pair.Value.OnDateUpdate(GetComplexTime());
        }
    }
}