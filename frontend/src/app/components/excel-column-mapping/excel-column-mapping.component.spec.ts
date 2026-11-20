import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ExcelColumnMappingComponent } from './excel-column-mapping.component';

describe('ExcelColumnMappingComponent', () => {
  let component: ExcelColumnMappingComponent;
  let fixture: ComponentFixture<ExcelColumnMappingComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ExcelColumnMappingComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ExcelColumnMappingComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
