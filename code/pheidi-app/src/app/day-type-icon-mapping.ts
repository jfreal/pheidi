import { DayType } from './day-type.enum';

export const DayTypeIconMapping: Record<DayType, string> = {
    [DayType.Rest]: "fas fa-bed",
    [DayType.Run]: "fas fa-running",
    [DayType.Sprint]: "fas fa-tachometer-alt",
    [DayType.Cross]: "fas fa-times",
    [DayType.Strength]: "fas fa-dumbbell",
    [DayType.Fartlek]: "fas fa-chart-line"
};