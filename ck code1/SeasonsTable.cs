using System;
using System.Collections.Generic;
using Pug.UnityExtensions;
using UnityEngine;

[CreateAssetMenu(menuName = "Pug/Tables/SeasonsTable", order = 4)]
public class SeasonsTable : ScriptableObject
{
	[Serializable]
	public class SeasonDates
	{
		public Season season;

		public SeasonsDate date;

		[ArrayElementTitle("year")]
		public List<SeasonsDateWithYear> specificYearDates;
	}

	[Serializable]
	public class SeasonsDate
	{
		public int startDay;

		public int startMonth;

		public int endDay;

		public int endMonth;
	}

	[Serializable]
	public class SeasonsDateWithYear
	{
		public int year;

		public SeasonsDate date;
	}

	[ArrayElementTitle("season")]
	public List<SeasonDates> seasonDates;

	public Season CalculateSeason()
	{
		Season result = Season.None;
		DateTime utcNow = DateTime.UtcNow;
		foreach (SeasonDates seasonDate in seasonDates)
		{
			bool flag = false;
			foreach (SeasonsDateWithYear specificYearDate in seasonDate.specificYearDates)
			{
				if (!flag)
				{
					flag = utcNow.Year == specificYearDate.year;
				}
				if (IsBetweenTwoDates(utcNow, specificYearDate.date, checkYear: true, specificYearDate.year))
				{
					result = seasonDate.season;
					break;
				}
			}
			if (!flag && IsBetweenTwoDates(utcNow, seasonDate.date, checkYear: false, 0))
			{
				result = seasonDate.season;
				break;
			}
		}
		return result;
	}

	private void OnValidate()
	{
		foreach (SeasonDates seasonDate in seasonDates)
		{
			seasonDate.date.startDay = ValidateDay(seasonDate.date.startDay, seasonDate.date.startMonth, 0);
			seasonDate.date.endDay = ValidateDay(seasonDate.date.endDay, seasonDate.date.endMonth, 0);
			seasonDate.date.startMonth = ValidateMonth(seasonDate.date.startMonth);
			seasonDate.date.endMonth = ValidateMonth(seasonDate.date.endMonth);
			foreach (SeasonsDateWithYear specificYearDate in seasonDate.specificYearDates)
			{
				specificYearDate.date.startDay = ValidateDay(specificYearDate.date.startDay, specificYearDate.date.startMonth, specificYearDate.year);
				specificYearDate.date.endDay = ValidateDay(specificYearDate.date.endDay, specificYearDate.date.endMonth, specificYearDate.year);
				specificYearDate.date.startMonth = ValidateMonth(specificYearDate.date.startMonth);
				specificYearDate.date.endMonth = ValidateMonth(specificYearDate.date.endMonth);
			}
		}
	}

	private int ValidateDay(int day, int month, int year)
	{
		int num = DaysInMonth(month, year);
		if (day < 1)
		{
			day = 1;
		}
		if (day > num)
		{
			day = num;
		}
		return day;
	}

	private int DaysInMonth(int month, int year)
	{
		if (year % 4 == 0 && month == 2)
		{
			return 29;
		}
		if (month == 1 || month == 3 || month == 5 || month == 7 || month == 8 || month == 10 || month == 12)
		{
			return 31;
		}
		return 30;
	}

	private int ValidateMonth(int month)
	{
		if (month < 1)
		{
			month = 1;
		}
		if (month > 12)
		{
			month = 12;
		}
		return month;
	}

	private static bool IsBetweenTwoDates(DateTime dt, SeasonsDate date, bool checkYear, int year)
	{
		int year2 = (checkYear ? year : dt.Year);
		DateTime dateTime = new DateTime(year2, date.startMonth, date.startDay, 9, 0, 0);
		DateTime dateTime2 = new DateTime(year2, date.endMonth, date.endDay, 23, 59, 59);
		if (dateTime2 < dateTime && !checkYear)
		{
			DateTime dateTime3 = new DateTime(dt.Year, 12, 31, 23, 59, 59);
			dateTime = new DateTime(dt.Year, date.startMonth, date.startDay, 9, 0, 0);
			DateTime dateTime4 = new DateTime(dt.Year, 1, 1, 0, 0, 0);
			dateTime2 = new DateTime(dt.Year, date.endMonth, date.endDay, 23, 59, 59);
			if (!(dt >= dateTime) || !(dt.ToLocalTime() <= dateTime3))
			{
				if (dt.ToLocalTime() >= dateTime4)
				{
					return dt.ToLocalTime() <= dateTime2;
				}
				return false;
			}
			return true;
		}
		if (dt >= dateTime)
		{
			return dt.ToLocalTime() <= dateTime2;
		}
		return false;
	}
}
