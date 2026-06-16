import {mkdir, open} from "node:fs/promises";
import {join} from "node:path";
import os from "node:os";
import config from "./config.json" with {type: "json"};

const getRandInt = (max, min = 0) => min + Math.floor(Math.random() * (max - min + 1));

async function checkDirExists(path) {
    let result = undefined;

    try {
        const handle = await open(path);
        const stat = await handle.stat();
        await handle.close();

        if (stat.isDirectory()) {
            result = true;
        }
    } catch (err) {
        if (err.code === "ENOENT") {
            result = false;
        } else {
            throw err;
        }
    }

    return result;
}

async function makeDir(path) {
    await mkdir(path, {recursive: true});
}

async function generateFile(targetPath, extension, pairs) {
    const now = new Date();
    const ts = (/([\d-]+)T([\d:]+)\.\d+Z/).exec(now.toISOString());
    const filename = `${ts[1].replace(/-/g, "")}-${ts[2].replace(/:/g, "")}.${extension}`;
    const fullName = join(targetPath, filename);
    const body = pairs.map(([a, b]) => `${a} ${b}`).join("\n");

    const handle = await open(fullName, "w");
    await handle.writeFile(body + "\n\n");
    await handle.close();

    console.log("Wrote: ", filename);
}

function generatePairs(minPairs, maxPairs, maxInteger) {
    const result = [];

    for (let i = minPairs; i <= maxPairs; i += 1) {
        const pair = [getRandInt(maxInteger, 2), getRandInt(maxInteger, 2)];
        result.push(pair);
    }

    return result;
}

function getRandomDelay(maxDelay, minDelay = 1000) {
    const [max, min] = [Math.floor(maxDelay / 100), Math.floor(minDelay / 100)];
    const d = getRandInt(max, min) * 100;
    return d > maxDelay ? maxDelay : d;
}

function resolvePath(path) {
    if (path.startsWith("~")) {
        return join(os.homedir(), path.slice(1));
    }
    return path;
}

async function main() {
    const inputPath = resolvePath(config.directories.input);
    const pathExists = await checkDirExists(inputPath);
    if (pathExists === false) {
        await makeDir(inputPath);
    } else if (typeof pathExists === "undefined") {
        throw new Error(`The input path configured as "${inputPath}" exists but is not a directory`);
    }

    console.log("Target directory:", inputPath);

    const {fileExtension, maxDelay, maxFilesToGenerate, minPairs, maxPairs, maxInteger} = config.settings;
    let counter = 0;

    const delay = getRandomDelay(maxDelay);
    let interval = setInterval(() => {
        const pairs = generatePairs(minPairs, maxPairs, maxInteger);
        generateFile(inputPath, fileExtension, pairs);
        counter += 1;

        if (counter >= maxFilesToGenerate) {
            clearInterval(interval);
        }
    }, delay);
}

await main();
