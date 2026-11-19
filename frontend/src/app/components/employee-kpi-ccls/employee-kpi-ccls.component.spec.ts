import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EmployeeKpiCclsComponent } from './employee-kpi-ccls.component';

describe('EmployeeKpiCclsComponent', () => {
  let component: EmployeeKpiCclsComponent;
  let fixture: ComponentFixture<EmployeeKpiCclsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EmployeeKpiCclsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(EmployeeKpiCclsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
