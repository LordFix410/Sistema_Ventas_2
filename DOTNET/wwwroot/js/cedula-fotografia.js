const fileInput = document.getElementById('sourceImage');
const canvas = document.getElementById('cropCanvas');
const ctx = canvas.getContext('2d');
const cropGuides = document.getElementById('cropGuides');
const cropGuideInner = cropGuides.querySelector('span');
const statusLabel = document.getElementById('sourceStatus');
const addButton = document.getElementById('addPhoto');
const widthInput = document.getElementById('photoWidth');
const heightInput = document.getElementById('photoHeight');
const photoPresetInput = document.getElementById('photoPreset');
const includeMarginInput = document.getElementById('idIncludeMargin');
const paperSizeInput = document.getElementById('idPaperSize');
const photoSizeSummary = document.getElementById('photoSizeSummary');
const zoomInput = document.getElementById('zoom');
const filterInput = document.getElementById('filterPreset');
const borderInput = document.getElementById('whiteBorder');
const backgroundInput = document.getElementById('backgroundColor');
const removeBackgroundButton = document.getElementById('removeBackground');
const backgroundProgress = document.getElementById('backgroundProgress');
const backgroundProgressText = document.getElementById('backgroundProgressText');
const queueElement = document.getElementById('photoQueue');
const queueCount = document.getElementById('queueCount');
const sheetsElement = document.getElementById('idSheets');
const summary = document.getElementById('sheetSummary');
const printButton = document.getElementById('printIdPhotos');
const wordButton = document.getElementById('wordIdPhotos');
const colorModeButton = document.getElementById('idColorMode');
const printStyle = document.getElementById('idPrintStyle');
const compositionCanvas = document.createElement('canvas');
const compositionContext = compositionCanvas.getContext('2d');

let sourceImage = null;
let segmentedSource = null;
let showSegmentedSource = false;
let segmenterPromise = null;
let birefnetPromise = null;
let sourceName = '';
let sourceMimeType = 'image/jpeg';
let panX = 0;
let panY = 0;
let pointer = null;
let outputPhotos = [];
let workStarted = false;
let blackWhiteMode = false;
let renderedBorder = 0;
const imageUrls = new WeakMap();
const cmToPx = cm => cm * 96 / 2.54;
const cmToTwips = cm => Math.round(cm * 567);
const cmToEmu = cm => Math.round(cm * 360000);

function objectUrl(blob) {
  if (!imageUrls.has(blob)) imageUrls.set(blob, URL.createObjectURL(blob));
  return imageUrls.get(blob);
}

function filters() {
  if (filterInput.value === 'natural') return 'saturate(1.06) contrast(1.03) brightness(1.02)';
  if (filterInput.value === 'bw') return 'grayscale(1) contrast(1.08)';
  if (filterInput.value === 'enhance') return 'brightness(1.08) contrast(1.12) saturate(1.15)';
  if (filterInput.value === 'warm') return 'sepia(.2) saturate(1.12)';
  return 'none';
}

function canvasSize() {
  const {widthCm, heightCm} = photoDimensions();
  const width = 600;
  return [width, Math.round(width * heightCm / widthCm)];
}

function photoDimensions() {
  const photoWidthCm = Math.max(1, Math.min(12, Number(widthInput.value) || 3.4));
  const photoHeightCm = Math.max(1, Math.min(16, Number(heightInput.value) || 4.1));
  const borderCm = Math.max(0, Math.min(5, Number(borderInput.value) || 0)) / 10;
  return {photoWidthCm, photoHeightCm, borderCm, widthCm: photoWidthCm, heightCm: photoHeightCm};
}

function updatePhotoSizeSummary() {
  if (!photoSizeSummary) return;
  const presetName = {cedula: 'Cédula', photo: 'Fotografía', custom: 'Personalizado'}[photoPresetInput.value];
  photoSizeSummary.textContent = `Tamaño final · ${presetName} · ${Number(widthInput.value) || 3.4} × ${Number(heightInput.value) || 4.1} cm · borde interno ${Number(borderInput.value) || 0} mm`;
}

