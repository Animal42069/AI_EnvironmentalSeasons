using AIProject;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Manager;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AI_EnvironmentalSeasons
{
    [BepInPlugin(GUID, "Environmental Seasons", VERSION)]
    [BepInProcess("AI-Syoujyo")]

    public class EnvironmentalSeasons : BaseUnityPlugin
    {
        public const string VERSION = "1.1.2.0";
        internal const string GUID = "animal42069.aienvironmentalseasons";

        internal static Harmony harmony;

        internal static ConfigEntry<float> _location_latitute;
        internal static ConfigEntry<float> _location_longitude;
        internal static ConfigEntry<int> _location_utcOffset;
        internal static ConfigEntry<int> _cold_threshold;
        internal static ConfigEntry<int> _hot_threshold;
        internal static ConfigEntry<int> _start_year;
        internal static ConfigEntry<int> _start_day;
        internal static ConfigEntry<float> _length_of_day;

        internal static int currentMonth = 0;

        internal static readonly List<int> monthlyHighDayTemperature = new List<int> { 23, 24, 26, 28, 30, 33, 33, 33, 33, 31, 29, 25 };
        internal static readonly List<int> monthlyLowDayTemperature = new List<int> { 15, 15, 16, 19, 22, 25, 29, 29, 28, 24, 20, 16 };

        internal static readonly List<int> monthlyHighNightTemperature = new List<int> { 18, 19, 22, 24, 26, 28, 28, 28, 28, 26, 24, 21 };
        internal static readonly List<int> monthlyLowNightTemperature = new List<int> { 10, 11, 11, 13, 17, 20, 25, 23, 21, 17, 14, 12 };

        internal static readonly List<(int, int)> monthlyMorningTime = new List<(int, int)> { (5, 56), (5, 46), (5, 21), (4, 45), (4, 15), (4, 03), (4, 15), (4, 37), (4, 56), (5, 10), (5, 27), (5, 46) };
        internal static readonly List<(int, int)> monthlyDayTime = new List<(int, int)> { (10, 37), (10, 42), (10, 37), (10, 28), (10, 24), (10, 28), (10, 34), (10, 32), (10, 23), (10, 14), (10, 12), (10, 23) };
        internal static readonly List<(int, int)> monthlyNightTime = new List<(int, int)> { (19, 18), (19, 38), (19, 53), (20, 12), (20, 34), (20, 53), (20, 52), (20, 27), (19, 50), (19, 17), (18, 58), (19, 00) };
 
        internal static readonly List<(int, int)> monthlyMorningLightTime = new List<(int, int)> { (7, 17), (7, 04), (6, 37), (6, 05), (5, 41), (5, 34), (5, 44), (6, 00), (6, 13), (6, 27), (6, 47), (7, 08) };
        internal static readonly List<(int, int)> monthlyDayLightTime = new List<(int, int)> { (12, 37), (12, 42), (12, 37), (12, 28), (12, 24), (12, 28), (12, 34), (12, 32), (12, 23), (12, 14), (12, 12), (12, 23) };
        internal static readonly List<(int, int)> monthlyNightLightTime = new List<(int, int)> { (17, 57), (18, 20), (18, 36), (18, 52), (19, 08), (19, 22), (19, 23), (19, 04), (18, 32), (18, 00), (17, 38), (17, 38) };

        internal static EnviroSky enviroSky;
        internal static EnvironmentSimulator environmentSimulator;
        internal static UnityEngine.UI.Text dateLabel;

        public void Awake()
        {
            (_location_latitute = Config.Bind("World Location", "Latitude", 26.36f, new ConfigDescription("World Latitude", new AcceptableValueRange<float>(-90f, 90f)))).SettingChanged += (s, e) =>
            { UpdateWorldPosition(); };
            (_location_longitude = Config.Bind("World Location", "Longitude", 127.145f, new ConfigDescription("World Longitude", new AcceptableValueRange<float>(-180f, 180f)))).SettingChanged += (s, e) =>
            { UpdateWorldPosition(); };
            (_location_utcOffset = Config.Bind("World Location", "UTC Offset", 9, new ConfigDescription("World UTC Timezone Offset", new AcceptableValueRange<int>(-13, 13)))).SettingChanged += (s, e) =>
            { UpdateWorldPosition(); };

            _cold_threshold = Config.Bind("Temperature", "Cold Threshold", 18, "Characters will get cold when the temperature is below this threshold");
            _hot_threshold = Config.Bind("Temperature", "Hot Threshold", 27, "Characters will get hot when the temperature is above this threshold");

            _start_year = Config.Bind("Time", "Starting Year", DateTime.Now.Year, "Year that a new game starts on");
            _start_day = Config.Bind("Time", "Starting Day", 90, "Day that a new game starts on");
            _length_of_day = Config.Bind("Time", "Length of Day", 80f, "Number of real minutes per game day");

            harmony = new Harmony("EnvironmentalSeasons");
            harmony.PatchAll(typeof(EnvironmentalSeasons));
        }

        [HarmonyPrefix, HarmonyPatch(typeof(EnvironmentSimulator), "OnTimeUpdate")]
        internal static bool EnvironmentSimulator_OnTimeUpdate(EnvironmentSimulator __instance)
        {
            if (__instance._enviroSky == null || __instance._enviroSky.GameTime == null)
                return true;

            if (__instance._enviroSky.GameTime.Years <= 1)
            {
                environmentSimulator = __instance;

                var loadDateTime = Game.Instance.Data.AutoData.Environment.Time;

                if (loadDateTime._year == 1)
                {
                    __instance._enviroSky.GameTime.Years = _start_year.Value;
                    __instance._enviroSky.GameTime.Days = _start_day.Value;
                }
                else
                {
                    __instance._enviroSky.GameTime.Years = loadDateTime._year;
                    __instance._enviroSky.GameTime.Days = GetDayOfYear(loadDateTime._day, loadDateTime._month, loadDateTime._year);
                }

                __instance.SetTimeToEnviroTime(__instance.OldDayUpdatedTime, __instance._enviroSky.GameTime);
                __instance.SetTimeToEnviroTime(__instance.OldHourUpdatedTime, __instance._enviroSky.GameTime);
                __instance.SetTimeToEnviroTime(__instance.OldMinuteUpdatedTime, __instance._enviroSky.GameTime);
                __instance.SetTimeToEnviroTime(__instance.OldSecondUpdatedTime, __instance._enviroSky.GameTime);

                __instance.OldTime.Days = __instance._enviroSky.GameTime.Days;
                __instance.OldTime.Hours = __instance._enviroSky.GameTime.Hours;
                __instance.OldTime.Minutes = __instance._enviroSky.GameTime.Minutes;
                __instance.OldTime.Seconds = __instance._enviroSky.GameTime.Seconds;

                UpdateDayLength();

                if (dateLabel != null)
                    dateLabel.text = GetDateTime(__instance._enviroSky.GameTime).ToString("D");

                return false;
            }

            EnviroTime oldTime = __instance.OldTime;
            EnviroTime gameTime = __instance._enviroSky.GameTime;
            DateTime newTime = GetDateTime(gameTime);
            bool timeUpdated = false;

            if (gameTime.Days > oldTime.Days)
            {
                timeUpdated = true;

                if (__instance._onDay != null)
                    __instance._onDay.OnNext(newTime - GetDateTime(__instance.OldDayUpdatedTime));

                __instance.SetTimeToEnviroTime(__instance.OldDayUpdatedTime, gameTime);
            }

            if (timeUpdated || gameTime.Hours > oldTime.Hours)
            {
                timeUpdated = true;

                if (__instance._onHour != null)
                    __instance._onHour.OnNext(newTime - GetDateTime(__instance.OldHourUpdatedTime));

                __instance.SetTimeToEnviroTime(__instance.OldHourUpdatedTime, gameTime);
            }

            if (timeUpdated || gameTime.Minutes > oldTime.Minutes)
            {
                timeUpdated = true;

                if (__instance._onMinute != null)
                    __instance._onMinute.OnNext(newTime - GetDateTime(__instance.OldMinuteUpdatedTime));

                __instance.SetTimeToEnviroTime(__instance.OldMinuteUpdatedTime, gameTime);
            }

            if (timeUpdated || gameTime.Seconds > oldTime.Seconds)
            {
                timeUpdated = true;

                if (__instance._onSecond != null)
                    __instance._onSecond.OnNext(newTime - GetDateTime(__instance.OldSecondUpdatedTime));

                __instance.SetTimeToEnviroTime(__instance.OldSecondUpdatedTime, gameTime);
            }

            if (timeUpdated)
            {
                __instance.SetTimeToEnviroTime(__instance.OldTime, gameTime);

                if (dateLabel != null)
                    dateLabel.text = newTime.ToString("D");
            }

            UpdateEnvironmentProfile(__instance, GetMonthOfYear(gameTime.Days, gameTime.Years));

            return false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(EnvironmentSimulator), "SetTimeToEnviroTime")]
        internal static void EnvironmentSimulator_SetTimeToEnviroTime(EnviroTime time, EnviroTime newTime)
        {
            time.Years = newTime.Years;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(EnvironmentSimulator), "RefreshTemperatureValue")]
        internal static bool EnvironmentSimulator_RefreshTemperatureValue(EnvironmentSimulator __instance)
        {
            Threshold range = __instance._environmentProfile.WeatherTemperatureRange.GetRange(__instance._tempTimeZone, __instance._weather);
            __instance.SetTemperatureValue(range.RandomValue);
            return false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(AIProject.SaveData.Environment), "SetSimulation")]
        internal static void Environment_SetSimulation(AIProject.SaveData.Environment __instance)
        {
            if (enviroSky == null)
                return;

            int day = enviroSky.GameTime.Days;
            int year = enviroSky.GameTime.Years;
            int month = GetDayAndMonthOfYear(ref day, year);

            __instance.Time = new AIProject.SaveData.Environment.SerializableDateTime(year, month, day, enviroSky.GameTime.Hours, enviroSky.GameTime.Minutes, enviroSky.GameTime.Seconds);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(EnviroSky), "AssignAndStart")]
        internal static void EnviroSky_AssignAndStart(EnviroSky __instance)
        {
            enviroSky = __instance;

            int day = Manager.Game.Instance.Environment.Time.Day;
            int month = Manager.Game.Instance.Environment.Time.Month;
            int year = Manager.Game.Instance.Environment.Time.Year;

            enviroSky.GameTime.Days = GetDayOfYear(day, month, year);
            enviroSky.GameTime.Years = year;

            UpdateWorldPosition();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(EnviroSky), "SetGameTime")]
        internal static void EnviroSky_SetGameTime(EnviroSky __instance)
        {
            if (__instance.internalHour < 0)
                __instance.internalHour = 0;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Manager.Map), "InitSearchActorTargetsAll")]
        public static void MapManager_InitSearchActorTargetsAll()
        {
            var timeObject = GameObject.Find("MapScene/MapUI(Clone)/CommandCanvas/MenuUI(Clone)/CellularUI/Interface Panel/HomeMenu(Clone)/Time");
            if (timeObject == null)
                return;
            timeObject.transform.localPosition = new Vector3(0, 315, 0);

            GameObject dateObject = Instantiate(timeObject);
            dateObject.name = "Date";
            dateObject.transform.SetParent(timeObject.transform.parent);
            dateObject.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            dateObject.transform.localPosition = new Vector3(0, 385, 0);
            dateObject.GetComponent<RectTransform>().sizeDelta = timeObject.GetComponent<RectTransform>().sizeDelta;

            dateLabel = dateObject.GetComponent<UnityEngine.UI.Text>();

            var weatherObject = GameObject.Find("MapScene/MapUI(Clone)/CommandCanvas/MenuUI(Clone)/CellularUI/Interface Panel/HomeMenu(Clone)/WeatherDisplay");
            if (weatherObject == null)
                return;

            weatherObject.transform.localPosition = new Vector3(0, 150, 0);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(EnvironmentSimulator), "Now", MethodType.Getter)]
        public static bool EnvironmentSimulator_Now(EnvironmentSimulator __instance, ref DateTime __result)
        {
            EnviroTime gameTime = __instance._enviroSky.GameTime;
            __result = new DateTime(1, 1, 1, gameTime.Hours, gameTime.Minutes, gameTime.Seconds);

            return false;
        }

        internal static void UpdateEnvironmentProfile(EnvironmentSimulator environment, int newMonth)
        {
            if (currentMonth == newMonth)
                return;

            currentMonth = newMonth;

            environment._environmentProfile._temperatureBorder._maxDegree = monthlyHighDayTemperature[newMonth];
            environment._environmentProfile._temperatureBorder._minDegree = monthlyLowNightTemperature[newMonth];

            environment._environmentProfile._temperatureBorder._highBorder = _hot_threshold.Value;
            environment._environmentProfile._temperatureBorder._lowBorder = _cold_threshold.Value;

            environment._environmentProfile._morningTime._hour = monthlyMorningTime[newMonth].Item1;
            environment._environmentProfile._morningTime._minute = monthlyMorningTime[newMonth].Item2;
            environment._environmentProfile._dayTime._hour = monthlyDayTime[newMonth].Item1;
            environment._environmentProfile._dayTime._minute = monthlyDayTime[newMonth].Item2;
            environment._environmentProfile._nightTime._hour = monthlyNightTime[newMonth].Item1;
            environment._environmentProfile._nightTime._minute = monthlyNightTime[newMonth].Item2;

            environment._environmentProfile._lightMorningTime._hour = monthlyMorningLightTime[newMonth].Item1;
            environment._environmentProfile._lightMorningTime._minute = monthlyMorningLightTime[newMonth].Item2;
            environment._environmentProfile._lightDayTime._hour = monthlyDayLightTime[newMonth].Item1;
            environment._environmentProfile._lightDayTime._minute = monthlyDayLightTime[newMonth].Item2;
            environment._environmentProfile._lightNightTime._hour = monthlyNightLightTime[newMonth].Item1;
            environment._environmentProfile._lightNightTime._minute = monthlyNightLightTime[newMonth].Item2;

            environment._environmentProfile._weatherTemperatureRange._dayTime._hour = monthlyDayTime[newMonth].Item1;
            environment._environmentProfile._weatherTemperatureRange._dayTime._minute = monthlyDayTime[newMonth].Item2;
            environment._environmentProfile._weatherTemperatureRange._nightTime._hour = monthlyNightTime[newMonth].Item1;
            environment._environmentProfile._weatherTemperatureRange._nightTime._minute = monthlyNightTime[newMonth].Item2;

            UpdateEnvironmentWeatherProfile(environment, newMonth);
        }

        internal static void UpdateEnvironmentWeatherProfile(EnvironmentSimulator environment, int newMonth)
        {
            int maxDayTemp = monthlyHighDayTemperature[newMonth];
            int minDayTemp = monthlyLowDayTemperature[newMonth];
            int midDayTemp = (maxDayTemp + minDayTemp) / 2 + (maxDayTemp + minDayTemp) % 2;
            int midHighDayTemp = (maxDayTemp + midDayTemp) / 2 + (maxDayTemp + midDayTemp) % 2;
            int midLowDayTemp = (midDayTemp + minDayTemp) / 2 + (midDayTemp + minDayTemp) % 2;

            environment._environmentProfile._weatherTemperatureRange._dayTimeRange._clearRange = new Threshold(midDayTemp, maxDayTemp);
            environment._environmentProfile._weatherTemperatureRange._dayTimeRange._cloud1Range = new Threshold(midLowDayTemp, midHighDayTemp);
            environment._environmentProfile._weatherTemperatureRange._dayTimeRange._cloud2Range = new Threshold(midLowDayTemp, midHighDayTemp);
            environment._environmentProfile._weatherTemperatureRange._dayTimeRange._cloud3Range = new Threshold(minDayTemp, midHighDayTemp);
            environment._environmentProfile._weatherTemperatureRange._dayTimeRange._cloud4Range = new Threshold(minDayTemp, midHighDayTemp);
            environment._environmentProfile._weatherTemperatureRange._dayTimeRange._fogRange = new Threshold(minDayTemp, midDayTemp);
            environment._environmentProfile._weatherTemperatureRange._dayTimeRange._rainRange = new Threshold(minDayTemp, midDayTemp);
            environment._environmentProfile._weatherTemperatureRange._dayTimeRange._stormRange = new Threshold(minDayTemp, midLowDayTemp);

            int maxNightTemp = monthlyHighNightTemperature[newMonth];
            int minNightTemp = monthlyLowNightTemperature[newMonth];
            int midNightTemp = (maxNightTemp + minNightTemp) / 2 + (maxNightTemp + minNightTemp) % 2;
            int midHighNightTemp = (maxNightTemp + midNightTemp) / 2 + (maxNightTemp + midNightTemp) % 2;
            int midLowNightTemp = (midNightTemp + minNightTemp) / 2 + (midNightTemp + minNightTemp) % 2;

            environment._environmentProfile._weatherTemperatureRange._nightTimeRange._clearRange = new Threshold(midNightTemp, maxNightTemp);
            environment._environmentProfile._weatherTemperatureRange._nightTimeRange._cloud1Range = new Threshold(midLowNightTemp, midHighNightTemp);
            environment._environmentProfile._weatherTemperatureRange._nightTimeRange._cloud2Range = new Threshold(midLowNightTemp, midHighNightTemp);
            environment._environmentProfile._weatherTemperatureRange._nightTimeRange._cloud3Range = new Threshold(minNightTemp, midHighNightTemp);
            environment._environmentProfile._weatherTemperatureRange._nightTimeRange._cloud4Range = new Threshold(minNightTemp, midHighNightTemp);
            environment._environmentProfile._weatherTemperatureRange._nightTimeRange._fogRange = new Threshold(minNightTemp, midNightTemp);
            environment._environmentProfile._weatherTemperatureRange._nightTimeRange._rainRange = new Threshold(minNightTemp, midNightTemp);
            environment._environmentProfile._weatherTemperatureRange._nightTimeRange._stormRange = new Threshold(minNightTemp, midLowNightTemp);
        }

        internal static DateTime GetDateTime(EnviroTime time)
        {
            int day = Math.Max(time.Days, 1);
            int year = Math.Max(time.Years, 1);
            int month = GetDayAndMonthOfYear(ref day, year);
            int hours = Math.Min(Math.Max(time.Hours, 0), 23);
            int minutes = Math.Min(Math.Max(time.Minutes, 0), 59);
            int seconds = Math.Min(Math.Max(time.Seconds, 0), 59);

            return new DateTime(year, month, day, hours, minutes, seconds);
        }

        internal static int GetDayAndMonthOfYear(ref int dayOfYear, int year)
        {
            int month = 1;
            while (dayOfYear > DateTime.DaysInMonth(year, month))
            {
                dayOfYear -= DateTime.DaysInMonth(year, month);
                month++;
            }

            return month;
        }

        internal static int GetMonthOfYear(int dayOfYear, int year)
        {
            int month = 1;
            while (dayOfYear > DateTime.DaysInMonth(year, month))
            {
                dayOfYear -= DateTime.DaysInMonth(year, month);
                month++;
            }

            return month;
        }

        internal static int GetDayOfYear(int day, int month, int year)
        {
            int dayOfYear = 0;

            while (month > 1)
                dayOfYear += DateTime.DaysInMonth(year, month--);

            dayOfYear += day;
            return dayOfYear;
        }

        internal static void UpdateWorldPosition()
        {
            if (enviroSky == null)
                return;

            enviroSky.GameTime.Latitude = _location_latitute.Value;
            enviroSky.GameTime.Longitude = _location_longitude.Value;
            enviroSky.GameTime.utcOffset = _location_utcOffset.Value;
        }

        internal static void SetTimeToEnviroTime(EnviroTime time, EnviroTime newTime)
        {
            time.Years = newTime.Years;
            time.Days = newTime.Days;
            time.Hours = newTime.Hours;
            time.Minutes = newTime.Minutes;
            time.Seconds = newTime.Seconds;
        }

        internal static void UpdateDayLength()
        {
            if (environmentSimulator == null)
                return;

            environmentSimulator.EnvironmentProfile.DayLengthInMinute = _length_of_day.Value / 2;
        }
    }
}
