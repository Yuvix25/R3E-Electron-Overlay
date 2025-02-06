import {base64EncodedUint8ArrayToString} from './consts.js';
import Hud from './Hud.js';
import { IDriverData, IDriverInfo } from './r3eTypes.js';
import { RawSourceMap, SourceMapConsumer } from 'source-map-js';

export class DeltaManager {
  static DELTA_WINDOW: number = 10; // seconds
  static DELTA_OF_DELTA_MULTIPLIER: number = 1.5;
  static deltaWindow: Array<[number, number]> = [];

  /**
   * Add a delta to the delta window.
   */
  static addDelta(delta: number) {
    const time = Hud.getGameTimestamp();
    DeltaManager.deltaWindow.push([time, delta]);
    while (DeltaManager.deltaWindow[0][0] < time - DeltaManager.DELTA_WINDOW)
      DeltaManager.deltaWindow.shift();
  }

  static getLastDelta(): [number, number] {
    if (DeltaManager.deltaWindow.length == 0)
      return null;
    return DeltaManager.deltaWindow[DeltaManager.deltaWindow.length - 1];
  }

  /**
   * Get some sort of delta of the delta itself during the window.
   */
  static getDeltaOfDeltas(mult?: number): number {
    if (DeltaManager.deltaWindow.length == 0)
      return 0;


    const lastDelta = this.getLastDelta()[1]
    const lastTime = this.getLastDelta()[0];

    const timeRange = lastTime - DeltaManager.deltaWindow[0][0];

    let res = 0;
    let weight = 0;
    for (let i = DeltaManager.deltaWindow.length - 1; i >= 0; i--) {
      const deltaData = DeltaManager.deltaWindow[i];
      const deltaDelta = lastDelta - deltaData[1];
      const deltaWeight = (timeRange - (lastTime - deltaData[0])) / timeRange;
      res += deltaDelta * deltaWeight;
      weight += deltaWeight;
    }
    res = res / weight * (mult ?? DeltaManager.DELTA_OF_DELTA_MULTIPLIER);
    return res / (Math.abs(res) + 0.5);
  }

  /**
   * Clear the delta window.
   */
  static clear() {
    DeltaManager.deltaWindow = [];
  }
}

export interface IExtendedDriverInfo extends IDriverInfo {
  uid?: string;
}

export interface IExtendedDriverData extends IDriverData {
  driverInfo: IExtendedDriverInfo;
}

/**
 * @param driverInfo - DriverData[x].DriverInfo
 * @return Unique ID for the driver (JSON of some fields)
 */
export function computeUid(driverInfo: IDriverInfo): string {
  if (driverInfo == null) {
    return null;
  }
  return `${base64EncodedUint8ArrayToString(driverInfo.name)}_${driverInfo.userId}_${driverInfo.slotId}_${driverInfo.liveryId}`;
}

export function getUid(driverInfo: IDriverInfo): string {
  return (driverInfo as IExtendedDriverInfo)?.uid ?? computeUid(driverInfo);
}

export function getRadarPointerRotation(d: number, x: number, z: number): number {
  const angle = Math.acos(Math.abs(z) / d);
  if(x > 0 && z > 0) {
    return angle;
  } else if (x > 0 && z < 0) {
    return (-angle);
  } else if (x < 0 && z > 0) {
    return (-angle);
  } else {
    return angle;
  }
}

export type Vector = { x: number; y: number; z: number; };

export function vectorSubtract(a: Vector, b: Vector): Vector {
  const res = {
    x: a.x - b.x,
    y: a.y - b.y,
    z: a.z - b.z,
  }
  return res;
}

export function distanceFromZero(a: Vector): number {
  return Math.sqrt(a.x ** 2 + a.z ** 2);
}

/**
 * Get a rotation matrix from the given eular angles - inverted.
 */
export function rotationMatrixFromEular(eular: Vector): Array<Array<number>> {
  const x = -eular.x;
  const y = -eular.y;
  const z = -eular.z;

  const c1 = Math.cos(x);
  const s1 = Math.sin(x);
  const c2 = Math.cos(y);
  const s2 = Math.sin(y);
  const c3 = Math.cos(z);
  const s3 = Math.sin(z);

  return [
    [c2 * c3, -c2 * s3, s2],
    [c1 * s3 + c3 * s1 * s2, c1 * c3 - s1 * s2 * s3, -c2 * s1],
    [s1 * s3 - c1 * c3 * s2, c3 * s1 + c1 * s2 * s3, c1 * c2]
  ];
}

/**
 * Rotate the given vector by the given matrix.
 */