function renderCrop() {
  updatePhotoSizeSummary();
  if (!sourceImage) return;
  const source = showSegmentedSource && segmentedSource ? segmentedSource : sourceImage;
  const sourceWidth = source.naturalWidth || source.width;
  const sourceHeight = source.naturalHeight || source.height;
  const [width, height] = canvasSize();
  if (canvas.width !== width || canvas.height !== height) {
    canvas.width = width;
    canvas.height = height;
    panX = panY = 0;
  }
  const dimensions = photoDimensions();
  renderedBorder = dimensions.borderCm * width / dimensions.widthCm;
  const innerWidth = Math.max(1, width - renderedBorder * 2);
  const innerHeight = Math.max(1, height - renderedBorder * 2);
  compositionCanvas.width = Math.max(1, Math.round(innerWidth));
  compositionCanvas.height = Math.max(1, Math.round(innerHeight));
  const baseScale = Math.max(innerWidth / sourceWidth, innerHeight / sourceHeight);
  const scale = baseScale * Number(zoomInput.value);
  const drawWidth = sourceWidth * scale;
  const drawHeight = sourceHeight * scale;
  compositionContext.clearRect(0, 0, compositionCanvas.width, compositionCanvas.height);
  compositionContext.fillStyle = backgroundInput.value || '#ffffff';
  compositionContext.fillRect(0, 0, compositionCanvas.width, compositionCanvas.height);
  compositionContext.drawImage(source, (compositionCanvas.width - drawWidth) / 2 + panX, (compositionCanvas.height - drawHeight) / 2 + panY, drawWidth, drawHeight);
  ctx.clearRect(0, 0, width, height);
  ctx.fillStyle = '#fff';
  ctx.fillRect(0, 0, width, height);
  ctx.save();
  ctx.filter = filters();
  ctx.drawImage(compositionCanvas, renderedBorder, renderedBorder, innerWidth, innerHeight);
  ctx.restore();
  ctx.filter = 'none';
  canvas.style.width = 'min(100%, 380px)';
  canvas.style.height = 'auto';
  const cssBorder = renderedBorder * canvas.clientWidth / canvas.width;
  cropGuideInner.style.inset = `${cssBorder}px`;
  addButton.disabled = false;
}

function loadImageFile(file) {
  if (!file) return;
  if (!file.type.startsWith('image/')) {
    statusLabel.textContent = 'Elegí un archivo de imagen compatible.';
    statusLabel.className = 'tip';
    return;
  }
  const image = new Image();
  const url = URL.createObjectURL(file);
  image.onload = () => {
    if (sourceImage) URL.revokeObjectURL(sourceImage.src);
    sourceImage = image;
    segmentedSource = null;
    showSegmentedSource = false;
    workStarted = true;
    sourceName = file.name;
    panX = panY = 0;
    zoomInput.value = '1';
    statusLabel.textContent = sourceName;
    statusLabel.className = 'photo-count';
    removeBackgroundButton.disabled = false;
    removeBackgroundButton.textContent = 'Quitar fondo';
    renderCrop();
    URL.revokeObjectURL(url);
  };
  image.onerror = () => {
    URL.revokeObjectURL(url);
    statusLabel.textContent = 'No se pudo abrir esta imagen.';
    statusLabel.className = 'tip';
  };
  image.src = url;
}

async function processIncomingFiles(incoming) {
  const files = [...incoming];
  let images = files.filter(file => file.type.startsWith('image/'));
  for (const archive of files.filter(file => /\.(zip|rar)$/i.test(file.name))) {
    statusLabel.textContent = `Abriendo ${archive.name}…`;
    statusLabel.className = 'photo-count';
    try { images.push(...await window.LocalFileTransfer.unpackArchive(archive)); }
    catch (error) { statusLabel.textContent = error.message; statusLabel.className = 'tip'; return; }
  }
  if (images.length) {
    loadImageFile(images[0]);
    if (images.length > 1) statusLabel.textContent = `${images[0].name} · el editor trabaja con una foto a la vez (${images.length} imágenes recibidas)`;
  }
}
fileInput.addEventListener('change', async event => {
  await processIncomingFiles(event.target.files);
  fileInput.value = '';
});

photoPresetInput.addEventListener('change', () => {
  workStarted = true;
  const preset = {cedula: [3.4, 4.1], photo: [10, 15]}[photoPresetInput.value];
  if (!preset) return;
  widthInput.value = String(preset[0]);
  heightInput.value = String(preset[1]);
  renderCrop();
});

