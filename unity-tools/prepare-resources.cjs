// Stage an auditable copy of this project's artwork without modifying the H5.
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const vm = require('node:vm');
const sharp = require('sharp');
const root = path.resolve(__dirname, '..');
const stage = path.join(root, 'output/unity-resource-staging');
const assetRoot = 'Assets/GuanshanYouyan';
const manifest = { schemaVersion: 1, createdAt: new Date().toISOString(), assets: [], archive: [], activeArt: {}, notes: [] };
const posix = p => p.split(path.sep).join('/');
const hash = b => crypto.createHash('sha256').update(b).digest('hex');
function write(rel, data) {
  const dest = path.join(stage, rel);
  fs.mkdirSync(path.dirname(dest), { recursive: true });
  fs.writeFileSync(dest, data);
}
function files(dir) {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap(e => e.isDirectory() ? files(path.join(dir, e.name)) : [path.join(dir, e.name)]).sort();
}
function archive(source) {
  const rel = posix(path.relative(root, source));
  const content = fs.readFileSync(source);
  const dest = 'SourceArchive~/' + rel;
  write(dest, content);
  manifest.archive.push({ source: rel, destination: dest, sha256: hash(content), bytes: content.length });
}
async function image(source, dest, category) {
  const content = fs.readFileSync(source);
  const ext = path.extname(source).toLowerCase();
  const converted = ['.webp', '.avif', '.svg'].includes(ext);
  const out = converted ? await sharp(content).png().toBuffer() : content;
  const meta = await sharp(out).metadata();
  if (converted && ext !== '.svg') {
    const before = await sharp(content).ensureAlpha().raw().toBuffer();
    const after = await sharp(out).ensureAlpha().raw().toBuffer();
    if (!before.equals(after)) throw new Error('Pixel verification failed: ' + source);
  }
  write(dest, out);
  manifest.assets.push({ source: posix(path.relative(root, source)), destination: dest, category, width: meta.width, height: meta.height, hasAlpha: !!meta.hasAlpha, sourceSha256: hash(content), sha256: hash(out), bytes: out.length, conversion: converted ? ext.slice(1) + '-to-png' : 'byte-copy', pixelVerified: converted && ext !== '.svg' });
}
async function main() {
  if (fs.existsSync(path.join(stage, 'resource-manifest.json'))) throw new Error('Staging exists; choose a fresh snapshot instead of overwriting it.');
  const publicRoot = path.join(root, 'h5/public');
  const publicAssets = path.join(publicRoot, 'assets');
  for (const f of files(publicAssets)) {
    archive(f);
    const rel = posix(path.relative(publicAssets, f));
    const ext = path.extname(f).toLowerCase();
    if (!['.png', '.jpg', '.jpeg', '.webp', '.avif', '.svg'].includes(ext)) continue;
    const dest = assetRoot + '/Art/WebLibrary/' + (['.webp', '.avif', '.svg'].includes(ext) ? rel.slice(0, -ext.length) + '-' + ext.slice(1) + '.png' : rel);
    await image(f, dest, rel.startsWith('walk/') ? 'WalkArt' : rel.startsWith('mist/') ? 'MistArt' : 'LegacyArt');
  }
  const referenceRoot = path.join(root, 'h5/design/motion-reference');
  for (const f of files(referenceRoot)) {
    if (/\.blend1$|\.zip$/i.test(f)) continue; // redundant backup and packed duplicate
    archive(f);
    const rel = posix(path.relative(referenceRoot, f));
    if (/\.(png|jpg)$/i.test(f)) await image(f, assetRoot + '/Art/ProductionReference/' + rel, 'ProductionReference');
    if (/\.fbx$/i.test(f)) {
      const data = fs.readFileSync(f), dest = assetRoot + '/Models/Quaternius/' + path.basename(f);
      write(dest, data);
      manifest.assets.push({ source: posix(path.relative(root, f)), destination: dest, category: 'ReferenceModel', sha256: hash(data), bytes: data.length });
    }
    if (path.basename(f) === 'License.txt') write(assetRoot + '/Models/Quaternius/License.txt', fs.readFileSync(f));
  }
  for (const f of files(path.join(root, 'design')).filter(f => /LinJian.*\.png$/i.test(f))) {
    archive(f);
    await image(f, assetRoot + '/Art/CharacterAtlases/' + path.basename(f), 'CharacterAtlas');
  }
  for (const f of files(path.join(root, 'output/imagegen')).filter(f => /\.png$/i.test(f))) {
    archive(f);
    await image(f, assetRoot + '/Art/PaperCut/' + path.basename(f), 'PaperCut');
  }
  for (const f of files(path.join(publicRoot, 'docs'))) {
    archive(f);
    write(assetRoot + '/Documentation/' + posix(path.relative(path.join(publicRoot, 'docs'), f)), fs.readFileSync(f));
  }
  const context = { window: {} };
  vm.createContext(context);
  for (const name of ['mist-art.js', 'mist-walk-art.js', 'game-data.js', 'mist-data.js', 'mist-walk-data.js', 'mist-player-rig-data.js']) {
    const source = path.join(publicRoot, name);
    archive(source);
    vm.runInContext(fs.readFileSync(source, 'utf8'), context, { timeout: 5000, filename: name });
  }
  for (const [name, data] of Object.entries(context.window)) write(assetRoot + '/Data/' + name + '.json', JSON.stringify(data, null, 2));
  for (const [key, art] of Object.entries(context.window.MistArt)) {
    const source = art.webp?.src;
    const match = manifest.assets.find(a => a.source === 'h5/public/' + source);
    if (match) manifest.activeArt[key] = match.destination;
  }
  const walker = context.window.MistArt.walker;
  if (walker?.fallback?.webp?.src) {
    const atlas = path.join(publicRoot, walker.fallback.webp.src);
    const { frameWidth: width, frameHeight: height, columns, idleFrame, walkFrames } = walker;
    const allFrames = [...new Set([idleFrame, ...walkFrames, walker.fallback.portraitFrame])].filter(Number.isInteger);
    const atlasMeta = await sharp(atlas).metadata();
    if (width * columns !== atlasMeta.width || height * walker.rows !== atlasMeta.height) throw new Error('Atlas layout mismatch');
    for (const index of allFrames) {
      const dest = assetRoot + '/Art/Characters/LinJian/Frame_' + String(index).padStart(2, '0') + '.png';
      const out = await sharp(atlas).extract({ left: (index % columns) * width, top: Math.floor(index / columns) * height, width, height }).png().toBuffer();
      write(dest, out);
      manifest.assets.push({ source: posix(path.relative(root, atlas)), destination: dest, category: 'LinJianFrame', frameIndex: index, width, height, hasAlpha: true, sha256: hash(out), bytes: out.length, conversion: 'exact-grid-extraction' });
    }
    write(assetRoot + '/Data/LinJianAnimation.json', JSON.stringify({ sourceAtlas: walker.fallback.webp.src, frameWidth: width, frameHeight: height, idleFrame, walkFrames, portraitFrame: walker.fallback.portraitFrame, cycleSeconds: walker.cycleSeconds, groundBaseline: walker.groundBaseline }, null, 2));
  }
  // Keep the live character identity and rig parts accessible as standalone sprites.
  if (walker?.regions && walker?.webp?.src) {
    const atlas = path.join(publicRoot, walker.webp.src);
    for (const [name, rect] of Object.entries(walker.regions)) {
      const dest = assetRoot + '/Art/Characters/LinJianParts/' + name + '.png';
      const out = await sharp(atlas).extract({ left: rect.x, top: rect.y, width: rect.width, height: rect.height }).png().toBuffer();
      write(dest, out);
      manifest.assets.push({ source: posix(path.relative(root, atlas)), destination: dest, category: 'LinJianPart', width: rect.width, height: rect.height, hasAlpha: true, sha256: hash(out), bytes: out.length, conversion: 'exact-region-extraction' });
    }
  }
  manifest.activeEntries = Object.entries(manifest.activeArt).map(([key, destination]) => ({ key, destination }));
  manifest.notes = ['Snapshot excludes dist duplicates, test screenshots, benchmarks, node_modules and .blend1 backups.', 'SourceArchive~ retains original browser formats, Blender files and source data without triggering Unity imports.', 'PNG conversions preserve decoded RGBA pixels; SVG is rasterized.', 'Only resources and data are migrated; H5 JavaScript gameplay is not a Unity implementation.', 'Current scene backgrounds are illustrations, not a prepared terrain RuleTile tileset.'];
  write(assetRoot + '/Data/resource-manifest.json', JSON.stringify(manifest, null, 2));
  write('resource-manifest.json', JSON.stringify(manifest, null, 2));
  const total = manifest.assets.reduce((s, a) => s + a.bytes, 0);
  console.log(JSON.stringify({ stage, assets: manifest.assets.length, originalSources: manifest.archive.length, generatedFrames: manifest.assets.filter(a => a.category === 'LinJianFrame').length, converted: manifest.assets.filter(a => a.pixelVerified).length, bytes: total, activeArt: manifest.activeArt }, null, 2));
}
main().catch(e => { console.error(e); process.exitCode = 1; });
