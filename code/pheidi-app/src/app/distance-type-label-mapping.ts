import { DistanceType } from './distance-type.enum';

export const DistanceTypeLabelMapping: Record<DistanceType, string> = {
    [DistanceType.None]: "-",
    [DistanceType.Half]: "½ L",
    [DistanceType.Quarter]: "¼ L",
    [DistanceType.QuarterUp]: "¼ L",
    [DistanceType.Long]: "L",
};