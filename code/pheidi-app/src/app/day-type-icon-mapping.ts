import { DayType } from './day-type.enum';

export const DayTypeIconMapping: Record<DayType, string> = {
    [DayType.Rest]: "fas fa-bed fa-fw",
    [DayType.Run]: "fas fa-running fa-fw",
    [DayType.Sprint]: "fas fa-tachometer-alt fa-fw",
    [DayType.Cross]: "fas fa-times fa-fw",
    [DayType.Strength]: "fas fa-dumbbell fa-fw",
    [DayType.Fartlek]: "fas fa-chart-line fa-fw"
};