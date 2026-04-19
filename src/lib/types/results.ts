export type RustResult<T, E = string> =
    | { Ok: T }
    | { Err: E };

export type ModAddResult =
    | { success: true; archiveFile: string }
    | { success: false; archiveFile: string; errorMessage: string };