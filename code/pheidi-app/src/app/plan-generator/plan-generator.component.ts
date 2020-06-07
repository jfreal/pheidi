import { Component, OnInit } from '@angular/core';
import { TrainingPlan } from '../training-plan'
import { DayConfig } from '../day-config';
import { DayType } from '../day-type.enum';
import { DistanceType } from '../distance-type.enum';

@Component({
  selector: 'app-plan-generator',
  templateUrl: './plan-generator.component.html',
  styleUrls: ['./plan-generator.component.sass']
})
export class PlanGeneratorComponent implements OnInit {
  trainingPlan: TrainingPlan;

  constructor() { }

  changeDayType(dayIndex, dayType) {

    if (dayType === DayType.Rest) {
      this.trainingPlan.dayConfigs[dayIndex].distanceType = DistanceType.None;
    }

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