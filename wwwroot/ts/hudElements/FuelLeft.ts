import HudElement from "./HudElement.js";
import {valueIsValidAssertNull, NA} from "../consts.js";
import {EEngineType, IDriverInfo} from '../r3eTypes.js';

export default class FuelLeft extends HudElement {
  override sharedMemoryKeys: string[] = ['vehicleInfo', 'fuelLeft', 'batterySoC'];

  protected override render(vehicleInfo: IDriverInfo, fuelLeft: number, battery: number): string {
    if (vehicleInfo.engineType === EEngineType.Electric) {
        fuelLeft = battery;
    }
    return valueIsValidAssertNull(fuelLeft) ? `${fuelLeft.toFixed(1)}` : NA;
  }
}