const imageGrid = document.getElementById('photoGrid');
const photoStatus = document.getElementById('photoStatus');
const sheets = document.getElementById('sheets');
const widthInput = document.getElementById('photoWidth');
const heightInput = document.getElementById('photoHeight');
const squareModeInput = document.getElementById('squareMode');
const heightField = document.getElementById('heightField');
const widthLabel = document.getElementById('photoWidthLabel');
const photoPresetInput = document.getElementById('photoPreset');
const photoOrientationInput = document.getElementById('photoOrientation');
const photoOrientationField = document.getElementById('photoOrientationField');
const paperInput = document.getElementById('paperSize');
const paperOrientationInput = document.getElementById('paperOrientation');
const fitInput = document.getElementById('fitMode');
const pageFitInput = document.getElementById('pageFitMode');
const pageFitNotice = document.getElementById('pageFitNotice');
const ratioInput = document.getElementById('ratioMode');
const ratioAInput = document.getElementById('ratioA');
const ratioBInput = document.getElementById('ratioB');
const includeMarginInput = document.getElementById('includeMargin');
const photoBorderInput = document.getElementById('photoBorder');
const colorModeButton = document.getElementById('photoColorMode');
const photoScaleField = document.getElementById('photoScaleField');
const photoWidthScale = document.getElementById('photoWidthScale');
const photoSizeValue = document.getElementById('photoSizeValue');
const printButton = document.getElementById('printPhotos');
const wordButton = document.getElementById('wordPhotos');
const enterScalePreviewButton = document.getElementById('enterScalePreview');
const exitScalePreviewButton = document.getElementById('exitScalePreview');
const scalePreviewBar = document.getElementById('scalePreviewBar');
const scalePreviewSlot = document.getElementById('scalePreviewSlot');
const scalePreviewOutputActions = document.getElementById('scalePreviewOutputActions');
const scalePreviewTitle = document.getElementById('scalePreviewTitle');
const layoutInfo = document.getElementById('layoutInfo');
const printPageStyle = document.getElementById('printPageStyle');
const scaleFieldHome = photoScaleField.parentElement;
const scaleFieldNextSibling = photoScaleField.nextSibling;
const outputControls = document.querySelector('.output-controls');
const outputControlsHome = outputControls.parentElement;
const outputControlsNextSibling = outputControls.nextSibling;

let photos = [];
let pageFitPercent = 100;
let workStarted = false;
let zipLoadingCount = 0;
const objectUrls = new WeakMap();
const photoRotations = new WeakMap();
const cmToPx = cm => cm * 96 / 2.54;
const cmToTwips = cm => Math.round(cm * 567);
const cmToEmu = cm => Math.round(cm * 360000);
const naturalSort = (a, b) => a.name.localeCompare(b.name, undefined, {numeric: true, sensitivity: 'base'});

function objectUrl(file) {
  if (!objectUrls.has(file)) objectUrls.set(file, URL.createObjectURL(file));
  return objectUrls.get(file);
}

function addPhotos(incoming) {
  const images = [...incoming].filter(file => file.type.startsWith('image/'));
  if (images.length) workStarted = true;
  photos = [...photos, ...images].sort(naturalSort);
  render();
}

async function addArchive(archive) {
  if (!archive) return;
  workStarted = true;
  zipLoadingCount++;
  photoStatus.textContent = `Abriendo ${archive.name}…`;
  photoStatus.className = 'photo-count';
  try {
    const unpacked = await window.LocalFileTransfer.unpackArchive(archive);
    if (!unpacked.length) throw new Error(`${archive.name} no contiene imágenes compatibles.`);
    addPhotos(unpacked);
  } catch (error) {
    photoStatus.textContent = error.message;
    photoStatus.className = 'tip';
  } finally {
    zipLoadingCount--;
  }
}

