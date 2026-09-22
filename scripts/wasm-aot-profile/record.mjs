// Records the AOT profile for the WebAssembly head's profile-guided AOT publish.
//
//   cd scripts/wasm-aot-profile && npm ci && node record.mjs [--skip-publish] [--screenshot <file>]
//
// 1. Publishes a profiling build of the head (UnoGenerateAotProfile=true) to artifacts/wasm-aot-profiling.
// 2. Serves it on localhost, opens it in headless Chrome, and waits for today's reading to render.
// 3. Saves the profile the runtime recorded to DailyReflection/Platforms/WebAssembly/aot.profile,
//    where the Uno SDK picks it up for every Release publish (see docs/WEB-DEPLOY.md).
//
// The recording is startup only, on purpose. Cloudflare Pages rejects files over 25 MiB, and
// AOT code lands in the one dotnet.native.wasm: a walk through every screen put it at 25.8 MiB,
// startup alone at 23.1 MiB. The screens startup never reaches still ran within ~10% of the
// full walk's speed, because they mostly run the same Uno layout and rendering code.

import { spawnSync } from 'node:child_process';
import { createReadStream, existsSync, rmSync, statSync } from 'node:fs';
import { createServer } from 'node:http';
import { dirname, extname, join, normalize, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { chromium } from 'playwright-core';

const repo = resolve(dirname(fileURLToPath(import.meta.url)), '../..');
const publishDir = join(repo, 'artifacts/wasm-aot-profiling');
const wwwroot = join(publishDir, 'wwwroot');
const profilePath = join(repo, 'DailyReflection/Platforms/WebAssembly/aot.profile');
// The WebAssembly SDK's incremental AOT compile doesn't track the profile: after it changes,
// a local publish reuses the old compiled output ("Everything is up-to-date, nothing to
// precompile") unless this folder is gone.
const aotCache = join(repo, 'DailyReflection/obj/Release/net10.0-browserwasm/wasm/for-publish');

const args = process.argv.slice(2);
const skipPublish = args.includes('--skip-publish');
const screenshotIndex = args.indexOf('--screenshot');
const screenshotPath = screenshotIndex >= 0 ? resolve(args[screenshotIndex + 1]) : null;

if (!skipPublish) {
	// PublishTrimmed=true overrides the bootstrapper's profiling default (false): the untrimmed
	// build crashes at startup on .NET 10 ("Your mono runtime and class libraries are out of
	// sync"), and a trimmed profile matches the trimmed AOT publish it feeds anyway.
	const publish = spawnSync('dotnet', [
		'publish', 'DailyReflection/DailyReflection.Uno.csproj',
		'-f', 'net10.0-browserwasm', '-c', 'Release',
		'-p:TargetFrameworkOverride=wasm',
		'-p:UnoGenerateAotProfile=true',
		'-p:PublishTrimmed=true',
		'-p:CompressionEnabled=false',
		'-o', publishDir,
	], { cwd: repo, stdio: 'inherit' });
	if (publish.status !== 0) {
		process.exit(publish.status ?? 1);
	}
}

if (!existsSync(join(wwwroot, 'index.html'))) {
	console.error(`No profiling build in ${wwwroot}; run without --skip-publish.`);
	process.exit(1);
}

const mimeTypes = {
	'.html': 'text/html', '.js': 'text/javascript', '.mjs': 'text/javascript', '.css': 'text/css',
	'.json': 'application/json', '.wasm': 'application/wasm', '.png': 'image/png', '.svg': 'image/svg+xml',
	'.ttf': 'font/ttf', '.woff': 'font/woff', '.woff2': 'font/woff2', '.webmanifest': 'application/manifest+json',
};

const server = createServer((req, res) => {
	let path = normalize(join(wwwroot, decodeURIComponent(new URL(req.url, 'http://x').pathname)));
	if (!path.startsWith(wwwroot)) {
		res.writeHead(403).end();
		return;
	}
	if (existsSync(path) && statSync(path).isDirectory()) {
		path = join(path, 'index.html');
	}
	if (!existsSync(path)) {
		res.writeHead(404).end();
		return;
	}
	res.writeHead(200, { 'Content-Type': mimeTypes[extname(path)] ?? 'application/octet-stream' });
	createReadStream(path).pipe(res);
});
await new Promise(r => server.listen(0, '127.0.0.1', r));
const url = `http://127.0.0.1:${server.address().port}/`;

const browser = await chromium.launch({ channel: 'chrome', headless: true });
const page = await browser.newPage({ viewport: { width: 1280, height: 800 }, acceptDownloads: true });

let runtimeFailure = null;
page.on('console', m => {
	if (/out of sync|mono_wasm_load_runtime \(\) failed|Unhandled exception/i.test(m.text())) {
		runtimeFailure ??= m.text();
	}
});
page.on('pageerror', e => runtimeFailure ??= e.message);

try {
	await page.goto(url);

	// The app draws on a canvas, so "the reading has rendered" is read off the pixels: the
	// first lines of the reading's body text, blank until the FeedView shows the reading
	// (a blank PNG of that area compresses to a few hundred bytes).
	const readingText = { x: 100, y: 130, width: 600, height: 60 };
	const deadline = Date.now() + 120000;
	while ((await page.screenshot({ clip: readingText })).length < 4000) {
		if (runtimeFailure) {
			throw new Error(`App failed at startup: ${runtimeFailure}`);
		}
		if (Date.now() > deadline) {
			throw new Error("Today's reading never rendered.");
		}
		await page.waitForTimeout(250);
	}
	// Let the work that follows first paint (deferred startup migrations, idle layout) run too.
	await page.waitForTimeout(5000);
	if (runtimeFailure) {
		throw new Error(`App failed at startup: ${runtimeFailure}`);
	}
	if (screenshotPath) {
		await page.screenshot({ path: screenshotPath });
	}

	// The bootstrapper saves the profile on Shift+Cmd/Alt+P as a browser download.
	const download = page.waitForEvent('download', { timeout: 60000 });
	await page.keyboard.press('Meta+Shift+KeyP');
	await (await download).saveAs(profilePath);
	rmSync(aotCache, { recursive: true, force: true });
	console.log(`Saved ${profilePath} (${statSync(profilePath).size} bytes)`);
} finally {
	await browser.close();
	server.close();
}
