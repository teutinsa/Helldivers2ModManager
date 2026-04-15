import type { UUIDTypes } from "uuid";
import type { legacy, Manifest, v1, v2 } from "./manifest";

export type Mod = {
    readonly manifest: Manifest;
    readonly directory: string;

    get guid(): UUIDTypes {
        switch (this.manifest) {
            case legacy.Manifest:
                return (<legacy.Manifest>this.manifest).Guid;
            
            case v1.Manifest:
                return (<v1.Manifest>this.manifest).Guid;
            
            case v2.Manifest:
                return (<v2.Manifest>this.manifest).Guid;
        }
    }

    get name(): string {
        switch (this.manifest) {
            case legacy.Manifest:
                return (<legacy.Manifest>this.manifest).Name;
            
            case v1.Manifest:
                return (<v1.Manifest>this.manifest).Name;
            
            case v2.Manifest:
                return (<v2.Manifest>this.manifest).Name;
        }
    }
};