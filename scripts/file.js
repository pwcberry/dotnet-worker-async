import os from "node:os";
import {mkdir, open} from "node:fs/promises";
import { join } from "node:path";

/**
 * Checks if a directory exists at the given path.
 * @param path The path to check
 * @returns {Promise<boolean>} Returns true asynchronously when the directory is found
 */
async function checkDirExists(path) {
    let result;

    try {
        await using handle = await open(path);
        const stat = await handle.stat();
        await handle.close();

        result =  stat.isDirectory();
    } catch {
        result = false;
    }

    return result;
}

async function makeDir(path) {
    await mkdir(path, {recursive: true});
}

function resolvePath(path) {
    if (path.startsWith("~")) {
        return join(os.homedir(), path.slice(1));
    }
    return path;
}

export {
    makeDir,
    resolvePath,
    checkDirExists,
}