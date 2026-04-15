import { UUID } from "$lib/types/uuid";

namespace legacy {
    export type Manifest = {
        readonly Guid: UUID;
        readonly Name: string;
        readonly Description: string;
        readonly IconPath?: string;
        readonly Options?: string[];
    };
}

namespace v1 {
    export type Manifest = {
        readonly Version: number;
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

namespace v2 {
    export type Manifest = {
        readonly Version: number;
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

type Manifest = legacy.Manifest | v1.Manifest | v2.Manifest;