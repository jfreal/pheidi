import { Week } from './week';
import { DayConfig } from './day-config';
import { DayType } from './day-type.enum';
import { DistanceType } from './distance-type.enum';

export class TrainingPlan {

    dayConfigs: DayConfig[];
    marathonDate: Date;
    numberOfWeeks: number = 18;
    generatedWeeks: Week[];
    weeksOfTaper: number = 3;
    lastLongRunDistance: number = 20;
    longRunDay: number = 6;
    minimumRunDistance: number = 3;

    constructor() {
        this.dayConfigs = [
            new DayConfig(DistanceType.None, DayType.Rest),
            new DayConfig(DistanceType.Quarter, DayType.Run),
            new DayConfig(DistanceType.Half, DayType.Run),
            new DayConfig(DistanceType.Quarter, DayType.Run),
            new DayConfig(DistanceType.None, DayType.Rest),
            new DayConfig(DistanceType.Long, DayType.Run),
            new DayConfig(DistanceType.None, DayType.Rest)
        ]
    }


    GeneratePlanWeeks() {

        let weeks: Array<Week> = [];

        let lastLongRunWeek = this.numberOfWeeks - this.weeksOfTaper;

        for (let i = this.numberOfWeeks - 1; i >= 0; i--) {

            var week = new Week(i + 1)

            let nextWeek = weeks.filter((w) => w.weekNumber == week.weekNumber + 1)[0];

            let weeksUntilLongRun = lastLongRunWeek - week.weekNumber;

            let longWeek = weeksUntilLongRun % 2 == 0;

            // -1 is marathon week
            if (week.weekNumber == lastLongRunWeek) {
                week.lastLongRun = true;
                week.distances.set(DistanceType.Long, this.lastLongRunDistance);
            } else {

                if (longWeek && week.weekNumber < lastLongRunWeek) {
                    week.distances.set(DistanceType.Long, this.lastLongRunDistance - weeksUntilLongRun);
                }

                //longest run generation
                if (!longWeek && week.weekNumber < lastLongRunWeek) {
                    let recoveryDistance = 6;

                    if (week.weekNumber < this.numberOfWeeks / 2) {
                        recoveryDistance = 5
                    }

                    console.log(week.weekNumber)

                    week.distances.set(DistanceType.Long, nextWeek.distances.get(DistanceType.Long) - recoveryDistance);
                }
            }

            //mid week generation
            if (week.weekNumber == lastLongRunWeek) {
                week.distances.set(DistanceType.Half, Math.ceil((this.lastLongRunDistance / 2)));
                week.distances.set(DistanceType.Quarter, Math.max(Math.ceil(this.lastLongRunDistance / 2 / 2), this.minimumRunDistance));
            } else {

                let incrementedHalf = Math.ceil((week.weekNumber / 2) + 2);

                week.distances.set(DistanceType.Half, incrementedHalf);
                week.distances.set(DistanceType.Quarter, Math.max(Math.floor(incrementedHalf / 2), this.minimumRunDistance));
                week.distances.set(DistanceType.QuarterUp, Math.max(Math.ceil(incrementedHalf / 2), this.minimumRunDistance));
            }

            console.log(week.distances);
            weeks.unshift(week);
        }

        let halfWay: number = this.numberOfWeeks - this.weeksOfTaper;
        for (let i = 0; i < this.weeksOfTaper; i++) {

            let halfOfTheLongestRun = (this.lastLongRunDistance / 2);

            let midWeekRun = halfOfTheLongestRun - ((i + 1) * 2);

            // var taperDistance = Math.floor(lastLongRunWeek / 2) + 1;

            // let indexOfWeekToClone = Math.floor((lastLongRunWeek / 2) - 1);
            // var weekToClone = weeks[indexOfWeekToClone];

            halfWay = Math.floor(halfWay / 2);

            let halfWayWeek = weeks[halfWay - 1];

            let taperWeek = weeks[this.numberOfWeeks - this.weeksOfTaper + i];

            taperWeek.taper = true;

            // week.distances.set(DistanceType.Half, midWeekRun);
            // week.distances.set(DistanceType.QuarterUp, Math.floor(midWeekRun / 2) + 1);
            // week.distances.set(DistanceType.Quarter, Math.floor(midWeekRun / 2));

            taperWeek.distances.set(DistanceType.Long, halfWayWeek.distances.get(DistanceType.Long));
        }

        if (this.marathonDate) {
            let now = new Date();
            now.setDate(this.marathonDate.getDate() - this.numberOfWeeks * 7);
        }

        this.generatedWeeks = weeks;
    }
}
