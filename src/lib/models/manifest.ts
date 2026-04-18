import type { UUID } from "$lib/types/uuid";

export namespace legacy {
    export type Manifest = {
        readonly Guid: UUID;
        readonly Name: string;
        readonly Description: string;
        readonly IconPath?: string;
        readonly Options?: string[];
    };
}

export namespace v1 {
    export type Manifest = {
        Version: 1;
        readonly Guid: UUID;
        readonly Name: string;
        readonly Description: string;
        readonly IconPath?: string;
        readonly Options?: Option[];
        readonly NexusData?: NexusData;
    };

    export type Option = {
        readonly Name: string;
        readonly Description: string;
        readonly Include?: string[];
        readonly Image?: string;
        readonly SubOptions?: SubOption[];
    };

    export type SubOption = {
        readonly Name: string;
        readonly Description: string;
        readonly Include: string[];
        readonly Image?: string;
    };

    export type NexusData = {
        readonly ModId: number;
        readonly Version: string;
    };
}

export namespace v2 {
    export type Manifest = {
        Version: 2;
        readonly Guid: UUID;
        readonly Name: string;
        readonly Description: string;
        readonly IconPath?: string;
        readonly Options?: Option[];
        readonly Categories?: Category[];
        readonly Tags?: string[];
        readonly NexusData?: NexusData;
    };

    export type Option = {
        readonly Guid: UUID;
        readonly Name: string;
        readonly CategoryRef?: UUID;
        readonly Description: string;
        readonly Include?: string[];
        readonly Image?: string;
        readonly SubOptions?: SubOption[]; 
    };

    export type SubOption = {
        readonly Guid: UUID;
        readonly Name: string;
        readonly Description: string;
        readonly Include: string[];
        readonly Image?: string;
    };

    export type Category = {
        readonly Guid: UUID;
        readonly Name: string;
        readonly Description: string;
    };

    export type NexusData = {
        readonly ModId: number;
    };
}

export type Manifest = legacy.Manifest | v1.Manifest | v2.Manifest;