export function rotateVector(matrix: Array<Array<number>>, vector: Vector): Vector {
  return {
    x: matrix[0][0] * vector.x + matrix[0][1] * vector.y + matrix[0][2] * vector.z,
    y: matrix[1][0] * vector.x + matrix[1][1] * vector.y + matrix[1][2] * vector.z,
    z: matrix[2][0] * vector.x + matrix[2][1] * vector.y + matrix[2][2] * vector.z,
  };
}


export function mpsToKph(mps: number) {
  return mps * 3.6;
}


export function getRaceDeltas(driverData: IDriverData[], position: number): string[] {
  
    const precision = 3;
    const deltas: string[] = [];
    const deltaLoop = (front: boolean) => {
        const push = (delta: string) => {
            front ? deltas.unshift(delta) : deltas.push(delta);
        }

        let delta = 0;
        const me = driverData[position];
        for (let i = position + (front ? -1 : 1); front ? i >= 0 : i < driverData.length; front ? i-- : i++) {
            const driver = driverData[i];

            let lapDiff = me.completedLaps - driver.completedLaps;
            if (lapDiff < 0 && driver.lapDistance < me.lapDistance) {
                lapDiff++;
            } else if (lapDiff > 0 && me.lapDistance < driver.lapDistance) {
                lapDiff--;
            }

            if (lapDiff != 0) {
                push(lapDiff > 0 ? `+${lapDiff}L` : `${lapDiff}L`);
            } else {
                let nextDelta = front ? driver.timeDeltaBehind : driver.timeDeltaFront;
                if (nextDelta < 0) {
                    front ? deltas.unshift(null) : deltas.push(null);
                    continue;
                }
                delta += nextDelta;
                push(front ? (-delta).toFixed(precision) : '+' + delta.toFixed(precision));
            }
        }
    }

    deltaLoop(true);

    deltas.push(null);

    deltaLoop(false);

    return deltas;
}

export class AudioController {
  audio: HTMLAudioElement = new Audio();
  audioIsPlaying: boolean = false;
  audioContext: AudioContext = new AudioContext();
  mediaSource: MediaElementAudioSourceNode = this.audioContext.createMediaElementSource(this.audio);
  stereoPanner: StereoPannerNode = this.audioContext.createStereoPanner();

  minPlaybackRate: number;
  maxPlaybackRate: number;
  playbackRateMultiplier: number;
  volumeMultiplier: number;
  soundFileName: string;

  constructor({ minPlaybackRate = 0.1, maxPlaybackRate = 10, playbackRateMultiplier = 2, volumeMultiplier = 1, soundFileName = '' } = {}) {
    this.minPlaybackRate = minPlaybackRate;
    this.maxPlaybackRate = maxPlaybackRate;
    this.playbackRateMultiplier = playbackRateMultiplier;
    this.volumeMultiplier = volumeMultiplier;
    this.soundFileName = soundFileName;

    this.mediaSource.connect(this.stereoPanner);
    this.stereoPanner.connect(this.audioContext.destination);

    this.audio.src = `/sounds/${this.soundFileName}`;

    this.audio.onplaying = () => {
      this.audioIsPlaying = true;
    };
    this.audio.onended = () => {
      this.audioIsPlaying = false;
    };
  }

  setVolume(volume: number) {
    this.volumeMultiplier = volume;
  }

  /**
   * @param {number} amount - Controls the volume and playback rate of the audio (higher = louder and faster)
   * @param {number} pan - Controls the panning of the audio (negative = left, positive = right), between -1 and 1
   */
  play(amount: number, pan: number) {
    this.audio.volume = Math.max(0, Math.min(1, amount / 10 * this.volumeMultiplier));
    this.audio.playbackRate = Math.min(Math.max(this.minPlaybackRate, amount * this.playbackRateMultiplier), this.maxPlaybackRate);
    this.stereoPanner.pan.value = pan;

    if (this.audio.paused && this.audioContext.state !== 'suspended' && !this.audioIsPlaying) {
      this.audio.play().catch(() => { });
    }
  }
}

enum LogLevel {
  INFO = 'INFO',
  WARN = 'WARN',
  ERROR = 'ERROR',
}

export const realConsoleError = console.error;

const FLOOD_MAX_GAP = 0.7 * 1000;
const FLOOD_CLEAR_TIME = 15 * 1000;


class LogMessage {
  private messageIdentifier: string;
  private lastFullMessage: string;
  private floodCount: number;
  private sameFullMessage: boolean;
  private firstTime: number = 0;
  private lastTime: number = 0;

  constructor(
    messageIdentifier: string,
    fullMessage: string,
  ) {
    this.messageIdentifier = messageIdentifier;
    this.lastFullMessage = fullMessage;
    this.floodCount = 0;
    this.sameFullMessage = true;
  }

