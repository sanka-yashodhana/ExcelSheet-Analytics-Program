import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EmployeeDepartmentChartComponent } from './employee-department-chart.component';

describe('EmployeeDepartmentChartComponent', () => {
  let component: EmployeeDepartmentChartComponent;
  let fixture: ComponentFixture<EmployeeDepartmentChartComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EmployeeDepartmentChartComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(EmployeeDepartmentChartComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
