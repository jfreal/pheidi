import { Component, OnInit } from '@angular/core';
import { TrainingPlan } from '../training-plan'
import { DayConfig } from '../day-config';

@Component({
  selector: 'app-plan-generator',
  templateUrl: './plan-generator.component.html',
  styleUrls: ['./plan-generator.component.sass']
})
export class PlanGeneratorComponent implements OnInit {
  trainingPlan: TrainingPlan;

  constructor() { }

  changeDayType(dayIndex, dayType) {
    this.trainingPlan.dayConfigs[dayIndex].dayType = dayType;
  }

  changeDistanceType(dayIndex, distanceType) {
    this.trainingPlan.dayConfigs[dayIndex].distanceType = distanceType;
  }

  ngOnInit() {
    this.trainingPlan = new TrainingPlan();
    this.trainingPlan.GeneratePlanWeeks();
  }
}