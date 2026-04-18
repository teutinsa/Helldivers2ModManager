import type { UUID } from "$lib/types/uuid";

export type ConfigLegacy = {
    For: "Legacy";
    Enabled: boolean;
    Selected: number;
};

export type ConfigV1 = {
    For: "V1";
    Enabled: boolean;
    Toggled: boolean[];
    Selected: number[];
};

export type ConfigV2 = {
    For: "V2";
    Enabled: boolean;
    Toggled: boolean[];
    Selected: number[];
};

export type Config = ConfigLegacy | ConfigV1 | ConfigV2;

export type ProfileV1 = {
    Version: "V1";
    Name: string;
    Configs: Record<UUID, Config>;
};

export type Profile = ProfileV1;

export type ProfilesConfig = {
    Profiles: Profile[];
    Active: number;
};