  /**
   * - floodCount = 0: first message - always log.
   * - floodCount = 1: second message - log if the time gap is big enough.
   * - floodCount > 1: third message and onwards - time gap was small enough once, so we consider this a flood from now until the next clear.
   * @param fullMessage
   * @return Whether the message should be logged (or if it's flooding)
   */
  log(fullMessage: string) {
    const now = Date.now();
    const gap = now - this.lastTime;
    this.lastTime = now;
    this.sameFullMessage = this.lastFullMessage == null || (this.sameFullMessage && fullMessage === this.lastFullMessage);
    this.lastFullMessage = fullMessage;

    if (this.floodCount === 0 || (this.floodCount === 1 && gap >= FLOOD_MAX_GAP)) {
      if (this.floodCount === 0) {
        this.firstTime = now;
        this.floodCount++
      }

      this.lastFullMessage = null;
      return true;
    }
    this.floodCount++;

    return false;
  }

  notFlooding() {
    return this.floodCount == 0;
  }

  getFloodCount() {
    return this.floodCount;
  }

  clear() {
    if (this.notFlooding()) {
      return null;
    }

    let message = this.sameFullMessage ? this.lastFullMessage : this.messageIdentifier;

    let res = null;

    if (this.floodCount == 2) {
      res = message;
    } else if (this.floodCount > 2) {
      res = `Message repeated ${this.floodCount-1} times: ${message}`; // -1 because the first message is already logged);
    }
    this.floodCount = 1;
    this.sameFullMessage = true;
    this.lastFullMessage = null;

    return res;
  }

  getFirstTimestamp() {
    return this.firstTime;
  }
  getLastTimestamp() {
    return this.lastTime;
  }
};

class MessagePool {
  private readonly logger: Logger;
  private readonly level: LogLevel;
  private readonly pool: Map<string, LogMessage> = new Map();

  constructor(logger: Logger, level: LogLevel) {
    this.logger = logger;
    this.level = level;
  }

  getMessage(messageIdentifier: string, fullMessage: string): LogMessage {
    let message = this.pool.get(messageIdentifier);
    if (message == undefined) {
      message = new LogMessage(messageIdentifier, fullMessage);
      this.pool.set(messageIdentifier, message);
    }
    return message;
  }

  log(messageIdentifier: string, fullMessage: string) {
    const message = this.getMessage(messageIdentifier, fullMessage);
    if (message.log(fullMessage)) {
      this.logger.log(fullMessage, this.level, message.getLastTimestamp());
    }
  }

  isFlooding(messageIdentifier: string) {
    const message = this.pool.get(messageIdentifier);
    return message != undefined && !message.notFlooding();
  }

  clear() {
    for (const message of this.pool.values()) {
      const messageToLog = message.clear();
      if (messageToLog != null) {
        this.logger.log(messageToLog, this.level, message.getFirstTimestamp(), message.getFloodCount() > 1 ? message.getLastTimestamp() : -1);
      }
    }
    this.pool.clear();
  }
}

export class Logger {
  private static readonly instances = new Set<Logger>();

  private readonly filename: string;
  private readonly messagePools: { [key in LogLevel]?: MessagePool } = {};
  private readonly callbacks: {
    [key in LogLevel]?: Array<(...args: any[]) => void>;
  } = {};

  constructor(filename: string) {
    this.filename = filename;

    if (filename == null) {
      this.log('Logger error: filename is null', LogLevel.ERROR);
      throw new Error('Logger error has occured, see log for details'); // don't want to throw the same error message, because the error handler might catch it and also log it, causing a duplicate log
    }

    Logger.instances.add(this);

    for (const level of Object.values(LogLevel)) {
      this.messagePools[level] = new MessagePool(this, level);
    }

    setInterval(() => {
      this.clear();
    }, FLOOD_CLEAR_TIME);
  }

  clear(log: boolean = false) {
    if (log) this.log(`Clearing log pools for ${this.filename}`, LogLevel.INFO);
    for (const pool of Object.values(this.messagePools)) {
      pool.clear();
    }
  }

  static clear(log: boolean = false) {
    for (const instance of this.instances) {
      instance.clear(log);
    }
  }

  private static async errorToString(error: Error): Promise<string> {
    return await Logger.mapStackTraceAsync(error.stack);
  }

  log(
    message: string,
    level: LogLevel = LogLevel.INFO,
    startTimestamp: number = -1,
    endTimestamp: number = -1
  ) {
    if (this.callbacks[level] != undefined) {
      for (const callback of this.callbacks[level]) {
        callback(message);
      }
    }
    Hud.hub.invoke('Log', level, startTimestamp, endTimestamp, message);
  }

