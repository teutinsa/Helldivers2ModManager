declare global {
    interface String {
        format(args: Record<string, unknown> | unknown[]): string;
    }
}

String.prototype.format = function (args): string {
    return this.replace(/\{(\w+)\}/g, (_, k) => {
        if (Array.isArray(args)) return String(args[parseInt(k)]) ?? `{${k}}`;
        return String(args[k]) ?? `{${k}}`;
    });
}

export {};