import { Day } from './day'
import { DistanceType } from './distance-type.enum';

export class Week {
    weekNumber: number;
    lastLongRun: boolean;

    distances: Map<DistanceType, number> = new Map<DistanceType, number>();

    constructor(weekNumber: number) {
        this.weekNumber = weekNumber;
    }
}
