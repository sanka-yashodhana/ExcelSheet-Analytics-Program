import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EmployeeHiringTrendsChartComponent } from './employee-hiring-trends-chart.component';

describe('EmployeeHiringTrendsChartComponent', () => {
  let component: EmployeeHiringTrendsChartComponent;
  let fixture: ComponentFixture<EmployeeHiringTrendsChartComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EmployeeHiringTrendsChartComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(EmployeeHiringTrendsChartComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
