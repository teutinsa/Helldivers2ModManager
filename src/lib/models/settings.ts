export type FixedLengthString<N extends number> = string & {
    readonly __length: N;
};

export function toFixedLengthString<N extends number>(value: string, length: N): FixedLengthString<N> {
    if (value.length != length) {
        throw new Error(`Expected string of length ${length}, got ${value.length}`);
    }
    return value as FixedLengthString<N>;
}

export type SkipEntry = FixedLengthString<16>;

export function toSkipEntry(value: string): SkipEntry {
    return toFixedLengthString(value, 16);
}

export function tryToSkipEntry(value: string): SkipEntry | null {
    if (value.length !== 16) return null;
    return value as SkipEntry;
}

export type SettingsV1 = {
    Version: "V1",
    GamePath: string,
    SkipList: SkipEntry[]
};

export type Settings = SettingsV1 /* | SettingsV2 */;