let dragDepth = 0;
function hasFileDrag(event) {
  return [...(event.dataTransfer?.types || [])].includes('Files');
}
document.addEventListener('dragenter', event => {
  if (!hasFileDrag(event)) return;
  event.preventDefault();
  dragDepth++;
  document.body.classList.add('page-drop-active');
});
document.addEventListener('dragover', event => {
  if (!hasFileDrag(event)) return;
  event.preventDefault();
  event.dataTransfer.dropEffect = 'copy';
});
document.addEventListener('dragleave', event => {
  if (!hasFileDrag(event)) return;
  dragDepth = Math.max(0, dragDepth - 1);
  if (!dragDepth) document.body.classList.remove('page-drop-active');
});
document.addEventListener('drop', async event => {
  if (!hasFileDrag(event)) return;
  event.preventDefault();
  dragDepth = 0;
  document.body.classList.remove('page-drop-active');
  const dropped = [...event.dataTransfer.files];
  addPhotos(dropped);
  for (const file of dropped.filter(item => /\.(zip|rar)$/i.test(item.name))) await addArchive(file);
});

window.addEventListener('beforeunload', event => {
  if (!workStarted && !photos.length && !zipLoadingCount) return;
  event.preventDefault();
  event.returnValue = '';
});

function paperDimensions() {
  const dimensions = paperInput.value === 'oficio' ? [21.59, 33.02] : [21.59, 27.94];
  return paperOrientationInput.value === 'landscape' ? dimensions.reverse() : dimensions;
}

function ratioValues() {
  if (ratioInput.value === 'none') return null;
  if (ratioInput.value === 'custom') return [Math.max(0.1, Number(ratioAInput.value) || 3), Math.max(0.1, Number(ratioBInput.value) || 4)];
  return ratioInput.value.split(':').map(Number);
}

function currentAspectRatio() {
  if (squareModeInput.checked) return [1, 1];
  const ratio = ratioValues();
  if (ratio) return ratio;
  return [Math.max(0.1, Number(widthInput.value) || 5), Math.max(0.1, Number(heightInput.value) || 5)];
}

function applyRatioFromWidth() {
  if (squareModeInput.checked) { heightInput.value = widthInput.value; return; }
  const ratio = ratioValues();
  if (ratio) heightInput.value = (Number(widthInput.value || 5) * ratio[1] / ratio[0]).toFixed(2);
}

function maxRatioWidth() {
  const ratio = currentAspectRatio();
  const [paperWidth, paperHeight] = paperDimensions();
  const margin = includeMarginInput.checked ? 1 : 0;
  const limit = Math.min(15, paperWidth - margin * 2, (paperHeight - margin * 2) * ratio[0] / ratio[1]);
  return Math.max(1, Math.floor(limit * 10 + 1e-6) / 10);
}

function updatePhotoSizeLabel() {
  if (pageFitInput.checked) {
    const [paperWidth, paperHeight] = paperDimensions();
    const margin = includeMarginInput.checked ? 1 : 0;
    photoSizeValue.textContent = `${((paperWidth - margin * 2) * pageFitPercent / 100).toFixed(2)} × ${((paperHeight - margin * 2) * pageFitPercent / 100).toFixed(2)} cm`;
    return;
  }
  const width = Number(widthInput.value) || 5;
  const ratio = squareModeInput.checked ? [1, 1] : ratioValues();
  const height = ratio ? width * ratio[1] / ratio[0] : Number(heightInput.value) || width;
  photoSizeValue.textContent = `${width.toFixed(2)} × ${height.toFixed(2)} cm`;
}

function settings() {
  const [paperWidth, paperHeight] = paperDimensions();
  const margin = includeMarginInput.checked ? 1 : 0;
  if (pageFitInput.checked) {
    return {paperWidth, paperHeight, photoWidth: (paperWidth - margin * 2) * pageFitPercent / 100, photoHeight: (paperHeight - margin * 2) * pageFitPercent / 100, margin, gap: 0, columns: 1, rows: 1, perPage: 1, pageFit: true};
  }
  const photoWidth = Math.max(1, Math.min(15, Number(widthInput.value) || 5));
  const ratio = squareModeInput.checked ? [1, 1] : ratioValues();
  const photoHeight = squareModeInput.checked ? photoWidth : ratio ? photoWidth * ratio[1] / ratio[0] : Math.max(1, Math.min(40, Number(heightInput.value) || 5));
  const gap = 0.1;
  const columns = Math.max(1, Math.floor((paperWidth - margin * 2 + gap) / (photoWidth + gap)));
  const rows = Math.max(1, Math.floor((paperHeight - margin * 2 + gap) / (photoHeight + gap)));
  return {paperWidth, paperHeight, photoWidth, photoHeight, margin, gap, columns, rows, perPage: columns * rows};
}

