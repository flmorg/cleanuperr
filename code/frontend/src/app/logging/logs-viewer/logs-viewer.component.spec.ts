import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LogsViewerComponent } from './logs-viewer.component';

describe('LogsViewerComponent', () => {
  let component: LogsViewerComponent;
  let fixture: ComponentFixture<LogsViewerComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LogsViewerComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LogsViewerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
