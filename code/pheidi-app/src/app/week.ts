import { Day } from './day'
import { DistanceType } from './distance-type.enum';

export class Week {
    weekNumber: number;
    lastLongRun: boolean;

    distances: Record<DistanceType, number> = {
        [DistanceType.Long]: 0,
        [DistanceType.Half]: 0,
        [DistanceType.Quarter]: 0,
        [DistanceType.QuarterUp]: 0,
        [DistanceType.None]: 0
    };
    taper: boolean;

    constructor(weekNumber: number) {
        this.weekNumber = weekNumber;
    }
}
