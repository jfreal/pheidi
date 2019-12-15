import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { PlanGeneratorComponent } from './plan-generator/plan-generator.component';
import { DaySummaryComponent } from './day-summary/day-summary.component';
import { DayTypeSelectorComponent } from './day-type-selector/day-type-selector.component';
import { DistanceTypeSelectorComponent } from './distance-type-selector/distance-type-selector.component'

@NgModule({
  declarations: [
    AppComponent,
    PlanGeneratorComponent,
    DayTypeSelectorComponent,
    DaySummaryComponent,
    DayTypeSelectorComponent,
    DistanceTypeSelectorComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    FormsModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
