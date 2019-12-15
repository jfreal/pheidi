import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { DayType } from '../day-type.enum';
import { DayTypeIconMapping } from '../day-type-icon-mapping';

@Component({
  selector: 'app-day-type-selector',
  templateUrl: './day-type-selector.component.html',
  styleUrls: ['./day-type-selector.component.sass']
})
export class DayTypeSelectorComponent implements OnInit {


  public DayTypeIconMapping = DayTypeIconMapping;

  public dayTypes = Object.values(DayType);

  @Output() onDayTypeChanged = new EventEmitter<DayType>();

  @Input() selectedDayType: DayType;

  constructor() { }

  ngOnInit() {
  }

  changeDayType(dayType: DayType) {

    this.selectedDayType = dayType;
    this.onDayTypeChanged.emit(dayType);
    console.log(dayType);
  }

}