function render() {
  imageGrid.replaceChildren();
  for (const file of photos) {
    const figure = document.createElement('figure');
    const image = document.createElement('img');
    image.src = objectUrl(file);
    image.alt = file.name;
    image.style.transform = `rotate(${photoRotations.get(file) || 0}deg)`;
    const caption = document.createElement('figcaption');
    caption.textContent = file.name;
    const remove = document.createElement('button');
    remove.type = 'button';
    remove.className = 'remove-photo';
    remove.textContent = 'Quitar';
    remove.addEventListener('click', () => {
      photos = photos.filter(photo => photo !== file);
      render();
    });
    const actions = document.createElement('div');
    actions.className = 'preview-photo-actions';
    for (const [label, rotation] of [['↶ Girar a la izquierda', -90], ['↷ Girar a la derecha', 90]]) {
      const rotate = document.createElement('button');
      rotate.type = 'button';
      rotate.className = 'rotate-photo';
      rotate.textContent = label.startsWith('↶') ? '↶ 90°' : '↷ 90°';
      rotate.title = label;
      rotate.setAttribute('aria-label', `${label}: ${file.name}`);
      rotate.addEventListener('click', () => {
        workStarted = true;
        photoRotations.set(file, ((photoRotations.get(file) || 0) + rotation + 360) % 360);
        render();
      });
      actions.append(rotate);
    }
    actions.append(remove);
    figure.append(image, caption, actions);
    imageGrid.append(figure);
  }
  photoStatus.textContent = photos.length
    ? `${photos.length} imagen${photos.length === 1 ? '' : 'es'} cargada${photos.length === 1 ? '' : 's'}.`
    : 'Arrastrá imágenes o un archivo ZIP/RAR a cualquier parte de la página.';
  photoStatus.className = photos.length ? 'photo-count' : 'empty';
  printButton.disabled = wordButton.disabled = photos.length === 0;
  enterScalePreviewButton.disabled = photos.length === 0;
  renderSheets();
}

