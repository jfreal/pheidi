import { TrainingPlan } from './training-plan';

describe('TrainingPlan', () => {
  it('should create an instance', () => {
    expect(new TrainingPlan()).toBeTruthy();
  });

  it('should have week array with the number of weeks', () => {
    let trainingPlan = new TrainingPlan();

    trainingPlan.GeneratePlanWeeks();

    expect(trainingPlan.generatedWeeks.length).toBe(18);
  });

  it('generated weeks change when we set the number of weeks', () => {
    let trainingPlan = new TrainingPlan();

    trainingPlan.numberOfWeeks = 6;

    trainingPlan.GeneratePlanWeeks();

    expect(trainingPlan.generatedWeeks.length).toBe(6);
  });

  it('the last week before taping has the longest distance', () => {
    let trainingPlan = new TrainingPlan();

    trainingPlan.numberOfWeeks = 100;
    trainingPlan.weeksOfTaper = 20;
    trainingPlan.lastLongRunDistance = 99;
    trainingPlan.GeneratePlanWeeks();

    expect(trainingPlan.generatedWeeks[79].days[6]).toBe(99);
  });
});
