const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const root = path.resolve(__dirname, '../unity/GuanshanYouyan2D');
const tracks = [
  { id: '297537', title: 'Underwater - mysterious ambient music (first 120 seconds)', artist: 'HarumachiMusic', file: 'underwater-297537-first-120s.wav', originalFile: 'harumachimusic-underwater-mysterious-ambient-music-297537.mp3', trimStartSeconds: 0, trimDurationSeconds: 120, volume: 0.65, sourceUrl: 'https://pixabay.com/music/mystery-underwater-mysterious-ambient-music-297537/', enabled: true },
  { id: '519335', title: 'Mystery Mysterious Mystical', artist: 'MondaMusic', file: 'mondamusic-mystery-mysterious-mystical-519335.mp3', originalFile: 'mondamusic-mystery-mysterious-mystical-519335.mp3', volume: 0.55, sourceUrl: 'https://pixabay.com/music/modern-classical-mystery-mysterious-mystical-519335/', enabled: true }
];
const manifestPath = path.join(root, 'Assets/GuanshanYouyan/Data/resource-manifest.json');
const manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf8'));
for (const track of tracks) {
  track.assetPath = 'Assets/GuanshanYouyan/Audio/Music/' + track.file;
  track.archivePath = 'SourceArchive~/Audio/Pixabay/' + track.originalFile;
  const content = fs.readFileSync(path.join(root, track.assetPath));
  const archive = fs.readFileSync(path.join(root, track.archivePath));
  if (track.file === track.originalFile && !content.equals(archive)) throw new Error('Archive differs: ' + track.file);
  track.originalSha256 = crypto.createHash('sha256').update(archive).digest('hex');
  track.sha256 = crypto.createHash('sha256').update(content).digest('hex');
  track.bytes = content.length;
  track.license = 'Pixabay Content License';
  track.licenseUrl = 'https://pixabay.com/service/license-summary/';
  track.contentIdRegistered = true;
  track.provenance = 'MP3 supplied by the user after downloading from the linked Pixabay page.';
  const entry = { source: 'user-downloads/' + track.originalFile, destination: track.assetPath, category: 'BackgroundMusic', sha256: track.sha256, bytes: track.bytes, sourceUrl: track.sourceUrl, conversion: track.trimDurationSeconds ? 'first-120-seconds-pcm-wav' : 'byte-copy' };
  manifest.assets = manifest.assets.filter(a => a.destination !== track.assetPath && a.destination !== 'Assets/GuanshanYouyan/Audio/Music/' + track.originalFile);
  manifest.assets.push(entry);
  manifest.archive = manifest.archive.filter(a => a.destination !== track.archivePath);
  manifest.archive.push({ source: entry.source, destination: track.archivePath, sha256: track.originalSha256, bytes: archive.length, sourceUrl: track.sourceUrl });
}
const library = { schemaVersion: 1, addedAt: new Date().toISOString(), playbackMode: 'simultaneous-layers', enabledTrackIds: ['297537', '519335'], loop: true, tracks };
fs.writeFileSync(manifestPath, JSON.stringify(manifest, null, 2));
fs.writeFileSync(path.join(root, 'Assets/GuanshanYouyan/Data/bgm-library.json'), JSON.stringify(library, null, 2));
console.log(JSON.stringify({ resources: manifest.assets.length, tracks: tracks.map(t => ({ id: t.id, bytes: t.bytes, sha256: t.sha256 })) }, null, 2));