removeBackgroundButton.addEventListener('click', async () => {
  if (!sourceImage) return;
  if (segmentedSource) {
    showSegmentedSource = !showSegmentedSource;
    removeBackgroundButton.textContent = showSegmentedSource ? 'Ver fondo original' : 'Ver fondo quitado';
    statusLabel.textContent = `${sourceName} · ${showSegmentedSource ? 'mostrando el recorte procesado' : 'mostrando el fondo original'} (el resultado se conserva).`;
    statusLabel.className = 'photo-count';
    renderCrop();
    return;
  }
  const imageToSegment = sourceImage;
  removeBackgroundButton.disabled = true;
  removeBackgroundButton.textContent = 'Procesando…';
  backgroundProgress.hidden = false;
  statusLabel.textContent = 'Cargando el modelo de recorte de personas…';
  statusLabel.className = 'photo-count';
  try {
    const {removeProductBackground} = await import('./background-removal.js');
    segmentedSource = await removeProductBackground(imageToSegment, text => { backgroundProgressText.textContent = text; });
    if (sourceImage !== imageToSegment) return;
    showSegmentedSource = true;
    removeBackgroundButton.textContent = 'Ver fondo original';
    statusLabel.textContent = `${sourceName} · fondo removido.`;
    statusLabel.className = 'photo-count';
    renderCrop();
  } catch (error) {
    statusLabel.textContent = error.message?.includes('No se encontró') ? error.message : 'No se pudo quitar el fondo. Revisá la conexión e intentá de nuevo.';
    statusLabel.className = 'tip';
    removeBackgroundButton.textContent = 'Quitar fondo';
  } finally {
    backgroundProgress.hidden = true;
    removeBackgroundButton.disabled = !sourceImage;
  }
});

const dropZone = document.querySelector('.drop');
let fileDragDepth = 0;
function hasDraggedFiles(event) {
  return [...(event.dataTransfer?.types || [])].includes('Files');
}
document.addEventListener('dragenter', event => {
  if (!hasDraggedFiles(event)) return;
  event.preventDefault();
  fileDragDepth++;
  document.body.classList.add('page-drop-active');
  dropZone.classList.add('drag-active');
});
document.addEventListener('dragover', event => {
  if (!hasDraggedFiles(event)) return;
  event.preventDefault();
  if (event.dataTransfer) event.dataTransfer.dropEffect = 'copy';
});
document.addEventListener('dragleave', event => {
  if (!hasDraggedFiles(event)) return;
  fileDragDepth = Math.max(0, fileDragDepth - 1);
  if (fileDragDepth === 0) {
    document.body.classList.remove('page-drop-active');
    dropZone.classList.remove('drag-active');
  }
});
document.addEventListener('drop', event => {
  if (!hasDraggedFiles(event)) return;
  event.preventDefault();
  fileDragDepth = 0;
  document.body.classList.remove('page-drop-active');
  dropZone.classList.remove('drag-active');
  processIncomingFiles(event.dataTransfer?.files || []);
});

window.LocalFileTransfer.take().then(processIncomingFiles).catch(error => { statusLabel.textContent = error.message; statusLabel.className = 'tip'; });

window.addEventListener('beforeunload', event => {
  if (!workStarted && !sourceImage && !outputPhotos.length) return;
  event.preventDefault();
  event.returnValue = '';
});

function canvasBlob(target) {
  return new Promise((resolve, reject) => {
    try {
      // PNG is a required canvas format and preserves the white border cleanly.
      const dataUrl = target.toDataURL('image/png');
      const match = /^data:(image\/png);base64,(.*)$/.exec(dataUrl);
      if (!match) throw Error('El navegador no pudo crear la vista previa de la foto.');
      const binary = atob(match[2]);
      const bytes = new Uint8Array(binary.length);
      for (let index = 0; index < binary.length; index++) bytes[index] = binary.charCodeAt(index);
      resolve(new Blob([bytes], {type: match[1]}));
    } catch (error) { reject(error); }
  });
}

for (const control of [widthInput, heightInput, zoomInput, filterInput, borderInput, backgroundInput]) {
  const onChange = () => { workStarted = true; photoPresetInput.value = 'custom'; renderCrop(); };
  control.addEventListener('input', onChange);
  control.addEventListener('change', onChange);
}