function renderSheets() {
  sheets.replaceChildren();
  if (!photos.length) {
    layoutInfo.textContent = 'La hoja y las fotos aparecerán aquí.';
    return;
  }
  const config = settings();
  const photoExceedsPaper = config.photoWidth > config.paperWidth - config.margin * 2 || config.photoHeight > config.paperHeight - config.margin * 2;
  if (photoExceedsPaper) {
    layoutInfo.textContent = `La foto de ${config.photoWidth.toFixed(2)} × ${config.photoHeight.toFixed(2)} cm no cabe en la hoja con el margen elegido.`;
    printButton.disabled = wordButton.disabled = true;
    enterScalePreviewButton.disabled = true;
    return;
  }
  printButton.disabled = wordButton.disabled = false;
  enterScalePreviewButton.disabled = false;
  const pageCount = Math.ceil(photos.length / config.perPage);
  const colorMode = colorModeButton.getAttribute('aria-pressed') === 'true' ? 'blanco y negro' : 'color';
  layoutInfo.textContent = `${config.columns} por ${config.rows} · ${config.perPage} fotos por hoja · ${pageCount} hoja${pageCount === 1 ? '' : 's'} · ${config.photoWidth.toFixed(2)} × ${config.photoHeight.toFixed(2)} cm · margen ${config.margin ? '1 cm' : 'sin margen'} · ${colorMode}`;
  document.documentElement.style.setProperty('--paper-w', `${config.paperWidth}cm`);
  document.documentElement.style.setProperty('--paper-h', `${config.paperHeight}cm`);
  printPageStyle.textContent = `@media print{@page{size:${config.paperWidth}cm ${config.paperHeight}cm;margin:0}html,body{width:${config.paperWidth}cm!important;margin:0!important;padding:0!important}.print-sheet{width:${config.paperWidth}cm!important;height:${config.paperHeight}cm!important;box-sizing:border-box!important}}`;
  const isScalePreview = document.body.classList.contains('photo-preview-100');
  const scale = isScalePreview ? 1 : Math.min(1, (sheets.clientWidth - 12) / cmToPx(config.paperWidth));
  for (let start = 0, page = 0; start < photos.length; start += config.perPage, page++) {
    const shell = document.createElement('div');
    shell.className = 'sheet-shell';
    shell.style.width = `${cmToPx(config.paperWidth) * scale}px`;
    shell.style.height = `${cmToPx(config.paperHeight) * scale}px`;
    const sheet = document.createElement('div');
    sheet.className = 'print-sheet';
    sheet.style.width = `${config.paperWidth}cm`;
    sheet.style.height = `${config.paperHeight}cm`;
    sheet.style.padding = `${config.margin}cm`;
    sheet.style.gap = `${config.gap}cm`;
    sheet.style.gridTemplateColumns = `repeat(${config.columns}, ${config.photoWidth}cm)`;
    sheet.style.transform = `scale(${scale})`;
    sheet.style.transformOrigin = 'top center';
    for (const file of photos.slice(start, start + config.perPage)) {
      const frame = document.createElement('div');
      frame.className = `photo-tile${fitInput.value === 'contain' ? ' contain' : ''}${photoBorderInput.checked ? '' : ' no-border'}`;
      frame.style.width = `${config.photoWidth}cm`;
      frame.style.height = `${config.photoHeight}cm`;
      const image = document.createElement('img');
      image.src = objectUrl(file);
      image.alt = file.name;
      const rotation = photoRotations.get(file) || 0;
      if (rotation % 180) {
        image.style.position = 'absolute';
        image.style.width = `${config.photoHeight}cm`;
        image.style.height = `${config.photoWidth}cm`;
        image.style.left = '50%';
        image.style.top = '50%';
        image.style.transform = `translate(-50%, -50%) rotate(${rotation}deg)`;
        frame.style.position = 'relative';
      } else if (rotation) image.style.transform = `rotate(${rotation}deg)`;
      if (colorModeButton.getAttribute('aria-pressed') === 'true') image.style.filter = 'grayscale(1)';
      frame.append(image);
      sheet.append(frame);
    }
    shell.append(sheet);
    sheets.append(shell);
  }
}

function syncDimensionControls() {
  const pageFit = pageFitInput.checked;
  pageFitNotice.hidden = !pageFit;
  document.getElementById('photoSettings').classList.toggle('page-fit-active', pageFit);
  for (const input of [photoPresetInput, photoOrientationInput, squareModeInput, widthInput, heightInput, ratioInput, ratioAInput, ratioBInput]) input.disabled = pageFit;
  photoScaleField.hidden = false;
  const square = squareModeInput.checked;
  const ratio = ratioInput.value !== 'none';
  heightField.hidden = square || ratio;
  document.getElementById('ratioCustomAField').hidden = ratioInput.value !== 'custom';
  document.getElementById('ratioCustomBField').hidden = ratioInput.value !== 'custom';
  widthLabel.textContent = square ? 'Medida de cada lado (cm)' : 'Ancho de cada foto (cm)';
  if (pageFit) {
    photoScaleField.querySelector('.photo-scale-caption').textContent = 'Tamaño de foto (% del área disponible)';
    photoWidthScale.min = '25'; photoWidthScale.max = '100'; photoWidthScale.step = '1';
    photoWidthScale.value = String(pageFitPercent);
  } else {
    photoScaleField.querySelector('.photo-scale-caption').textContent = 'Medida resultante';
    photoWidthScale.min = '1'; photoWidthScale.max = maxRatioWidth().toFixed(1); photoWidthScale.step = '0.1';
    if (ratio && Number(widthInput.value) > Number(photoWidthScale.max)) widthInput.value = photoWidthScale.max;
    if (square || ratio) applyRatioFromWidth();
    photoWidthScale.value = String(Math.min(Number(widthInput.value) || 5, Number(photoWidthScale.max)));
  }
  updatePhotoSizeLabel();
  photoOrientationField.hidden = photoPresetInput.value !== 'photo';
}

