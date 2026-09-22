// Records the AOT profile for the WebAssembly head's profile-guided AOT publish.
//
//   cd scripts/wasm-aot-profile && npm ci && node record.mjs [--skip-publish] [--screenshots <dir>]
//
// 1. Publishes a profiling build of the head (UnoGenerateAotProfile=true) to artifacts/wasm-aot-profiling.
// 2. Serves it on localhost and walks the app in headless Chrome: both readings sources,
//    the date pickers, the sober-time display options and all three tabs, at desktop and
//    phone widths.
// 3. Saves the profile the runtime recorded to DailyReflection/Platforms/WebAssembly/aot.profile,
//    where the Uno SDK picks it up for every Release publish (see docs/WEB-DEPLOY.md).
//
// The app renders to a canvas, so the walk clicks coordinates of the 1280x800 and 400x800
// layouts. If the UI moves, run with --screenshots and adjust the coordinates below.

import { spawnSync } from 'node:child_process';
import { createReadStream, existsSync, mkdirSync, statSync } from 'node:fs';
import { createServer } from 'node:http';
import { dirname, extname, join, normalize, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { chromium } from 'playwright-core';

const repo = resolve(dirname(fileURLToPath(import.meta.url)), '../..');
const publishDir = join(repo, 'artifacts/wasm-aot-profiling');
const wwwroot = join(publishDir, 'wwwroot');
const profilePath = join(repo, 'DailyReflection/Platforms/WebAssembly/aot.profile');

const args = process.argv.slice(2);
const skipPublish = args.includes('--skip-publish');
const screenshotsIndex = args.indexOf('--screenshots');
const screenshotDir = screenshotsIndex >= 0 ? resolve(args[screenshotsIndex + 1]) : null;

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

let shot = 0;
const step = async (label, action, wait = 1500) => {
	await action();
	await page.waitForTimeout(wait);
	if (runtimeFailure) {
		throw new Error(`App failed during "${label}": ${runtimeFailure}`);
	}
	if (screenshotDir) {
		mkdirSync(screenshotDir, { recursive: true });
		await page.screenshot({ path: join(screenshotDir, `${String(shot++).padStart(2, '0')}-${label}.png`) });
	}
};
const click = (x, y) => () => page.mouse.click(x, y);

try {
	await step('startup', () => page.goto(url), 20000);

	// Desktop layout (1280x800): vertical tab rail on the left.
	const rail = { reflection: [40, 135], soberTime: [40, 400], settings: [40, 665] };
	await step('open-date-picker', click(1184, 32));
	await step('pick-previous-month', click(899, 133));
	await step('confirm-date', click(1106, 365), 3000);
	await step('open-date-picker-again', click(1184, 32));
	await step('cancel-date', click(1029, 365));
	await step('share', click(1248, 32));
	await step('settings', click(...rail.settings));
	await step('secular-on', click(1217, 170));
	await step('secular-reading', click(...rail.reflection), 3000);
	await step('secular-scroll', () => page.mouse.wheel(0, 600));
	await step('settings-again', click(...rail.settings));
	await step('secular-off', click(1217, 170));
	await step('open-sober-date', click(680, 295));
	await step('pick-sober-year', click(877, 174));
	await step('confirm-sober-date', click(914, 486));
	await step('open-display-options', click(680, 360));
	await step('days-only', click(112, 462));
	await step('sober-time-days', click(...rail.soberTime), 2500);
	await step('settings-third', click(...rail.settings));
	await step('open-display-options-again', click(680, 360));
	await step('days-months-years', click(112, 412));
	await step('sober-time-full', click(...rail.soberTime), 2500);

	// Phone layout (400x800): bottom tab bar.
	const bar = { reflection: [66, 760], soberTime: [199, 760], settings: [333, 760] };
	await step('phone-width', () => page.setViewportSize({ width: 400, height: 800 }), 2500);
	await step('phone-reflection', click(...bar.reflection), 2500);
	await step('phone-scroll', () => page.mouse.wheel(0, 800));
	await step('phone-settings', click(...bar.settings));
	await step('phone-sober-time', click(...bar.soberTime));
	await step('desktop-width', () => page.setViewportSize({ width: 1280, height: 800 }), 2500);

	// The bootstrapper saves the profile on Shift+Cmd/Alt+P as a browser download.
	const download = page.waitForEvent('download', { timeout: 60000 });
	await page.keyboard.press('Meta+Shift+KeyP');
	await (await download).saveAs(profilePath);
	console.log(`Saved ${profilePath} (${statSync(profilePath).size} bytes)`);
} finally {
	await browser.close();
	server.close();
}