canvas.addEventListener('pointerdown', event => {
  if (!sourceImage) return;
  pointer = {id: event.pointerId, x: event.clientX, y: event.clientY};
  canvas.setPointerCapture(event.pointerId);
});
canvas.addEventListener('pointermove', event => {
  if (!pointer || pointer.id !== event.pointerId || !sourceImage) return;
  if (!canvas.clientWidth || !canvas.clientHeight) return;
  panX += (event.clientX - pointer.x) * (canvas.width / canvas.clientWidth);
  panY += (event.clientY - pointer.y) * (canvas.height / canvas.clientHeight);
  pointer.x = event.clientX;
  pointer.y = event.clientY;
  renderCrop();
});
canvas.addEventListener('pointerup', () => { pointer = null; });
canvas.addEventListener('pointercancel', () => { pointer = null; });

function currentPaper() { return paperSizeInput.value === 'oficio' ? [21.59, 33.02] : [21.59, 27.94]; }

function photoBlob() {
  return new Promise((resolve, reject) => {
    const {widthCm, heightCm} = photoDimensions();
    const ppcm = 300 / 2.54;
    const width = Math.round(widthCm * ppcm), height = Math.round(heightCm * ppcm);
    const output = document.createElement('canvas');
    output.width = width;
    output.height = height;
    const out = output.getContext('2d');
    out.fillStyle = '#fff';
    out.fillRect(0, 0, width, height);
    out.drawImage(canvas, 0, 0, width, height);
    const cutLine = Math.max(1, Math.round(ppcm * 0.02));
    out.strokeStyle = '#111';
    out.lineWidth = cutLine;
    out.strokeRect(cutLine / 2, cutLine / 2, width - cutLine, height - cutLine);
    canvasBlob(output)
      .then(blob => resolve({blob, widthCm, heightCm}))
      .catch(reject);
  });
}

addButton.addEventListener('click', async () => {
  if (!sourceImage) return;
  workStarted = true;
  addButton.disabled = true;
  try {
    const photo = await photoBlob();
    outputPhotos.push({name: sourceName, ...photo, copies: 1});
    renderQueue();
    statusLabel.textContent = `${sourceName} agregada. Podés cambiar las copias en la lista.`;
    statusLabel.className = 'photo-count';
    if (window.matchMedia('(max-width: 850px)').matches) {
      document.getElementById('previewTitle')?.scrollIntoView?.({behavior: 'smooth', block: 'start'});
    }
  } catch (error) {
    statusLabel.textContent = `No se pudo agregar la foto: ${error.message || 'formato no compatible'}`;
    statusLabel.className = 'tip';
  } finally {
    addButton.disabled = false;
  }
});

function renderQueue() {
  queueElement.replaceChildren();
  for (const [index, photo] of outputPhotos.entries()) {
    const row = document.createElement('div');
    row.className = 'queue-item';
    const img = document.createElement('img');
    img.src = objectUrl(photo.blob);
    img.alt = photo.name;
    const label = document.createElement('span');
    label.textContent = `${photo.name} · total con borde: ${photo.widthCm} × ${photo.heightCm} cm`;
    const copies = document.createElement('label');
    copies.className = 'queue-copies';
    copies.append('Copias');
    const quantity = document.createElement('input');
    quantity.type = 'number'; quantity.min = '1'; quantity.max = '100'; quantity.step = '1'; quantity.value = String(photo.copies); quantity.setAttribute('aria-label', `Copias de ${photo.name}`);
    quantity.addEventListener('change', () => { workStarted = true; photo.copies = Math.max(1, Math.min(100, Number(quantity.value) || 1)); quantity.value = String(photo.copies); renderQueue(); });
    copies.append(quantity);
    const remove = document.createElement('button');
    remove.type = 'button';
    remove.className = 'id-remove';
    remove.textContent = 'Quitar';
    remove.addEventListener('click', () => {
      workStarted = true;
      outputPhotos.splice(index, 1);
      renderQueue();
    });
    row.append(img, label, copies, remove);
    queueElement.append(row);
  }
  const total = outputPhotos.reduce((sum, photo) => sum + photo.copies, 0);
  queueCount.textContent = `${total} copia${total === 1 ? '' : 's'}`;
  printButton.disabled = wordButton.disabled = total === 0;
  renderSheets();
}

