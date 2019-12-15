import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { DistanceTypeSelectorComponent } from './distance-type-selector.component';

describe('DistanceTypeSelectorComponent', () => {
  let component: DistanceTypeSelectorComponent;
  let fixture: ComponentFixture<DistanceTypeSelectorComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ DistanceTypeSelectorComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DistanceTypeSelectorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
