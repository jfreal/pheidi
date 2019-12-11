import { DayType } from './day-type.enum'
import { DistanceType } from './distance-type.enum'

export class DayConfig {
    constructor(distanceType: DistanceType, dayType: DayType) {
        this.distanceType = distanceType;
        this.dayType = dayType;
    }

    distanceType: DistanceType
    dayType: DayType
}
