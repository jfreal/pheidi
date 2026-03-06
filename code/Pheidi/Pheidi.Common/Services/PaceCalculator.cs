using Pheidi.Common.Models;

namespace Pheidi.Common.Services;

public class PaceCalculator
{
    // Simplified VDOT table: maps VDOT values to pace per mile (in seconds) for each zone
    // Source: Jack Daniels' Running Formula (simplified subset)
    private static readonly Dictionary<int, Dictionary<VdotZone, (int MinSecPerMile, int MaxSecPerMile)>> VdotTable = new()
    {
        [30] = new() { [VdotZone.Easy] = (720, 780), [VdotZone.Marathon] = (660, 690), [VdotZone.Tempo] = (600, 630), [VdotZone.Interval] = (540, 570), [VdotZone.Repetition] = (500, 530) },
        [35] = new() { [VdotZone.Easy] = (640, 700), [VdotZone.Marathon] = (585, 615), [VdotZone.Tempo] = (540, 565), [VdotZone.Interval] = (490, 515), [VdotZone.Repetition] = (455, 480) },
        [40] = new() { [VdotZone.Easy] = (575, 630), [VdotZone.Marathon] = (530, 555), [VdotZone.Tempo] = (490, 510), [VdotZone.Interval] = (445, 465), [VdotZone.Repetition] = (415, 435) },
        [45] = new() { [VdotZone.Easy] = (525, 575), [VdotZone.Marathon] = (485, 510), [VdotZone.Tempo] = (450, 470), [VdotZone.Interval] = (410, 430), [VdotZone.Repetition] = (385, 400) },
        [50] = new() { [VdotZone.Easy] = (485, 530), [VdotZone.Marathon] = (450, 470), [VdotZone.Tempo] = (415, 435), [VdotZone.Interval] = (380, 400), [VdotZone.Repetition] = (358, 375) },
        [55] = new() { [VdotZone.Easy] = (450, 495), [VdotZone.Marathon] = (420, 438), [VdotZone.Tempo] = (390, 405), [VdotZone.Interval] = (358, 375), [VdotZone.Repetition] = (338, 353) },
        [60] = new() { [VdotZone.Easy] = (420, 465), [VdotZone.Marathon] = (393, 410), [VdotZone.Tempo] = (367, 382), [VdotZone.Interval] = (340, 355), [VdotZone.Repetition] = (320, 335) },
        [65] = new() { [VdotZone.Easy] = (395, 438), [VdotZone.Marathon] = (370, 385), [VdotZone.Tempo] = (348, 362), [VdotZone.Interval] = (323, 338), [VdotZone.Repetition] = (305, 318) },
        [70] = new() { [VdotZone.Easy] = (372, 415), [VdotZone.Marathon] = (350, 365), [VdotZone.Tempo] = (330, 345), [VdotZone.Interval] = (308, 322), [VdotZone.Repetition] = (292, 305) },
    };

    // VDOT estimation from race results: maps (distance, time in seconds) to approximate VDOT
    // Simplified lookup using marathon equivalent times
    private static readonly (RaceDistance Distance, int TimeSeconds, int Vdot)[] VdotLookup =
    [
        (RaceDistance.FiveK, 1500, 30), (RaceDistance.FiveK, 1320, 35), (RaceDistance.FiveK, 1170, 40),
        (RaceDistance.FiveK, 1050, 45), (RaceDistance.FiveK, 960, 50), (RaceDistance.FiveK, 880, 55),
        (RaceDistance.FiveK, 810, 60), (RaceDistance.FiveK, 750, 65), (RaceDistance.FiveK, 700, 70),
        (RaceDistance.TenK, 3120, 30), (RaceDistance.TenK, 2760, 35), (RaceDistance.TenK, 2460, 40),
        (RaceDistance.TenK, 2190, 45), (RaceDistance.TenK, 1980, 50), (RaceDistance.TenK, 1830, 55),
        (RaceDistance.TenK, 1680, 60), (RaceDistance.TenK, 1560, 65), (RaceDistance.TenK, 1455, 70),
        (RaceDistance.HalfMarathon, 6960, 30), (RaceDistance.HalfMarathon, 6120, 35),
        (RaceDistance.HalfMarathon, 5400, 40), (RaceDistance.HalfMarathon, 4860, 45),
        (RaceDistance.HalfMarathon, 4380, 50), (RaceDistance.HalfMarathon, 4020, 55),
        (RaceDistance.HalfMarathon, 3720, 60), (RaceDistance.HalfMarathon, 3450, 65),
        (RaceDistance.HalfMarathon, 3210, 70),
        (RaceDistance.FullMarathon, 14880, 30), (RaceDistance.FullMarathon, 13080, 35),
        (RaceDistance.FullMarathon, 11520, 40), (RaceDistance.FullMarathon, 10320, 45),
        (RaceDistance.FullMarathon, 9360, 50), (RaceDistance.FullMarathon, 8580, 55),
        (RaceDistance.FullMarathon, 7920, 60), (RaceDistance.FullMarathon, 7380, 65),
        (RaceDistance.FullMarathon, 6900, 70),
    ];

    public PaceZone GetPaceZone(VdotZone zone, decimal vdot)
    {
        var nearestVdot = VdotTable.Keys.OrderBy(k => Math.Abs(k - (int)vdot)).First();
        var paces = VdotTable[nearestVdot];

        if (!paces.TryGetValue(zone, out var pace))
            return PaceZone.ForWorkoutType(WorkoutType.Easy);

        return new PaceZone
        {
            Zone = zone,
            MinPacePerMile = pace.MinSecPerMile / 60m,
            MaxPacePerMile = pace.MaxSecPerMile / 60m,
            RpeMin = PaceZone.RpeMapping[zone].Min,
            RpeMax = PaceZone.RpeMapping[zone].Max,
            RpeDescription = PaceZone.RpeMapping[zone].Description
        };
    }

    public int EstimateVdot(RaceDistance distance, TimeSpan raceTime)
    {
        var timeSeconds = (int)raceTime.TotalSeconds;
        var relevant = VdotLookup
            .Where(v => v.Distance == distance)
            .OrderBy(v => Math.Abs(v.TimeSeconds - timeSeconds))
            .First();

        return relevant.Vdot;
    }

    public static string FormatPace(decimal minutesPerMile)
    {
        var totalSeconds = (int)(minutesPerMile * 60);
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;
        return $"{minutes}:{seconds:D2}";
    }

    public static string GetRpeDescription(int rpe) => rpe switch
    {
        1 => "Very light — barely moving",
        2 => "Light — warming up",
        3 => "Easy — conversational pace",
        4 => "Moderate-easy — can talk in sentences",
        5 => "Moderate — can talk in short phrases",
        6 => "Moderate-hard — steady effort",
        7 => "Hard — comfortably hard",
        8 => "Very hard — breathing heavy",
        9 => "Near max — can only say a few words",
        10 => "All-out — maximum effort",
        _ => "Unknown effort level"
    };
}
