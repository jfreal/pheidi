import { TrainingPlan } from './training-plan';

describe('TrainingPlan', () => {
  it('should create an instance', () => {
    expect(new TrainingPlan()).toBeTruthy();
  });
});

describe('TrainingPlan Weeks', () => {
  it('should have week array with the number of weeks', () => {
    let trainingPlan = new TrainingPlan();

    trainingPlan.weeks = 8;

    var weeks = trainingPlan.Weeks();

    expect(weeks.length).toBe(8);
  });
});
