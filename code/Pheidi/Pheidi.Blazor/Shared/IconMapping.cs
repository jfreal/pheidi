using Pheidi.Common;
using System.Collections.Generic;

namespace Pheidi.Blazor.Shared
{
    public static class IconMapping
    {
        public static readonly Dictionary<Activity, IconAndFriendlyName> Activity = new Dictionary<Activity, IconAndFriendlyName>()
                {
                    {Common.Activity.Rest, new IconAndFriendlyName() { Icon ="fas fa-bed fa-fw", Name="REST"}},
                    {Common.Activity.Run, new IconAndFriendlyName() { Icon ="fas fa-running fa-fw", Name="RUN"}},
                    {Common.Activity.Sprint, new IconAndFriendlyName() { Icon ="fas fa-tachometer-alt fa-fw", Name="SPRINT"}},
                    {Common.Activity.Cross, new IconAndFriendlyName() { Icon ="fas fa-times fa-fw", Name="CROSS"}},
                    {Common.Activity.Strength, new IconAndFriendlyName() { Icon ="fas fa-dumbbell fa-fw", Name="STRENGTH"}},
                    {Common.Activity.Fartlek, new IconAndFriendlyName() { Icon ="fas fa-chart-line fa-fw", Name="FARTLEK"}}
                };

        public static readonly Dictionary<EffortType, IconAndFriendlyName> EffortType = new Dictionary<EffortType, IconAndFriendlyName>()
                {
                    {Common.EffortType.Distance, new IconAndFriendlyName() { Icon ="fas fa-road", Name="Distance"}},
                    {Common.EffortType.Time, new IconAndFriendlyName() { Icon ="far fa-clock", Name="Time"}},
                    {Common.EffortType.Reps, new IconAndFriendlyName() { Icon ="fas fa-tachometer-alt fa-fw", Name="Reps"}}
                };

        public static readonly Dictionary<DistanceType, IconAndFriendlyName> DistanceType = new Dictionary<DistanceType, IconAndFriendlyName>()
                {
                    {Common.DistanceType.Long, new IconAndFriendlyName() { Name = "L" } },
                    {Common.DistanceType.Half, new IconAndFriendlyName() { Name = "½ L" } },
                    {Common.DistanceType.Quarter, new IconAndFriendlyName() { Name = "¼ L" } },
                    {Common.DistanceType.QuarterUp, new IconAndFriendlyName() { Name = "¼ L<i class=\"fas fa-angle-up\"></i>" } },
                    {Common.DistanceType.None, new IconAndFriendlyName() { Name = "-" } },
                };
    }

    public class IconAndFriendlyName
    {
        public string Icon { get; set; }
        public string Name { get; set; }
    }
}
