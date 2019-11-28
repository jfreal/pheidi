import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { PlanGeneratorComponent } from './plan-generator/plan-generator.component'

const routes: Routes = [
  { path: '', component: PlanGeneratorComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