function makePlacements() {
  const [paperWidth, paperHeight] = currentPaper();
  const margin = includeMarginInput.checked ? 1 : 0, gap = 0.3;
  const pages = [];
  let page = [], x = margin, y = margin, rowHeight = 0;
  for (const photo of outputPhotos) {
    for (let copy = 0; copy < photo.copies; copy++) {
      if (photo.widthCm > paperWidth - margin * 2 || photo.heightCm > paperHeight - margin * 2) {
        return {pages: [], error: 'Una foto es más grande que el área imprimible del papel seleccionado.'};
      }
      if (x + photo.widthCm > paperWidth - margin + 0.001) {
        x = margin;
        y += rowHeight + gap;
        rowHeight = 0;
      }
      if (y + photo.heightCm > paperHeight - margin + 0.001) {
        if (page.length) pages.push(page);
        page = [];
        x = margin;
        y = margin;
        rowHeight = 0;
      }
      page.push({photo, x, y});
      x += photo.widthCm + gap;
      rowHeight = Math.max(rowHeight, photo.heightCm);
    }
  }
  if (page.length) pages.push(page);
  return {pages, paperWidth, paperHeight, margin};
}

function renderSheets() {
  sheetsElement.replaceChildren();
  const layout = makePlacements();
  if (layout.error) {
    summary.textContent = layout.error;
    summary.className = 'tip';
    printButton.disabled = wordButton.disabled = true;
    return;
  }
  const total = outputPhotos.reduce((sum, photo) => sum + photo.copies, 0);
  const [paperWidth, paperHeight] = currentPaper();
  const scale = 1;
  summary.className = 'muted';
  const paperLabel = paperSizeInput.value === 'oficio' ? 'Oficio' : 'Carta';
  summary.textContent = total ? `${total} copias · ${layout.pages.length} hoja${layout.pages.length === 1 ? '' : 's'} · ${paperLabel} · escala 100 % · ${includeMarginInput.checked ? 'margen 1 cm' : 'sin margen'} · separación 3 mm` : `Hoja ${paperLabel} · escala 100 % · agregá fotos para ver la vista previa.`;
  document.documentElement.style.setProperty('--id-paper-w', `${paperWidth}cm`);
  document.documentElement.style.setProperty('--id-paper-h', `${paperHeight}cm`);
  printStyle.textContent = `@media print{@page{size:${paperWidth}cm ${paperHeight}cm;margin:0}}`;
  layout.pages.forEach((placements, pageIndex) => {
    const shell = document.createElement('div');
    shell.className = 'id-sheet-shell';
    shell.style.height = `${cmToPx(paperHeight) * scale}px`;
    const sheet = document.createElement('div');
    sheet.className = 'id-sheet';
    sheet.style.width = `${paperWidth}cm`;
    sheet.style.height = `${paperHeight}cm`;
    sheet.style.transform = `scale(${scale})`;
    sheet.style.transformOrigin = 'top center';
    for (const placement of placements) {
      const img = document.createElement('img');
      img.className = 'id-print-photo';
      img.src = objectUrl(placement.photo.blob);
      img.alt = placement.photo.name;
      img.style.filter = blackWhiteMode ? 'grayscale(1)' : '';
      img.style.left = `${placement.x}cm`;
      img.style.top = `${placement.y}cm`;
      img.style.width = `${placement.photo.widthCm}cm`;
      img.style.height = `${placement.photo.heightCm}cm`;
      sheet.append(img);
    }
    shell.append(sheet);
    sheetsElement.append(shell);
  });
  if (total) wordButton.disabled = printButton.disabled = false;
}

window.addEventListener('resize', renderSheets);
includeMarginInput.addEventListener('change', () => { workStarted = true; renderSheets(); });
paperSizeInput.addEventListener('change', () => { workStarted = true; renderSheets(); });
document.getElementById('printIdPhotos').addEventListener('click', () => window.print());
colorModeButton.addEventListener('click', () => {
  blackWhiteMode = !blackWhiteMode;
  workStarted = true;
  colorModeButton.setAttribute('aria-pressed', String(blackWhiteMode));
  colorModeButton.textContent = blackWhiteMode ? 'B/N' : 'Color';
  renderSheets();
});

