import HudElement, {Style} from "./HudElement.js";
import {laptimeFormat, valueIsValidAssertUndefined} from "../consts.js";
import {SharedMemoryKey} from '../SharedMemoryConsumer.js';

export default class CurrentLaptime extends HudElement {
    override sharedMemoryKeys: SharedMemoryKey[] = ['lapTimeCurrentSelf', '+currentLaptime'];

    protected override render(laptime: number, currentLaptime: number): Style {
        if (!valueIsValidAssertUndefined(laptime)) {
            const currentTime = currentLaptime;
            return this.style(laptimeFormat(currentTime, true), {
                color: 'red',
            });
        }
        return this.style(laptimeFormat(laptime, true), {
            color: 'white',
        });
    }
}
