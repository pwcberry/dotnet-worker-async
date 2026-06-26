import {open} from "node:fs/promises";
import {join} from "node:path";
import {argv} from "node:process";
import {makeDir, checkDirExists, resolvePath} from "./file.js";
import config from "./config.json" with {type: "json"};
import {uuid} from "./util.js";

const getRandInt = (max, min = 0) => min + Math.floor(Math.random() * (max - min + 1));

async function generatePairFile(targetPath, extension, pairs) {
    const now = new Date();
    const ts = (/([\d-]+)T([\d:]+)\.\d+Z/).exec(now.toISOString());
    const hash = uuid().substring(0, 12).replace(/-/g, "");
    const filename = `${ts[1].replace(/-/g, "")}-${ts[2].replace(/:/g, "")}-${hash}.${extension}`;
    const body = pairs.map(([a, b]) => `${a} ${b}`).join("\n");
    const fullName = join(targetPath, filename);

    await using handle = await open(fullName, "w");
    await handle.writeFile(body + "\n\n");
    await handle.close();

    console.log("Wrote: ", filename);
}

function generatePairs(minPairs, maxPairs, maxInteger) {
    const result = [];
    const maxPairsToGenerate = minPairs + getRandInt(maxPairs - minPairs) + 1;

    for (let i = minPairs; i <= maxPairsToGenerate; i += 1) {
        const pair = [getRandInt(maxInteger, 2), getRandInt(maxInteger, 2)];
        result.push(pair);
    }

    return result;
}

function getRandomDelay(maxDelay, minDelay = 1000) {
    const [max, min] = [Math.floor(maxDelay / 100), Math.floor(minDelay / 100)];
    const d = getRandInt(max, min) * 100;
    return Math.min(d, maxDelay);
}

function runGeneratorOnTimer(inputPath) {
    const {fileExtension, maxDelay, maxFilesToGenerate, minPairs, maxPairs, maxInteger} = config.settings;
    let counter = 0;

    const delay = getRandomDelay(maxDelay);
    let interval = setInterval(async () => {
        const pairs = generatePairs(minPairs, maxPairs, maxInteger);
        await generatePairFile(inputPath, fileExtension, pairs);
        counter += 1;

        if (counter >= maxFilesToGenerate) {
            clearInterval(interval);
        }
    }, delay);
}

async function runGeneratorAsBatch(inputPath, fileCount) {
    const {fileExtension, maxFilesToGenerate, minPairs, maxPairs, maxInteger} = config.settings;
    let counter = Math.min(fileCount, maxFilesToGenerate);

    while (counter > 0) {
        const pairs = generatePairs(minPairs, maxPairs, maxInteger);
        await generatePairFile(inputPath, fileExtension, pairs);
        counter -= 1;
    }
}

async function main(command, argument) {
    const inputPath = resolvePath(config.directories.input);
    const pathExists = await checkDirExists(inputPath);
    if (pathExists === false) {
        await makeDir(inputPath);
    } else if (pathExists === undefined) {
        throw new Error(`The input path configured as "${inputPath}" exists but is not a directory`);
    }

    console.log("Target directory:", inputPath);

    if (command === "timer") {
        runGeneratorOnTimer(inputPath);
    } else {
        let fileCount = parseInt(argument, 10);
        fileCount = Number.isNaN(fileCount) ? Number.MAX_VALUE : fileCount;
        await runGeneratorAsBatch(inputPath, fileCount);
    }
}

const validCommands = ["timer", "batch"];
let command = argv[2];
let argument = argv[3] ?? "";

if (!validCommands.includes(command)) {
    command = "";
}

await main(command, argument);