photoPresetInput.addEventListener('change', () => {
  workStarted = true;
  const presets = {cedula: [2.5, 3], photo: photoOrientationInput.value === 'landscape' ? [15, 10] : [10, 15]};
  if (presets[photoPresetInput.value]) {
    const [width, height] = presets[photoPresetInput.value];
    squareModeInput.checked = false;
    ratioInput.value = 'none';
    widthInput.value = String(width);
    heightInput.value = String(height);
  } else {
    // Al volver a personalizado, no hereda las medidas de orientación del preset.
    squareModeInput.checked = false;
    ratioInput.value = 'none';
    widthInput.value = '5';
    heightInput.value = '5';
  }
  syncDimensionControls();
  renderSheets();
});

photoOrientationInput.addEventListener('change', () => {
  if (photoPresetInput.value !== 'photo') return;
  workStarted = true;
  const [width, height] = photoOrientationInput.value === 'landscape' ? [15, 10] : [10, 15];
  widthInput.value = String(width);
  heightInput.value = String(height);
  syncDimensionControls();
  renderSheets();
});

for (const control of [paperInput, fitInput]) control.addEventListener('input', () => { workStarted = true; renderSheets(); });
for (const control of [paperInput, paperOrientationInput, includeMarginInput, pageFitInput]) control.addEventListener('change', () => { workStarted = true; syncDimensionControls(); renderSheets(); });
for (const control of [ratioInput, ratioAInput, ratioBInput]) control.addEventListener('input', () => {
  workStarted = true;
  photoPresetInput.value = 'custom';
  if (control === ratioInput && ratioInput.value !== 'none') squareModeInput.checked = false;
  if (ratioInput.value !== 'none') applyRatioFromWidth();
  syncDimensionControls();
  renderSheets();
});
widthInput.addEventListener('input', () => {
  workStarted = true;
  if (photoPresetInput.value !== 'custom') photoPresetInput.value = 'custom';
  applyRatioFromWidth();
  syncDimensionControls();
  renderSheets();
});
photoWidthScale.addEventListener('input', () => {
  workStarted = true;
  if (pageFitInput.checked) {
    pageFitPercent = Number(photoWidthScale.value);
    updatePhotoSizeLabel();
    renderSheets();
    return;
  }
  const [ratioWidth, ratioHeight] = currentAspectRatio();
  photoPresetInput.value = 'custom';
  widthInput.value = photoWidthScale.value;
  if (squareModeInput.checked) heightInput.value = widthInput.value;
  else if (ratioInput.value !== 'none') applyRatioFromWidth();
  else heightInput.value = (Number(widthInput.value) * ratioHeight / ratioWidth).toFixed(2);
  syncDimensionControls();
  renderSheets();
});
heightInput.addEventListener('input', () => {
  workStarted = true;
  photoPresetInput.value = 'custom';
  squareModeInput.checked = false;
  ratioInput.value = 'none';
  syncDimensionControls();
  renderSheets();
});
squareModeInput.addEventListener('change', () => {
  workStarted = true;
  photoPresetInput.value = 'custom';
  if (squareModeInput.checked) ratioInput.value = 'none';
  syncDimensionControls();
  renderSheets();
});
paperInput.addEventListener('change', renderSheets);
window.addEventListener('resize', renderSheets);
printButton.addEventListener('click', () => window.print());
photoBorderInput.addEventListener('change', () => { workStarted = true; renderSheets(); });
colorModeButton.addEventListener('click', () => {
  workStarted = true;
  const blackWhite = colorModeButton.getAttribute('aria-pressed') !== 'true';
  colorModeButton.setAttribute('aria-pressed', String(blackWhite));
  colorModeButton.textContent = blackWhite ? 'B/N' : 'Color';
  renderSheets();
});

function enterScalePreview() {
  if (enterScalePreviewButton.disabled) return;
  const config = settings();
  const paperName = paperInput.value === 'oficio' ? 'Oficio' : 'Carta';
  const orientationName = paperOrientationInput.value === 'landscape' ? 'horizontal' : 'vertical';
  scalePreviewTitle.textContent = `100% · ${paperName} ${orientationName} · ${config.paperWidth.toFixed(2)} × ${config.paperHeight.toFixed(2)} cm`;
  scalePreviewBar.hidden = false;
  scalePreviewSlot.append(photoScaleField);
  scalePreviewOutputActions.append(outputControls);
  document.body.classList.add('photo-preview-100');
  renderSheets();
  exitScalePreviewButton.focus();
}

