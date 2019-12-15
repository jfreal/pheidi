import { Component, OnInit, Input } from '@angular/core';
import { DayType } from '../day-type.enum';
import { Week } from '../week';
import { DayConfig } from '../day-config';
import { DistanceType } from '../distance-type.enum';

@Component({
  selector: 'app-day-summary',
  templateUrl: './day-summary.component.html',
  styleUrls: ['./day-summary.component.sass']
})
export class DaySummaryComponent implements OnInit {

  public DayType = DayType;
  public DistanceType = DistanceType;

  @Input() dayConfig: DayConfig;
  @Input() week: Week;
  @Input() dayIndex: number;

  constructor() { }

  ngOnInit() {
  }

}
