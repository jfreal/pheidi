import { Day } from './day'

export class Week {
    days: Day[];
    weekNumber: number;
    lastLongRun: boolean;

    constructor(weekNumber: number) {
        this.weekNumber = weekNumber;
        this.days = [
            new Day(),
            new Day(),
            new Day(),
            new Day(),
            new Day(),
            new Day(),
            new Day()
        ];
    }
}
