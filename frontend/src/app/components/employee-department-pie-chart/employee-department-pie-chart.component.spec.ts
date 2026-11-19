import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EmployeeDepartmentPieChartComponent } from './employee-department-pie-chart.component';

describe('EmployeeDepartmentPieChartComponent', () => {
  let component: EmployeeDepartmentPieChartComponent;
  let fixture: ComponentFixture<EmployeeDepartmentPieChartComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EmployeeDepartmentPieChartComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(EmployeeDepartmentPieChartComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