function exitScalePreview() {
  if (scaleFieldNextSibling && scaleFieldNextSibling.parentNode === scaleFieldHome) scaleFieldHome.insertBefore(photoScaleField, scaleFieldNextSibling);
  else scaleFieldHome.append(photoScaleField);
  if (outputControlsNextSibling && outputControlsNextSibling.parentNode === outputControlsHome) outputControlsHome.insertBefore(outputControls, outputControlsNextSibling);
  else outputControlsHome.append(outputControls);
  document.body.classList.remove('photo-preview-100');
  scalePreviewBar.hidden = true;
  renderSheets();
  enterScalePreviewButton.focus();
}

enterScalePreviewButton.addEventListener('click', enterScalePreview);
exitScalePreviewButton.addEventListener('click', exitScalePreview);
document.addEventListener('keydown', event => {
  if (event.key === 'Escape' && document.body.classList.contains('photo-preview-100')) exitScalePreview();
});

function preparePhoto(file, widthCm, heightCm, fit, blackWhite, border, rotation = 0) {
  return new Promise((resolve, reject) => {
    const image = new Image();
    const source = URL.createObjectURL(file);
    image.onload = () => {
      const canvas = document.createElement('canvas');
      const widthPixels = Math.max(300, Math.round(widthCm / 2.54 * 300));
      const heightPixels = Math.max(300, Math.round(heightCm / 2.54 * 300));
      canvas.width = widthPixels;
      canvas.height = heightPixels;
      const ctx = canvas.getContext('2d');
      ctx.fillStyle = '#fff';
      ctx.fillRect(0, 0, widthPixels, heightPixels);
      const normalizedRotation = ((rotation % 360) + 360) % 360;
      const swapsAxes = normalizedRotation === 90 || normalizedRotation === 270;
      const orientedWidth = swapsAxes ? image.naturalHeight : image.naturalWidth;
      const orientedHeight = swapsAxes ? image.naturalWidth : image.naturalHeight;
      const scale = fit === 'cover'
        ? Math.max((widthPixels - 6) / orientedWidth, (heightPixels - 6) / orientedHeight)
        : Math.min((widthPixels - 6) / orientedWidth, (heightPixels - 6) / orientedHeight);
      const width = image.naturalWidth * scale;
      const height = image.naturalHeight * scale;
      ctx.filter = blackWhite ? 'grayscale(1)' : 'none';
      ctx.save();
      ctx.translate(widthPixels / 2, heightPixels / 2);
      ctx.rotate(normalizedRotation * Math.PI / 180);
      ctx.drawImage(image, -width / 2, -height / 2, width, height);
      ctx.restore();
      ctx.filter = 'none';
      if (border) {
        ctx.strokeStyle = '#000';
        ctx.lineWidth = Math.max(2, Math.round(300 / 2.54 * 0.05));
        ctx.strokeRect(ctx.lineWidth / 2, ctx.lineWidth / 2, widthPixels - ctx.lineWidth, heightPixels - ctx.lineWidth);
      }
      URL.revokeObjectURL(source);
      canvas.toBlob(blob => blob ? resolve(blob) : reject(Error(`No se pudo preparar ${file.name}.`)), 'image/jpeg', 0.95);
    };
    image.onerror = () => { URL.revokeObjectURL(source); reject(Error(`No se pudo abrir ${file.name}.`)); };
    image.src = source;
  });
}