document.getElementById('pickBackground').addEventListener('click', async () => {
  if (!('EyeDropper' in window)) {
    statusLabel.textContent = 'Este navegador no ofrece gotero; elegí el color desde Fondo.';
    statusLabel.className = 'tip';
    backgroundInput.click();
    return;
  }
  try {
    const result = await new EyeDropper().open();
    backgroundInput.value = result.sRGBHex;
    renderCrop();
  } catch (error) { if (error.name !== 'AbortError') { statusLabel.textContent = 'No se pudo tomar el color. Elegilo desde Fondo.'; statusLabel.className = 'tip'; } }
});

const encoder = new TextEncoder();
function le16(value) { return new Uint8Array([value & 255, (value >>> 8) & 255]); }
function le32(value) { return new Uint8Array([value & 255, (value >>> 8) & 255, (value >>> 16) & 255, (value >>> 24) & 255]); }
function bytesTogether(parts) {
  const result = new Uint8Array(parts.reduce((sum, part) => sum + part.length, 0));
  let offset = 0;
  for (const part of parts) { result.set(part, offset); offset += part.length; }
  return result;
}
function crc32(data) {
  let crc = 0xffffffff;
  for (const byte of data) {
    crc ^= byte;
    for (let bit = 0; bit < 8; bit++) crc = (crc >>> 1) ^ ((crc & 1) ? 0xedb88320 : 0);
  }
  return (crc ^ 0xffffffff) >>> 0;
}
function makeZip(entries) {
  const localParts = [], centralParts = [];
  let offset = 0;
  for (const [name, data] of entries) {
    const nameBytes = encoder.encode(name), crc = crc32(data);
    const local = bytesTogether([le32(0x04034b50), le16(20), le16(0x0800), le16(0), le16(0), le16(0), le32(crc), le32(data.length), le32(data.length), le16(nameBytes.length), le16(0), nameBytes]);
    localParts.push(local, data);
    centralParts.push(bytesTogether([le32(0x02014b50), le16(20), le16(20), le16(0x0800), le16(0), le16(0), le16(0), le32(crc), le32(data.length), le32(data.length), le16(nameBytes.length), le16(0), le16(0), le16(0), le16(0), le32(0), le32(offset), nameBytes]));
    offset += local.length + data.length;
  }
  const central = bytesTogether(centralParts);
  const end = bytesTogether([le32(0x06054b50), le16(0), le16(0), le16(entries.length), le16(entries.length), le32(central.length), le32(offset), le16(0)]);
  return bytesTogether([...localParts, central, end]);
}

function drawingAnchor(imageId, photo, placement) {
  const width = cmToEmu(photo.widthCm), height = cmToEmu(photo.heightCm);
  const left = cmToEmu(placement.x), top = cmToEmu(placement.y);
  return `<w:r><w:drawing><wp:anchor distT="0" distB="0" distL="0" distR="0" simplePos="0" relativeHeight="${imageId}" behindDoc="0" locked="0" layoutInCell="1" allowOverlap="1"><wp:simplePos x="0" y="0"/><wp:positionH relativeFrom="page"><wp:posOffset>${left}</wp:posOffset></wp:positionH><wp:positionV relativeFrom="page"><wp:posOffset>${top}</wp:posOffset></wp:positionV><wp:extent cx="${width}" cy="${height}"/><wp:effectExtent l="0" t="0" r="0" b="0"/><wp:wrapNone/><wp:docPr id="${imageId}" name="${photo.name.replace(/[&<>"']/g, '')}"/><wp:cNvGraphicFramePr><a:graphicFrameLocks noChangeAspect="1"/></wp:cNvGraphicFramePr><a:graphic><a:graphicData uri="http://schemas.openxmlformats.org/drawingml/2006/picture"><pic:pic><pic:nvPicPr><pic:cNvPr id="${imageId}" name="Foto ${imageId}"/><pic:cNvPicPr/></pic:nvPicPr><pic:blipFill><a:blip r:embed="rId${imageId}"/><a:stretch><a:fillRect/></a:stretch></pic:blipFill><pic:spPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="${width}" cy="${height}"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom></pic:spPr></pic:pic></a:graphicData></a:graphic></wp:anchor></w:drawing></w:r>`;
}

