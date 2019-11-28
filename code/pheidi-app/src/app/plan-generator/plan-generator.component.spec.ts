import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PlanGeneratorComponent } from './plan-generator.component';

describe('PlanGeneratorComponent', () => {
  let component: PlanGeneratorComponent;
  let fixture: ComponentFixture<PlanGeneratorComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PlanGeneratorComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PlanGeneratorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