function u16(value) { return new Uint8Array([value & 255, (value >>> 8) & 255]); }
function u32(value) { return new Uint8Array([value & 255, (value >>> 8) & 255, (value >>> 16) & 255, (value >>> 24) & 255]); }
function joinBytes(parts) {
  const output = new Uint8Array(parts.reduce((sum, part) => sum + part.length, 0));
  let offset = 0;
  for (const part of parts) { output.set(part, offset); offset += part.length; }
  return output;
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
    const nameBytes = new TextEncoder().encode(name), crc = crc32(data);
    const local = joinBytes([u32(0x04034b50), u16(20), u16(0x0800), u16(0), u16(0), u16(0), u32(crc), u32(data.length), u32(data.length), u16(nameBytes.length), u16(0), nameBytes]);
    localParts.push(local, data);
    centralParts.push(joinBytes([u32(0x02014b50), u16(20), u16(20), u16(0x0800), u16(0), u16(0), u16(0), u32(crc), u32(data.length), u32(data.length), u16(nameBytes.length), u16(0), u16(0), u16(0), u16(0), u32(0), u32(offset), nameBytes]));
    offset += local.length + data.length;
  }
  const central = joinBytes(centralParts);
  const end = joinBytes([u32(0x06054b50), u16(0), u16(0), u16(entries.length), u16(entries.length), u32(central.length), u32(offset), u16(0)]);
  return joinBytes([...localParts, central, end]);
}

function imageDrawing(id, widthExtent, heightExtent) {
  return `<w:p><w:pPr><w:jc w:val="center"/><w:spacing w:before="0" w:after="0"/></w:pPr><w:r><w:drawing><wp:inline distT="0" distB="0" distL="0" distR="0"><wp:extent cx="${widthExtent}" cy="${heightExtent}"/><wp:docPr id="${id}" name="Foto ${id}"/><a:graphic><a:graphicData uri="http://schemas.openxmlformats.org/drawingml/2006/picture"><pic:pic><pic:nvPicPr><pic:cNvPr id="${id}" name="Foto ${id}"/><pic:cNvPicPr/></pic:nvPicPr><pic:blipFill><a:blip r:embed="rId${id}"/><a:stretch><a:fillRect/></a:stretch></pic:blipFill><pic:spPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="${widthExtent}" cy="${heightExtent}"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom></pic:spPr></pic:pic></a:graphicData></a:graphic></wp:inline></w:drawing></w:r></w:p>`;
}

function buildTable(start, count, config) {
  const photoWidthTwips = cmToTwips(config.photoWidth), photoHeightTwips = cmToTwips(config.photoHeight), gapTwips = cmToTwips(config.gap), columns = config.columns;
  const grid = [];
  for (let col = 0; col < columns; col++) {
    grid.push(`<w:gridCol w:w="${photoWidthTwips}"/>`);
    if (col < columns - 1) grid.push(`<w:gridCol w:w="${gapTwips}"/>`);
  }
  let rowsXml = '';
  const rowCount = Math.ceil(count / columns);
  for (let row = 0; row < rowCount; row++) {
    let cells = '';
    for (let col = 0; col < columns; col++) {
      if (col) cells += `<w:tc><w:tcPr><w:tcW w:w="${gapTwips}" w:type="dxa"/><w:tcBorders><w:top w:val="nil"/><w:left w:val="nil"/><w:bottom w:val="nil"/><w:right w:val="nil"/></w:tcBorders></w:tcPr><w:p/></w:tc>`;
      const photoIndex = row * columns + col;
      const hasPhoto = photoIndex < count;
      const cellBorders = '<w:tcBorders><w:top w:val="nil"/><w:left w:val="nil"/><w:bottom w:val="nil"/><w:right w:val="nil"/></w:tcBorders>';
      const content = hasPhoto ? imageDrawing(start + photoIndex + 1, cmToEmu(config.photoWidth), cmToEmu(config.photoHeight)) : '<w:p/>';
      cells += `<w:tc><w:tcPr><w:tcW w:w="${photoWidthTwips}" w:type="dxa"/>${cellBorders}<w:vAlign w:val="center"/></w:tcPr>${content}</w:tc>`;
    }
    rowsXml += `<w:tr><w:trPr><w:trHeight w:val="${photoHeightTwips}" w:hRule="exact"/></w:trPr>${cells}</w:tr>`;
  }
  const tableTwips = columns * photoWidthTwips + (columns - 1) * gapTwips;
  return `<w:tbl><w:tblPr><w:tblW w:w="${tableTwips}" w:type="dxa"/><w:jc w:val="center"/><w:tblLayout w:type="fixed"/><w:tblBorders><w:top w:val="nil"/><w:left w:val="nil"/><w:bottom w:val="nil"/><w:right w:val="nil"/><w:insideH w:val="nil"/><w:insideV w:val="nil"/></w:tblBorders><w:tblCellMar><w:top w:w="0" w:type="dxa"/><w:left w:w="0" w:type="dxa"/><w:bottom w:w="0" w:type="dxa"/><w:right w:w="0" w:type="dxa"/></w:tblCellMar></w:tblPr><w:tblGrid>${grid.join('')}</w:tblGrid>${rowsXml}</w:tbl>`;
}

