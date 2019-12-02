import { Component, OnInit } from '@angular/core';
import { TrainingPlan } from './training-plan'

@Component({
  selector: 'app-plan-generator',
  templateUrl: './plan-generator.component.html',
  styleUrls: ['./plan-generator.component.sass']
})
export class PlanGeneratorComponent implements OnInit {
  trainingPlan: TrainingPlan;

  constructor() { }

  ngOnInit() {

    this.trainingPlan = new TrainingPlan();
    this.trainingPlan.GeneratePlanWeeks();

  }
}