async function exportWord() {
  const layout = makePlacements();
  if (layout.error || !layout.pages.length) return alert(layout.error || 'Agregá al menos una foto.');
  const all = layout.pages.flatMap(page => page);
  const wordImages = blackWhiteMode ? await Promise.all(all.map(item => grayscaleBlob(item.photo.blob))) : all.map(item => item.photo.blob);
  const extensions = wordImages.map(blob => blob.type === 'image/png' ? 'png' : 'jpg');
  const relationships = all.map((_, index) => `<Relationship Id="rId${index + 1}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/image" Target="media/image${index + 1}.${extensions[index]}"/>`).join('');
  let body = '';
  for (let pageIndex = 0; pageIndex < layout.pages.length; pageIndex++) {
    const pagePhotos = layout.pages[pageIndex];
    // Each copy gets its own Word relationship so every picture stays independently editable.
    const pageStart = layout.pages.slice(0, pageIndex).reduce((sum, page) => sum + page.length, 0);
    const anchored = pagePhotos.map((placement, index) => drawingAnchor(pageStart + index + 1, placement.photo, placement)).join('');
    body += `<w:p><w:pPr><w:spacing w:before="0" w:after="0"/>${pageIndex ? '<w:pageBreakBefore/>' : ''}</w:pPr>${anchored}</w:p>`;
  }
  const pageWidth = cmToTwips(layout.paperWidth), pageHeight = cmToTwips(layout.paperHeight);
  const wordMargin = cmToTwips(includeMarginInput.checked ? 1 : 0);
  const documentXml = `<?xml version="1.0" encoding="UTF-8" standalone="yes"?><w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:wp="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing" xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:pic="http://schemas.openxmlformats.org/drawingml/2006/picture"><w:body>${body}<w:sectPr><w:pgSz w:w="${pageWidth}" w:h="${pageHeight}"/><w:pgMar w:top="${wordMargin}" w:right="${wordMargin}" w:bottom="${wordMargin}" w:left="${wordMargin}" w:header="${wordMargin}" w:footer="${wordMargin}" w:gutter="0"/></w:sectPr></w:body></w:document>`;
  const contentTypes = `<?xml version="1.0" encoding="UTF-8"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Default Extension="jpg" ContentType="image/jpeg"/><Default Extension="png" ContentType="image/png"/><Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/></Types>`;
  const rootRels = `<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/></Relationships>`;
  const entries = [
    ['[Content_Types].xml', encoder.encode(contentTypes)],
    ['_rels/.rels', encoder.encode(rootRels)],
    ['word/document.xml', encoder.encode(documentXml)],
    ['word/_rels/document.xml.rels', encoder.encode(`<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">${relationships}</Relationships>`)]
  ];
  for (let i = 0; i < all.length; i++) entries.push([`word/media/image${i + 1}.${extensions[i]}`, new Uint8Array(await wordImages[i].arrayBuffer())]);
  const blob = new Blob([makeZip(entries)], {type: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document'});
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = 'cedula-y-fotografia.docx';
  link.click();
  setTimeout(() => URL.revokeObjectURL(url), 5000);
}

async function grayscaleBlob(blob) {
  const bitmap = await createImageBitmap(blob);
  const canvas = document.createElement('canvas');
  canvas.width = bitmap.width;
  canvas.height = bitmap.height;
  const context = canvas.getContext('2d', {willReadFrequently: true});
  context.drawImage(bitmap, 0, 0);
  bitmap.close();
  const pixels = context.getImageData(0, 0, canvas.width, canvas.height);
  for (let index = 0; index < pixels.data.length; index += 4) {
    const gray = Math.round(pixels.data[index] * 0.299 + pixels.data[index + 1] * 0.587 + pixels.data[index + 2] * 0.114);
    pixels.data[index] = pixels.data[index + 1] = pixels.data[index + 2] = gray;
  }
  context.putImageData(pixels, 0, 0);
  return new Promise((resolve, reject) => canvas.toBlob(result => result ? resolve(result) : reject(Error('No se pudo convertir una foto a blanco y negro.')), 'image/png'));
}

wordButton.addEventListener('click', () => exportWord().catch(error => alert(error.message)));
renderQueue();
