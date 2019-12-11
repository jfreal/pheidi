import { Day } from './day'

export class Week {
    days: Day[];
    weekNumber: number;
    lastLongRun: boolean;

    longRunDistance: number;
    halfRunDistance: number;
    quarterRunDistance: number;

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
