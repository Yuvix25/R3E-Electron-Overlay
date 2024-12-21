import HudElement, {Hide} from "./HudElement.js";
import {ESessionPhase} from "../r3eTypes.js";
import {IExtendedDriverData, getUid} from "../utils.js";
import {RELATIVE_LENGTH, halfLengthTop, halfLengthBottom, insertCell, NA, nameFormat, getClassColors, finishedBadly} from "../consts.js";
import RankedData from "../actions/RankedData.js";
import {SharedMemoryKey} from '../SharedMemoryConsumer.js';


export default class RelativeViewer extends HudElement {
    override sharedMemoryKeys: SharedMemoryKey[] = ['driverData', 'position', 'layoutLength', 'sessionPhase', '+deltasAhead', '+deltasBehind'];

    public static readonly IMAGE_REDIRECT = 'https://game.raceroom.com/store/image_redirect?id=';

    private rankedData: RankedData = null;

    protected override onHud(): void {
        this.rankedData = this.hud.rankedDataService;
    }

    protected override render(allDrivers: IExtendedDriverData[], place: number, trackLength: number, phase: ESessionPhase, deltasAhead: Record<string, number>, deltasBehind: Record<string, number>): null | Hide {
        const relative = document.getElementById('relative-viewer');
        const relativeTable = relative.getElementsByTagName('tbody')[0];

        // 1 - garage, 2 - gridwalk, 3 - formation, 4 - countdown, 5 - green flag, 6 - checkered flag
        if (phase < 3) {
            relativeTable.innerHTML = '';
            return this.hide();
        }

        if (allDrivers == null || place == null || allDrivers.length <= 1)
            return this.hide();

        let driverCount = allDrivers.length;

        let classes = new Set<number>();
        for (const driver of allDrivers) {
            classes.add(driver.driverInfo.classId);
        }

        relative.style.display = 'block';

        const classColors = getClassColors(allDrivers);

        let myUid: string = null;
        let myDriver: IExtendedDriverData = null;
        for (const driver of allDrivers) {
            const uid = getUid(driver.driverInfo);
            driver.driverInfo.uid = uid;

            if (driver.place == place) {
                myUid = uid;
                myDriver = driver;
            }
        }

        if (myUid == null || myDriver == null)
            return this.hide();

        const deltasAheadArr: Array<[IExtendedDriverData, number]> = [];
        const deltasBehindArr: Array<[IExtendedDriverData, number]> = [];
        for (const driver of allDrivers) {
            if (driver === myDriver) continue;
            if (finishedBadly(driver.finishStatus)) {
                driverCount--;
                continue;
            }

            const uid = driver.driverInfo.uid;

            if (deltasAhead[uid] != null) {
                deltasAheadArr.push([driver, deltasAhead[uid]]);
            } else if (deltasBehind[uid] != null) {
                deltasBehindArr.push([driver, deltasBehind[uid]]);
            }
        }

        deltasAheadArr.sort((a, b) => {
            return this.getDistanceToDriverAhead(trackLength, myDriver, a[0]) - this.getDistanceToDriverAhead(trackLength, myDriver, b[0]);
        });
        deltasBehindArr.sort((a, b) => {
            return this.getDistanceToDriverAhead(trackLength, myDriver, b[0]) - this.getDistanceToDriverAhead(trackLength, myDriver, a[0]);
        });

        deltasAheadArr.push([myDriver, 0]);

        let start = 0, end = RELATIVE_LENGTH;
        if (deltasAheadArr.length - 1 >= halfLengthTop && deltasBehindArr.length >= halfLengthBottom) {
            start = deltasAheadArr.length - halfLengthTop - 1; // -1 because we added the current driver
            end = deltasAheadArr.length + halfLengthBottom;
        } else if (deltasAheadArr.length - 1 < halfLengthTop) {
            start = 0;
            end = Math.min(driverCount, RELATIVE_LENGTH);
        } else if (deltasBehindArr.length < halfLengthBottom) {
            start = Math.max(0, driverCount - RELATIVE_LENGTH);
            end = driverCount;
        }

        const mergedDeltas = [...deltasAheadArr, ...deltasBehindArr];

        if (mergedDeltas.length <= 5)
            this.root.style.setProperty('--relative-view-row-height', '40px');
        else
            this.root.style.setProperty('--relative-view-row-height', 'auto');

        let zeroDeltaCount = 0;
        for (let i = start; i < end; i++) {
            if (mergedDeltas[i] == undefined)
                break;
            const driver = mergedDeltas[i][0];

            if (driver.place == -1) break;

            const row = relativeTable.children.length > i - start ? relativeTable.children[i - start] : relativeTable.insertRow(i - start);
            row.classList.remove('last-row')
            if (!(row instanceof HTMLTableRowElement)) {
                console.error('something went wrong while creating the relative table, row is not an HTMLElement');
                return this.hide();
            }
            row.dataset.classIndex = driver.driverInfo.classPerformanceIndex.toString();

            if (driver === myDriver) {
                row.style.backgroundColor = 'rgba(255, 255, 0, 0.4)';
            } else {
                row.style.backgroundColor = '';
            }

            const classId = driver.driverInfo.classId;
            const manafacturerId = driver.driverInfo.manufacturerId;
            const classImgCell = insertCell(row, undefined, 'class-img');
            let classImg = classImgCell.children?.[0]?.children?.[0];
            if (classImg == null) {
                classImg = document.createElement('div');
                classImgCell.appendChild(classImg);
                classImg = document.createElement('img');
                classImgCell.children[0].appendChild(classImg);
            }
            if (!(classImg instanceof HTMLImageElement)) {
                console.error('something went wrong while creating the relative table, classImg is not an HTMLElement');
                return this.hide();
            }

            const newSrc = classes.size > 1 ? RelativeViewer.IMAGE_REDIRECT + classId + '&size=thumb' : RelativeViewer.IMAGE_REDIRECT + manafacturerId + '&size=small';
            if (classImg.src !== newSrc) {
                classImg.src = newSrc;
            }

            const classColor = classColors.get(driver.driverInfo.classPerformanceIndex);
            const colorCell = insertCell(row, undefined, 'class-color');
            colorCell.style.backgroundColor = classColor;

            insertCell(row, driver.placeClass.toString(), 'place-class');

            const name = nameFormat(driver.driverInfo.name)
            insertCell(row, name, 'name');

            let carName = '';
            if (this.hud.r3eData != null) {
                const car = this.hud.r3eData.cars[driver.driverInfo.modelId];
                if (car != null) {
                    carName = car.Name;
                }
            }
            insertCell(row, carName, 'car-name');

            const rankedData = this.rankedData.getRankedData(driver.driverInfo.userId);
            const rating = rankedData?.Rating.toFixed(0) ?? '1500';
            const reputation = rankedData?.Reputation.toFixed(0) ?? '70';
            insertCell(row, rating + '/' + reputation, 'ranked');

            const pitCell = insertCell(row, 'PIT', 'pit');
            pitCell.style.opacity = driver.inPitlane ? '1' : '0';

            const deltaRaw = mergedDeltas[i][1];
            if (typeof deltaRaw === 'number' && deltaRaw.toFixed(1) === '0.0')
                zeroDeltaCount++;
            const delta = driver.place == place ? '' : (deltaRaw == null ? NA : deltaRaw.toFixed(1));
            insertCell(row, delta, 'time-delta');
        }
        if (zeroDeltaCount >= 3) {
            console.warn('relative 0.0 bug detected');
        }

        while (relativeTable.children.length > end - start) {
            relativeTable.deleteRow(relativeTable.children.length - 1);
        }

        const lastRow = relativeTable.insertRow(relativeTable.children.length);
        lastRow.classList.add('last-row');

        return null;
    }

    private getDistanceToDriverAhead(trackLength: number, driver: IExtendedDriverData, driverAhead: IExtendedDriverData): number {
        let distance = driverAhead.lapDistance - driver.lapDistance;

        if (distance < 0) {
            distance += trackLength;
        }

        return distance;
    }
}