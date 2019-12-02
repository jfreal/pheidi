import { Week } from './week';

export class TrainingPlan {
    marathonDate: Date;
    numberOfWeeks: number = 18;
    generatedWeeks: Week[];
    weeksOfTaper: number = 3;
    lastLongRunDistance: number = 20;
    longRunDay: number = 6;
    minimumRunDistance: number = 3;
    GeneratePlanWeeks() {

        let weeks: Array<Week> = [];

        let lastLongRunWeek = this.numberOfWeeks - this.weeksOfTaper;

        console.log(this.numberOfWeeks)

        for (let i = this.numberOfWeeks - 1; i >= 0; i--) {

            var week = new Week(i + 1)

            let nextWeek = weeks.filter((w) => w.weekNumber == week.weekNumber + 1)[0];

            let weeksUntilLongRun = lastLongRunWeek - week.weekNumber;

            let longWeek = weeksUntilLongRun % 2 == 0;

            // -1 is marathon week
            if (week.weekNumber == lastLongRunWeek) {
                week.lastLongRun = true;
                week.days[6].distance = this.lastLongRunDistance;
            } else {

                if (longWeek && week.weekNumber < lastLongRunWeek) {
                    week.days[6].distance = this.lastLongRunDistance - weeksUntilLongRun;
                }

                //longest run generation
                if (!longWeek && week.weekNumber < lastLongRunWeek) {
                    let recoveryDistance = 6;

                    if (week.weekNumber < this.numberOfWeeks / 2) {
                        recoveryDistance = 5
                    }

                    week.days[6].distance = nextWeek.days[6].distance - recoveryDistance;
                }
            }

            //mid week generation
            if (week.weekNumber == lastLongRunWeek) {
                week.days[2].distance = Math.ceil((this.lastLongRunDistance / 2));
                week.days[1].distance = Math.max(Math.ceil(this.lastLongRunDistance / 2 / 2), this.minimumRunDistance);
                week.days[3].distance = Math.max(Math.ceil(this.lastLongRunDistance / 2 / 2), this.minimumRunDistance);
            } else {
                week.days[2].distance = Math.ceil((week.weekNumber / 2) + 2);
                week.days[1].distance = Math.max(Math.floor(week.days[2].distance / 2), this.minimumRunDistance);
                week.days[3].distance = Math.max(Math.ceil(week.days[2].distance / 2), this.minimumRunDistance);
            }

            weeks.unshift(week);
        }

        for (let i = 0; i < this.weeksOfTaper; i++) {

            let halfOfTheLongestRun = (this.lastLongRunDistance / 2);
            let midWeekRun = halfOfTheLongestRun - ((i + 1) * 2);

            console.log(midWeekRun);

            // var taperDistance = Math.floor(lastLongRunWeek / 2) + 1;

            // let indexOfWeekToClone = Math.floor((lastLongRunWeek / 2) - 1);
            // var weekToClone = weeks[indexOfWeekToClone];

            weeks[this.numberOfWeeks - i - 1].days[2].distance = midWeekRun;
        }

        if (this.marathonDate) {
            let now = new Date();
            now.setDate(this.marathonDate.getDate() - this.numberOfWeeks * 7);
        }

        this.generatedWeeks = weeks;
    }
}
