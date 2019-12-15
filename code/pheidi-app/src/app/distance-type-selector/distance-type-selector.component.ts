import { Component, OnInit, EventEmitter, Input, Output } from '@angular/core';
import { DistanceType } from '../distance-type.enum';
import { DistanceTypeLabelMapping } from '../distance-type-label-mapping';

@Component({
  selector: 'app-distance-type-selector',
  templateUrl: './distance-type-selector.component.html',
  styleUrls: ['./distance-type-selector.component.sass']
})
export class DistanceTypeSelectorComponent implements OnInit {

  public DistanceTypeLabelMapping = DistanceTypeLabelMapping;

  public distanceTypes = Object.values(DistanceType);

  @Output() onDistanceTypeChanged = new EventEmitter<DistanceType>();

  @Input() selectedDistanceType: DistanceType;

  constructor() { }

  ngOnInit() {
  }

  changeDistanceType(distanceType: DistanceType) {

    this.selectedDistanceType = distanceType;
    this.onDistanceTypeChanged.emit(distanceType);
    console.log(distanceType);
  }
}
