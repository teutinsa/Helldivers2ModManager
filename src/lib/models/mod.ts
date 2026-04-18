import type { UUID } from "$lib/types/uuid";
import { join } from "@tauri-apps/api/path";
import type { Manifest } from "./manifest";
import { convertFileSrc } from "@tauri-apps/api/core";

export class Mod {
    constructor(
        public readonly Manifest: Manifest,
        public readonly Directory: string
    ) {}

    get guid(): UUID {
        return this.Manifest.Guid;
    }

    get name(): string {
        return this.Manifest.Name;
    }

    get description(): string {
        return this.Manifest.Description;
    }

    async iconPath(): Promise<string | null> {
        if (!this.Manifest.IconPath) return null;
        return convertFileSrc(await join(this.Directory, this.Manifest.IconPath));
    }
}