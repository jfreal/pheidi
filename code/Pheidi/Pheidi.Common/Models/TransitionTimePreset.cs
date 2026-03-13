namespace Pheidi.Common.Models;

public enum TransitionTimePreset
{
    None = 0,
    HomeShower = 20,
    GymShower = 25,
    LunchBreak = 30,
    GymWithCommute = 35
}

public static class TransitionTimePresetExtensions
{
    public static int Minutes(this TransitionTimePreset preset) => (int)preset;

    public static string Description(this TransitionTimePreset preset) => preset switch
    {
        TransitionTimePreset.None => "No transition time needed",
        TransitionTimePreset.HomeShower => "Shower at home (20 min)",
        TransitionTimePreset.GymShower => "Shower at gym (25 min)",
        TransitionTimePreset.LunchBreak => "Lunch break run (30 min)",
        TransitionTimePreset.GymWithCommute => "Gym with commute (35 min)",
        _ => "No transition time"
    };
}
