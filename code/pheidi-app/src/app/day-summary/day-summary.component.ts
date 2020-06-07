import { Component, OnInit, Input, OnChanges, SimpleChanges } from '@angular/core';
import { DayType } from '../day-type.enum';
import { Week } from '../week';
import { DayConfig } from '../day-config';
import { DistanceType } from '../distance-type.enum';
import { FormControl } from '@angular/forms';

@Component({
  selector: 'app-day-summary',
  templateUrl: './day-summary.component.html',
  styleUrls: ['./day-summary.component.sass']
})
export class DaySummaryComponent implements OnInit, OnChanges {

  public dayControl = new FormControl('');

  public DayType = DayType;
  public DistanceType = DistanceType;

  @Input() week: Week;
  @Input() dayIndex: number;
  @Input() dayConfig: DayConfig;
  @Input() distanceType: DistanceType;
  @Input() dayType: DayType;

  constructor() { }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes.distanceType) {
      console.log('distance!')
    }


  }

  ngOnInit() {
    this.dayControl.setValue(this.week.distances[this.dayConfig.distanceType]);
  }

}
