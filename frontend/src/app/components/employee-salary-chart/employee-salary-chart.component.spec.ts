import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EmployeeSalaryChartComponent } from './employee-salary-chart.component';

describe('EmployeeSalaryChartComponent', () => {
  let component: EmployeeSalaryChartComponent;
  let fixture: ComponentFixture<EmployeeSalaryChartComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EmployeeSalaryChartComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(EmployeeSalaryChartComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