async function exportWord() {
  const config = settings();
  const processed = [];
  const blackWhite = colorModeButton.getAttribute('aria-pressed') === 'true';
  for (let index = 0; index < photos.length; index++) {
    photoStatus.textContent = `Preparando foto ${index + 1} de ${photos.length} para Word…`;
    processed.push(await preparePhoto(photos[index], config.photoWidth, config.photoHeight, fitInput.value, blackWhite, photoBorderInput.checked, photoRotations.get(photos[index]) || 0));
  }
  const relationships = processed.map((_, index) => `<Relationship Id="rId${index + 1}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/image" Target="media/image${index + 1}.jpg"/>`).join('');
  const body = [];
  for (let start = 0; start < processed.length; start += config.perPage) {
    if (start) body.push('<w:p><w:r><w:br w:type="page"/></w:r></w:p>');
    body.push(buildTable(start, Math.min(config.perPage, processed.length - start), config));
  }
  const paperW = cmToTwips(config.paperWidth), paperH = cmToTwips(config.paperHeight), margin = cmToTwips(config.margin);
  const pageOrientation = config.paperWidth > config.paperHeight ? ' w:orient="landscape"' : '';
  const documentXml = `<?xml version="1.0" encoding="UTF-8" standalone="yes"?><w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:wp="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing" xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:pic="http://schemas.openxmlformats.org/drawingml/2006/picture"><w:body>${body.join('')}<w:sectPr><w:pgSz w:w="${paperW}" w:h="${paperH}"${pageOrientation}/><w:pgMar w:top="${margin}" w:right="${margin}" w:bottom="${margin}" w:left="${margin}" w:header="0" w:footer="0" w:gutter="0"/></w:sectPr></w:body></w:document>`;
  const contentTypes = `<?xml version="1.0" encoding="UTF-8"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Default Extension="jpg" ContentType="image/jpeg"/><Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/></Types>`;
  const rootRels = `<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/></Relationships>`;
  const entries = [
    ['[Content_Types].xml', new TextEncoder().encode(contentTypes)],
    ['_rels/.rels', new TextEncoder().encode(rootRels)],
    ['word/document.xml', new TextEncoder().encode(documentXml)],
    ['word/_rels/document.xml.rels', new TextEncoder().encode(`<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">${relationships}</Relationships>`)]
  ];
  for (let i = 0; i < processed.length; i++) entries.push([`word/media/image${i + 1}.jpg`, new Uint8Array(await processed[i].arrayBuffer())]);
  const blob = new Blob([makeZip(entries)], {type: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document'});
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = 'fotos-a-escala.docx';
  document.body.append(link);
  link.click();
  link.remove();
  setTimeout(() => URL.revokeObjectURL(url), 5000);
  photoStatus.textContent = 'Documento Word generado.';
  photoStatus.className = 'photo-count';
}

wordButton.addEventListener('click', async () => {
  wordButton.disabled = true;
  wordButton.textContent = 'Preparando Word…';
  photoStatus.className = 'photo-count';
  try {
    await exportWord();
  } catch (error) {
    photoStatus.textContent = `No se pudo generar Word: ${error.message}`;
    photoStatus.className = 'tip';
    alert(error.message);
  } finally {
    wordButton.textContent = 'Descargar Word';
    renderSheets();
  }
});
syncDimensionControls();
renderSheets();
window.LocalFileTransfer.take().then(async incoming => {
  addPhotos(incoming.filter(file => file.type.startsWith('image/')));
  for (const archive of incoming.filter(file => /\.(zip|rar)$/i.test(file.name))) await addArchive(archive);
}).catch(error => {
  photoStatus.textContent = error.message;
  photoStatus.className = 'tip';
});
