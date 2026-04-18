export type FixedLengthString<N extends number> = string & {
    readonly __length: N;
};

export type SkipEntry = FixedLengthString<16>;

export type SettingsV1 = {
    Version: "V1",
    GamePath: string,
    SkipList: SkipEntry[]
};

export type Settings = SettingsV1 /* | SettingsV2 */;