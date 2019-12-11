import { Component, OnInit, Input } from '@angular/core';
import { DayType } from '../day-type.enum';
import { DayTypeIconMapping } from '../day-type-icon-mapping';

@Component({
  selector: 'app-type-of-day',
  templateUrl: './type-of-day.component.html',
  styleUrls: ['./type-of-day.component.sass']
})
export class TypeOfDayComponent implements OnInit {

  public DayTypeIconMapping = DayTypeIconMapping;

  public dayTypes = Object.values(DayType);

  @Input() selectedDayType: DayType;

  constructor() { }

  ngOnInit() {
  }

}