  logFunction(callback: (...args: any[]) => void, level: LogLevel) {
    if (this.callbacks[level] == undefined) {
      this.callbacks[level] = [];
    }
    this.callbacks[level].push(callback);

    const pool = this.messagePools[level];
    const loggerInstance = this;

    return async function (...args: any[]) {
      let origin = loggerInstance.filename;

      const messageIdentifier = JSON.stringify(args[0]);
      let messages = [];
      for (const arg of args) {
        if (arg instanceof Error) {
          messages.push(await Logger.errorToString(arg));
        } else {
          messages.push(JSON.stringify(arg));
        }
      }
      const message = messages.join(' ');
      let fullMessage = `${origin}: ${message}`;

      if (pool.isFlooding(messageIdentifier) || level !== LogLevel.ERROR) {
        pool.log(messageIdentifier, fullMessage);
      } else {
        if (!args.some((x) => x instanceof Error)) {
          try {
            const stack = await Logger.getMappedStackTrace();
            const caller_line = stack.split('\n')[8];
            if (caller_line == undefined) {
              throw new Error('stacktrace too short');
            }
            const index = caller_line.indexOf('at ');
            origin = caller_line.slice(index + 1, caller_line.length) ?? origin;
          } catch (e) {}
        }

        fullMessage = `${origin}: ${message}`;
        pool.log(messageIdentifier, fullMessage);
      }
    };
  }

  private static readonly sourceMaps: { [key: string]: RawSourceMap } = {};
  private static async getSourceMapFromUri(uri: string) {
    if (Logger.sourceMaps[uri] != undefined) {
      return Logger.sourceMaps[uri];
    }
    const uriQuery = new URL(uri).search;
    const currentScriptContent = await (await fetch(uri)).text();

    let mapUri = RegExp(/\/\/# sourceMappingURL=(.*)/).exec(
      currentScriptContent
    )[1];
    mapUri = new URL(mapUri, uri).href + uriQuery;

    const map = await (await fetch(mapUri)).json();

    Logger.sourceMaps[uri] = map;

    return map;
  }

  private static async mapStackTrace(stack: string) {
    realConsoleError(stack);
    const stackLines = stack.split('\n');
    const mappedStack = [];

    for (const line of stackLines) {
      const match = RegExp(/(.*)(http:\/\/.*):(\d+):(\d+)/).exec(line);
      if (match == null) {
        mappedStack.push(line);
        continue;
      }

      const uri = match[2];
      const consumer = new SourceMapConsumer(
        await Logger.getSourceMapFromUri(uri)
      );

      const originalPosition = consumer.originalPositionFor({
        line: parseInt(match[3]),
        column: parseInt(match[4]),
      });

      if (
        originalPosition.source == null ||
        originalPosition.line == null ||
        originalPosition.column == null
      ) {
        mappedStack.push(line);
        continue;
      }

      mappedStack.push(
        `${originalPosition.source}:${originalPosition.line}:${
          originalPosition.column + 1
        }`
      );
    }

    return mappedStack.join('\n');
  }

  static async mapStackTraceAsync(stack: string): Promise<string> {
    try {
      return await Logger.mapStackTrace(stack);
    } catch (e) {
      realConsoleError(e);
      return stack;
    }
  }

  public static async getMappedStackTrace(): Promise<string> {
    return await Logger.mapStackTraceAsync(Logger.getStackTrace());
  }

  public static getStackTrace(): string {
    try {
      throw Error('');
    } catch (err) {
      return err.stack;
    }
  }
}

export function enableLogging(ipc: import('electron').IpcRenderer, filename: string) {
  const logger = new Logger(filename);

  ipc.on('quit', () => {
    Logger.clear(true);
  });

  const originalLog = console.log;
  const originalWarn = console.warn;
  const originalError = console.error;
  console.log = logger.logFunction(originalLog, LogLevel.INFO);
  console.warn = logger.logFunction(originalWarn, LogLevel.WARN);
  console.error = logger.logFunction(originalError, LogLevel.ERROR);


  window.onerror = async (_message, _file, _line, _column, errorObj) => {
    if (errorObj?.stack !== undefined) {
      console.error(await Logger.mapStackTraceAsync(errorObj.stack));
    }

    return false;
  };
  window.addEventListener('unhandledrejection', (e) => {
    console.error(e.reason);
  });
  window.addEventListener('securitypolicyviolation', (e) => {
    const message = `Blocked '${e.blockedURI}' from ${e.documentURI}:${e.lineNumber} (${e.violatedDirective})`;
    console.error(message);
  });
}
