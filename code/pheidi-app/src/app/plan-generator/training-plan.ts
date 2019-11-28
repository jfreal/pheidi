import { Week } from './week';

export class TrainingPlan {
    marathonDate: Date;
    weeks: number;
    Weeks(): Week[] {

        let weeks: Array<Week> = [];

        for (let i = 0; i < this.weeks; i++) {
            weeks.push(new Week());
        }

        if (this.marathonDate) {
            let now = new Date();
            now.setDate(this.marathonDate.getDate() - this.weeks * 7);
        }

        return weeks;
    }
}
