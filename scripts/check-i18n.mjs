import { readFileSync, readdirSync, statSync } from 'fs';
import { join, relative } from 'path';

function flattenKeys(obj, prefix = null) {
    const keys = [];
    for (const [k, v] of Object.entries(obj)) {
        const fullKey = prefix ? `${prefix}.${k}` : k;
        if (v !== null && typeof v === 'object' && !Array.isArray(v)) {
            keys.push(...flattenKeys(v, fullKey));
        } else {
            keys.push(fullKey);
        }
    }
    return keys;
}

const translations = JSON.parse(readFileSync("./src/lib/locales/en.json", 'utf8'));
const knownKeys = new Set(flattenKeys(translations));

function getSvelteFiles(dir) {
    const results = [];
    for (const entry of readdirSync(dir)) {
        const fullPath = join(dir, entry);
        if (statSync(fullPath).isDirectory()) {
            results.push(...getSvelteFiles(fullPath));
        } else if (entry.endsWith(".svelte")) {
            results.push(fullPath);
        }
    }
    return results;
}

const KEY_REGEX = /\bt\(\s*['"]([^'"]+)['"]\s*(?:,[\s\S]*?)?\)/g;

const files = getSvelteFiles("./src");
const foundKeys = new Map();

for (const file of files) {
    const content = readFileSync(file, 'utf8');
    for (const match of content.matchAll(KEY_REGEX)) {
        const key = match[1];
        if (!foundKeys.has(key)) foundKeys.set(key, []);
        foundKeys.get(key).push(relative(".", file));
    }
}

const missingKeys = [...foundKeys.keys()].filter(k => !knownKeys.has(k));
const unusedKeys  = [...knownKeys].filter(k => !foundKeys.has(k));

let hasErrors = false;

if (missingKeys.length > 0) {
    hasErrors = true;
    console.error("\x1b[31m!!!\x1b[0m Missing translation keys:");
    for (const key of missingKeys) {
        console.error(`\t${key}`);
        for (const file of foundKeys.get(key)) {
            console.error(`\t\t\x1b[90m${file}\x1b[0m`);
        }
    }
}

if (unusedKeys.length > 0) {
    console.warn("\x1b[33m/!\\\x1b[0m Unused translation keys:");
    for (const key of unusedKeys) console.warn(`\t${key}`);
}

if (!hasErrors) {
    console.log("\x1b[32m( )\x1b[0m All translation keys accounted for.");
}

if (hasErrors) process.exit(1);