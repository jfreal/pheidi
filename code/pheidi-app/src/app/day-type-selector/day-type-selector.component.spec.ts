import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { DayTypeSelectorComponent } from './day-type-selector.component';

describe('DayTypeSelectorComponent', () => {
  let component: DayTypeSelectorComponent;
  let fixture: ComponentFixture<DayTypeSelectorComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ DayTypeSelectorComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DayTypeSelectorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
