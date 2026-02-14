using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CalendarEvent
{
    public int day;
    public string title;
    public CalendarEventType type;
}

public enum CalendarEventType
{
    Arena,
    PromotionMatch,
    Story,
    Quest
}

[Serializable]
public class CalendarSystem
{
    // 총 게임 기간: 3년 = 1080일 (360일/년)
    public const int DaysPerMonth = 30;
    public const int MonthsPerYear = 12;
    public const int TotalYears = 3;
    public const int TotalDays = DaysPerMonth * MonthsPerYear * TotalYears; // 1080

    // 승급전 주기: 3개월 = 90일
    public const int PromotionInterval = DaysPerMonth * 3; // 90

    // 아레나 주기: 3일
    public const int ArenaInterval = 3;

    /// <summary>해당 날짜가 아레나 오픈일인지</summary>
    public static bool IsArenaDay(int day) => day % ArenaInterval == 0;

    /// <summary>해당 날짜가 승급전인지</summary>
    public static bool IsPromotionDay(int day) => day > 0 && day % PromotionInterval == 0;

    /// <summary>현재 월 (1~12 반복)</summary>
    public static int GetMonth(int day) => ((day - 1) / DaysPerMonth) % MonthsPerYear + 1;

    /// <summary>현재 년 (1~3)</summary>
    public static int GetYear(int day) => (day - 1) / (DaysPerMonth * MonthsPerYear) + 1;

    /// <summary>월 내 일자 (1~30)</summary>
    public static int GetDayOfMonth(int day) => (day - 1) % DaysPerMonth + 1;

    /// <summary>다음 아레나 날짜</summary>
    public static int NextArenaDay(int currentDay)
    {
        for (int d = currentDay + 1; d <= currentDay + ArenaInterval; d++)
            if (IsArenaDay(d)) return d;
        return currentDay + ArenaInterval;
    }

    /// <summary>다음 승급전 날짜</summary>
    public static int NextPromotionDay(int currentDay)
    {
        int next = ((currentDay / PromotionInterval) + 1) * PromotionInterval;
        return next;
    }

    /// <summary>특정 월의 이벤트 목록 생성</summary>
    public static List<CalendarEvent> GetMonthEvents(int year, int month)
    {
        var events = new List<CalendarEvent>();

        int monthStart = ((year - 1) * MonthsPerYear + (month - 1)) * DaysPerMonth + 1;
        int monthEnd = monthStart + DaysPerMonth - 1;

        for (int d = monthStart; d <= monthEnd; d++)
        {
            if (IsPromotionDay(d))
            {
                events.Add(new CalendarEvent
                {
                    day = d,
                    title = "승급전",
                    type = CalendarEventType.PromotionMatch
                });
            }
            else if (IsArenaDay(d))
            {
                events.Add(new CalendarEvent
                {
                    day = d,
                    title = "아레나",
                    type = CalendarEventType.Arena
                });
            }
        }

        return events;
    }

    /// <summary>날짜를 "Y년 M월 D일" 형식으로</summary>
    public static string FormatDate(int day)
    {
        int y = GetYear(day);
        int m = GetMonth(day);
        int d = GetDayOfMonth(day);
        return $"{y}년 {m}월 {d}일";
    }
}
