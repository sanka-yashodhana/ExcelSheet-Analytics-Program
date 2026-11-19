import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EmployeeKpiCardsComponent } from './employee-kpi-cards.component';

describe('EmployeeKpiCardsComponent', () => {
  let component: EmployeeKpiCardsComponent;
  let fixture: ComponentFixture<EmployeeKpiCardsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EmployeeKpiCardsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(EmployeeKpiCardsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
