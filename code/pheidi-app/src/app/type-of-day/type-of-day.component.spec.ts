import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { TypeOfDayComponent } from './type-of-day.component';

describe('TypeOfDayComponent', () => {
  let component: TypeOfDayComponent;
  let fixture: ComponentFixture<TypeOfDayComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ TypeOfDayComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(TypeOfDayComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
