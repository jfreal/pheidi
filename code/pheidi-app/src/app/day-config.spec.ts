import { DayConfig } from './day-config';
import { DistanceType } from './distance-type.enum';
import { DayType } from './day-type.enum';

describe('DayConfig', () => {
  it('should create an instance', () => {
    expect(new DayConfig(DistanceType.Long, DayType.Run)).toBeTruthy();
  });
});
