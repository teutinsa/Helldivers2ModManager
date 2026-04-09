import { trace, debug, info, warn, error, LogLevel } from '@tauri-apps/plugin-log';

function log(level: LogLevel, scope: string, message: string, args?: Record<string, unknown>) {
    const format = args
        ? `${scope} : ${message}\n${JSON.stringify(args, null, 2)}`
        : `${scope} : ${message}`;

    switch (level) {
        case LogLevel.Trace: trace(format); break;
        case LogLevel.Debug: debug(format); break;
        case LogLevel.Info: info(format); break;
        case LogLevel.Warn: warn(format); break;
        case LogLevel.Error: error(format); break;
    }
}

export function createLogger(scope: string) {
    return {
        trace: (message: string, args?: Record<string, unknown>) => log(LogLevel.Trace, scope, message, args),
        debug: (message: string, args?: Record<string, unknown>) => log(LogLevel.Debug, scope, message, args),
        info: (message: string, args?: Record<string, unknown>) => log(LogLevel.Info, scope, message, args),
        warn: (message: string, args?: Record<string, unknown>) => log(LogLevel.Warn, scope, message, args),
        error: (message: string, args?: Record<string, unknown>) => log(LogLevel.Error, scope, message, args),
    }
}