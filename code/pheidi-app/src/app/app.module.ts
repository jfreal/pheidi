import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { PlanGeneratorComponent } from './plan-generator/plan-generator.component';
import { TypeOfDayComponent } from './type-of-day/type-of-day.component'

@NgModule({
  declarations: [
    AppComponent,
    PlanGeneratorComponent,
    TypeOfDayComponent
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
