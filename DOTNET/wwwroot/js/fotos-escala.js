const photoEditorTitle = document.getElementById('photoEditorTitle');
const photoToolbar = document.getElementById('photoToolbar');
const photoToolbarEmpty = document.getElementById('photoToolbarEmpty');
const photoToolbarBody = document.getElementById('photoToolbarBody');
const editorPhotoOrient = document.getElementById('editorPhotoOrient');
const cropOverlay = document.getElementById('cropOverlay');
const cropStageHost = document.getElementById('cropStageHost');
const cropCancel = document.getElementById('cropCancel');
const cropAccept = document.getElementById('cropAccept');
const photoToast = document.getElementById('photoToast');
const photoEditorStage = document.getElementById('photoEditorStage');
const photoEditorImageViewport = document.getElementById('photoEditorImageViewport');
const photoEditorImage = document.getElementById('photoEditorImage');
const cropSelection = document.getElementById('cropSelection');
const editorPhotoWidth = document.getElementById('editorPhotoWidth');
const editorPhotoHeight = document.getElementById('editorPhotoHeight');
const editorPhotoScale = document.getElementById('editorPhotoScale');
const editorPhotoScaleValue = document.getElementById('editorPhotoScaleValue');
const editorPhotoFit = document.getElementById('editorPhotoFit');
const editorPhotoAlign = document.getElementById('editorPhotoAlign');
const editorPhotoRotate = document.getElementById('editorPhotoRotate');
const editorCropMode = document.getElementById('editorCropMode');
const editorPhotoFullPage = document.getElementById('editorPhotoFullPage');
const editorPhotoReset = document.getElementById('editorPhotoReset');
const editorPhotoFilter = document.getElementById('editorPhotoFilter');
const photoEditorClose = document.getElementById('photoEditorClose');
const photoEditorRemove = document.getElementById('photoEditorRemove');
const photoStatus = document.getElementById('photoStatus');
const sheets = document.getElementById('sheets');
const sheetZoomStage = document.getElementById('sheetZoomStage');
const sheetZoomRange = document.getElementById('sheetZoomRange');
const sheetZoomValue = document.getElementById('sheetZoomValue');
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
const squareModeField = squareModeInput.closest('.square-option');
const squareModeFieldHome = squareModeField.parentElement;
const squareModeFieldNextSibling = squareModeField.nextSibling;
const outputControls = document.querySelector('.output-controls');
const outputControlsHome = outputControls.parentElement;
const outputControlsNextSibling = outputControls.nextSibling;

let photos = [];
let selectedPhoto = null;
let sheetZoom = 100;
let cropBoxDrag = null;
let cropBox = {x: 0, y: 0, width: 1, height: 1};
let tileDrag = null;
let resizeDrag = null;
let cropEditMode = false;
let lastCropTap = null;
const manualPreviewCache = new WeakMap();
let pageFitPercent = 100;
let workStarted = false;
let zipLoadingCount = 0;
const objectUrls = new WeakMap();
const photoRotations = new WeakMap();
const photoOriginals = new WeakMap();
const photoOriginalRotations = new WeakMap();
const photoOverrides = new Map();
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
  const dropped = [...(event.dataTransfer?.files || [])];
  // Solo se aceptan archivos reales. Arrastrar una foto ya cargada en la hoja
  // no debe volver a incorporarla a la lista.
  if (!dropped.length) return;
  event.preventDefault();
  dragDepth = 0;
  document.body.classList.remove('page-drop-active');
  addPhotos(dropped);
  for (const file of dropped.filter(item => /\.(zip|rar)$/i.test(item.name))) await addArchive(file);
});
document.addEventListener('dragstart', event => {
  if (event.target.closest?.('#photoGrid img, #sheets img')) event.preventDefault();
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

function optionsFor(file, base = settings()) {
  const override = photoOverrides.get(file);
  return override ? {...base, alignment: 'auto', photoFilter: 'none', ...override, pageFit: false} : {...base, fit: fitInput.value, cropX: 50, cropY: 50, cropZoom: 1, fullPage: false, alignment: 'auto', photoFilter: 'none'};
}

function photoFilterCss(filter) {
  return ({
    contrast: 'contrast(1.25) saturate(1.08)',
    bright: 'brightness(1.18) contrast(1.04)',
    soft: 'brightness(1.06) saturate(.82)',
    grayscale: 'grayscale(1)',
    sepia: 'sepia(.72) contrast(1.08)',
    document: 'grayscale(1) contrast(1.65) brightness(1.12)'
  })[filter] || '';
}

function filterForPrint(filter) {
  // Mantén los filtros CSS de la vista previa también al rasterizar para imprimir.
  return photoFilterCss(filter) || 'none';
}

function inputNumber(labelText, value, min, max, step, handler) {
  const label = document.createElement('label');
  label.textContent = labelText;
  const input = document.createElement('input');
  input.type = 'number'; input.min = String(min); input.max = String(max); input.step = String(step); input.value = Number(value).toFixed(1);
  input.addEventListener('change', () => {
    const parsed = Math.max(min, Math.min(max, Number(input.value) || min));
    input.value = parsed.toFixed(1);
    handler(parsed);
  });
  label.append(input);
  return label;
}

function updatePhotoOverride(file, changes) {
  savePhotoOverride(file, changes);
  workStarted = true;
  render();
}

function savePhotoOverride(file, changes) {
  const current = optionsFor(file);
  photoOverrides.set(file, {
    photoWidth: changes.photoWidth ?? current.photoWidth,
    photoHeight: changes.photoHeight ?? current.photoHeight,
    fit: changes.fit ?? current.fit ?? fitInput.value,
    cropX: changes.cropX ?? current.cropX ?? 50,
    cropY: changes.cropY ?? current.cropY ?? 50,
    cropZoom: changes.cropZoom ?? current.cropZoom ?? 1,
    fullPage: changes.fullPage ?? current.fullPage ?? false,
    photoFilter: changes.photoFilter ?? current.photoFilter ?? 'none',
    alignment: Object.hasOwn(changes, 'alignment') ? changes.alignment : current.alignment ?? 'auto',
    x: Object.hasOwn(changes, 'x') ? changes.x : current.x,
    y: Object.hasOwn(changes, 'y') ? changes.y : current.y
  });
  if (pageFitInput.checked) {
    pageFitInput.checked = false;
    syncDimensionControls();
  }
}

function applyImageCropPresentation(image, file, frameWidth, frameHeight, rotation = 0) {
  if (!image || !frameWidth || !frameHeight) return;
  image.dataset.cropFrameWidth = String(frameWidth);
  image.dataset.cropFrameHeight = String(frameHeight);
  image.dataset.cropRotation = String(rotation);
  if (!image.complete || !image.naturalWidth || !image.naturalHeight) {
    if (image.dataset.cropLoadPending !== 'true') {
      image.dataset.cropLoadPending = 'true';
      image.addEventListener('load', () => {
        image.dataset.cropLoadPending = 'false';
        applyImageCropPresentation(image, file, Number(image.dataset.cropFrameWidth), Number(image.dataset.cropFrameHeight), Number(image.dataset.cropRotation) || 0);
      }, {once: true});
    }
    return;
  }
  const config = optionsFor(file);
  const zoom = Math.max(1, Number(config.cropZoom) || 1);
  const cropX = Math.max(0, Math.min(100, Number(config.cropX) || 0));
  const cropY = Math.max(0, Math.min(100, Number(config.cropY) || 0));
  const swapsAxes = Math.abs(rotation % 180) === 90;
  const orientedWidth = swapsAxes ? image.naturalHeight : image.naturalWidth;
  const orientedHeight = swapsAxes ? image.naturalWidth : image.naturalHeight;
  const baseScale = (config.fit || fitInput.value) === 'cover'
    ? Math.max(frameWidth / orientedWidth, frameHeight / orientedHeight)
    : Math.min(frameWidth / orientedWidth, frameHeight / orientedHeight);
  const scale = baseScale * zoom;
  const scaledWidth = image.naturalWidth * scale;
  const scaledHeight = image.naturalHeight * scale;
  const overflowX = Math.max(0, orientedWidth * scale - frameWidth);
  const overflowY = Math.max(0, orientedHeight * scale - frameHeight);
  const translateX = (0.5 - cropX / 100) * overflowX;
  const translateY = (0.5 - cropY / 100) * overflowY;
  image.style.position = 'absolute';
  image.style.left = '50%';
  image.style.top = '50%';
  image.style.width = `${scaledWidth}px`;
  image.style.height = `${scaledHeight}px`;
  image.style.maxWidth = 'none';
  image.style.maxHeight = 'none';
  image.style.objectFit = 'fill';
  image.style.objectPosition = '50% 50%';
  image.style.transformOrigin = '50% 50%';
  image.style.transform = `translate(-50%, -50%) translate(${translateX}px, ${translateY}px) rotate(${rotation}deg)`;
}

function applyManualSourcePresentation(image, frameWidth, frameHeight, rotation = 0) {
  if (!image?.naturalWidth || !image?.naturalHeight || !frameWidth || !frameHeight) return;
  const swapsAxes = Math.abs(rotation % 180) === 90;
  const orientedWidth = swapsAxes ? image.naturalHeight : image.naturalWidth;
  const orientedHeight = swapsAxes ? image.naturalWidth : image.naturalHeight;
  const scale = Math.min(frameWidth / orientedWidth, frameHeight / orientedHeight);
  image.style.position = 'absolute';
  image.style.left = '50%';
  image.style.top = '50%';
  image.style.width = `${image.naturalWidth * scale}px`;
  image.style.height = `${image.naturalHeight * scale}px`;
  image.style.maxWidth = 'none';
  image.style.maxHeight = 'none';
  image.style.objectFit = 'fill';
  image.style.transformOrigin = '50% 50%';
  image.style.transform = `translate(-50%, -50%) rotate(${rotation}deg)`;
}

function orientedManualPreview(file, rotation) {
  let previews = manualPreviewCache.get(file);
  if (!previews) { previews = new Map(); manualPreviewCache.set(file, previews); }
  if (previews.has(rotation)) return previews.get(rotation);
  const preview = new Promise((resolve, reject) => {
    const source = new Image();
    source.onload = () => {
      const normalized = ((rotation % 360) + 360) % 360;
      const swapsAxes = normalized === 90 || normalized === 270;
      const width = swapsAxes ? source.naturalHeight : source.naturalWidth;
      const height = swapsAxes ? source.naturalWidth : source.naturalHeight;
      const canvas = document.createElement('canvas');
      canvas.width = width;
      canvas.height = height;
      const context = canvas.getContext('2d');
      context.translate(width / 2, height / 2);
      context.rotate(normalized * Math.PI / 180);
      context.drawImage(source, -source.naturalWidth / 2, -source.naturalHeight / 2);
      resolve(canvas.toDataURL('image/png'));
    };
    source.onerror = () => reject(new Error('No se pudo preparar la orientación de la foto.'));
    source.src = objectUrl(file);
  });
  previews.set(rotation, preview);
  return preview;
}

function renderCropBox() {
  cropSelection.style.left = `${cropBox.x * 100}%`;
  cropSelection.style.top = `${cropBox.y * 100}%`;
  cropSelection.style.width = `${cropBox.width * 100}%`;
  cropSelection.style.height = `${cropBox.height * 100}%`;
}

function cropPointFromPointer(event) {
  // Quita el borde del escenario para alinear el área visible con los píxeles recortados.
  const bounds = photoEditorStage.getBoundingClientRect();
  const width = Math.max(1, photoEditorStage.clientWidth);
  const height = Math.max(1, photoEditorStage.clientHeight);
  return {
    x: Math.max(0, Math.min(1, (event.clientX - bounds.left - photoEditorStage.clientLeft) / width)),
    y: Math.max(0, Math.min(1, (event.clientY - bounds.top - photoEditorStage.clientTop) / height))
  };
}

let cropApplying = false;

function showToast(message) {
  if (!photoToast) return;
  photoToast.textContent = message;
  photoToast.hidden = false;
  clearTimeout(showToast.timer);
  showToast.timer = setTimeout(() => { photoToast.hidden = true; }, 4200);
}

function selectPhoto(file) {
  if (!photos.includes(file)) return;
  if (selectedPhoto === file) {
    renderPhotoEditor();
    return;
  }
  cropEditMode = false;
  cropBox = {x: 0, y: 0, width: 1, height: 1};
  selectedPhoto = file;
  render();
}

function deselectPhoto() {
  selectedPhoto = null;
  cropEditMode = false;
  cropBoxDrag = null;
  cropBox = {x: 0, y: 0, width: 1, height: 1};
  render();
}

function removeSelectedPhoto() {
  if (!selectedPhoto) return;
  const removed = selectedPhoto;
  photos = photos.filter(file => file !== removed);
  photoOverrides.delete(removed);
  selectedPhoto = null;
  cropEditMode = false;
  cropBoxDrag = null;
  workStarted = true;
  render();
}

function isCropDoubleTap(event, file) {
  const now = Date.now();
  const isSecondTap = lastCropTap?.file === file
    && now - lastCropTap.time < 450
    && Math.hypot(event.clientX - lastCropTap.x, event.clientY - lastCropTap.y) < 28;
  lastCropTap = isSecondTap ? null : {file, time: now, x: event.clientX, y: event.clientY};
  return isSecondTap;
}

function cropKeyFor(file) {
  return `${objectUrl(file)}|${photoRotations.get(file) || 0}|manual`;
}

function cropPreviewReady(file) {
  return photoEditorImage.dataset.renderKey === cropKeyFor(file) && photoEditorImage.complete && photoEditorImage.naturalWidth > 0;
}

function cropBoxHasArea() {
  return cropBox.width > 0.004 && cropBox.height > 0.004;
}

// Barra de edición: siempre visible junto a la hoja. Solo cambia según haya una foto seleccionada.
function renderPhotoEditor() {
  const hasSelection = Boolean(selectedPhoto && photos.includes(selectedPhoto));
  photoToolbarEmpty.hidden = hasSelection;
  photoToolbar.hidden = !hasSelection;
  photoToolbarBody.hidden = !hasSelection;
  photoToolbar.classList.toggle('has-selection', hasSelection);
  document.getElementById('photoPanel').classList.toggle('editing-photo', hasSelection);
  if (!hasSelection) {
    cropEditMode = false;
    cropBoxDrag = null;
    cropOverlay.hidden = true;
    document.body.classList.remove('crop-open');
    return;
  }
  const file = selectedPhoto;
  const config = optionsFor(file);
  const paper = settings();
  photoEditorTitle.textContent = file.name;
  photoEditorTitle.title = file.name;
  editorPhotoWidth.value = Number(config.fullPage ? paper.paperWidth : config.photoWidth).toFixed(1);
  editorPhotoHeight.value = Number(config.fullPage ? paper.paperHeight : config.photoHeight).toFixed(1);
  if (editorPhotoScale.dataset.fileKey !== objectUrl(file)) {
    editorPhotoScale.dataset.fileKey = objectUrl(file);
    editorPhotoScale.dataset.baseWidth = String(Number(config.photoWidth) || 5);
    editorPhotoScale.dataset.baseHeight = String(Number(config.photoHeight) || 5);
    editorPhotoScale.value = '100';
    editorPhotoScaleValue.textContent = '100%';
  }
  editorPhotoFit.value = config.fit || fitInput.value;
  editorPhotoFilter.value = config.photoFilter || 'none';
  editorPhotoAlign.value = photoAlignment(file, config);
  editorPhotoAlign.disabled = Boolean(config.fullPage);
  editorPhotoFullPage.textContent = config.fullPage ? 'Volver a tamaño normal' : 'Toda la hoja';
  editorPhotoFullPage.setAttribute('aria-pressed', String(Boolean(config.fullPage)));
  editorPhotoFullPage.title = config.fullPage ? 'La foto ocupa toda la hoja, sin márgenes' : 'Hace que la foto ocupe toda la hoja, sin márgenes';
  const frameIsTall = Number(config.photoHeight) > Number(config.photoWidth);
  editorPhotoOrient.textContent = frameIsTall ? 'Marco horizontal' : 'Marco vertical';
  editorPhotoOrient.disabled = Boolean(config.fullPage);
  editorPhotoReset.hidden = !photoOriginals.has(file);
  editorCropMode.setAttribute('aria-pressed', String(cropEditMode));
  cropOverlay.hidden = !cropEditMode;
  document.body.classList.toggle('crop-open', cropEditMode);
  if (cropEditMode) renderCropStage(file);
}

// Ventana de recorte: la foto completa en grande; se arrastra para marcar el área y se aplica al soltar.
function renderCropStage(file) {
  const rotation = photoRotations.get(file) || 0;
  const renderKey = cropKeyFor(file);
  if (photoEditorImage.dataset.renderKey !== renderKey && photoEditorImage.dataset.pendingKey !== renderKey) {
    photoEditorImage.onload = () => { if (selectedPhoto === file && cropEditMode) renderPhotoEditor(); };
    if (rotation) {
      photoEditorImage.dataset.pendingKey = renderKey;
      photoEditorImage.style.visibility = 'hidden';
      orientedManualPreview(file, rotation).then(src => {
        if (selectedPhoto !== file || !cropEditMode || (photoRotations.get(file) || 0) !== rotation) return;
        photoEditorImage.dataset.renderKey = renderKey;
        photoEditorImage.dataset.pendingKey = '';
        photoEditorImage.src = src;
        photoEditorImage.style.visibility = 'visible';
      }).catch(error => {
        photoEditorImage.dataset.pendingKey = '';
        showToast(error.message);
      });
    } else {
      photoEditorImage.dataset.renderKey = renderKey;
      photoEditorImage.dataset.pendingKey = '';
      photoEditorImage.src = objectUrl(file);
      photoEditorImage.style.visibility = 'visible';
    }
  }
  const ready = cropPreviewReady(file);
  const sourceWidth = Math.max(1, photoEditorImage.naturalWidth || 1);
  const sourceHeight = Math.max(1, photoEditorImage.naturalHeight || 1);
  const swapsAxes = !ready && Math.abs(rotation % 180) === 90;
  const width = swapsAxes ? sourceHeight : sourceWidth;
  const height = swapsAxes ? sourceWidth : sourceHeight;
  const maxWidth = Math.max(160, Math.min(980, cropStageHost.clientWidth - 24));
  const availableStageHeight = cropStageHost.clientHeight > 0 ? cropStageHost.clientHeight - 24 : window.innerHeight * 0.62;
  const maxHeight = Math.max(120, Math.min(window.innerHeight * 0.62, availableStageHeight, 760));
  const stageScale = Math.min(maxWidth / width, maxHeight / height);
  photoEditorStage.style.aspectRatio = `${width} / ${height}`;
  photoEditorStage.style.width = `${width * stageScale}px`;
  photoEditorStage.style.height = `${height * stageScale}px`;
  applyManualSourcePresentation(photoEditorImage, photoEditorImageViewport.clientWidth, photoEditorImageViewport.clientHeight, ready ? 0 : rotation);
  photoEditorStage.classList.toggle('manual-preview-loading', !ready);
  cropSelection.hidden = !ready;
  if (!cropSelection.hidden) renderCropBox();
}

function closeCrop() {
  if (!cropEditMode) return;
  cropEditMode = false;
  cropBoxDrag = null;
  cropBox = {x: 0, y: 0, width: 1, height: 1};
  renderSheets();
  renderPhotoEditor();
  editorCropMode.focus({preventScroll: true});
}

function toggleCropMode(file = selectedPhoto) {
  if (!file) return;
  selectedPhoto = file;
  if (cropEditMode) { closeCrop(); return; }
  cropEditMode = true;
  cropBox = {x: 0, y: 0, width: 1, height: 1};
  cropBoxDrag = null;
  renderSheets();
  renderPhotoEditor();
  cropCancel.focus({preventScroll: true});
}

function updateSelectedPhotoDimensions() {
  if (!selectedPhoto) return;
  const width = Math.max(1, Math.min(60, Number(editorPhotoWidth.value) || 1));
  const height = Math.max(1, Math.min(60, Number(editorPhotoHeight.value) || 1));
  editorPhotoWidth.value = width.toFixed(1);
  editorPhotoHeight.value = height.toFixed(1);
  updatePhotoOverride(selectedPhoto, {photoWidth: width, photoHeight: height, fullPage: false});
  editorPhotoScale.value = '100';
  editorPhotoScaleValue.textContent = '100%';
  editorPhotoScale.dataset.baseWidth = String(width);
  editorPhotoScale.dataset.baseHeight = String(height);
}

function scaleSelectedPhoto() {
  if (!selectedPhoto) return;
  const factor = Number(editorPhotoScale.value) / 100;
  const width = Math.max(1, Math.min(60, Number(editorPhotoScale.dataset.baseWidth) * factor));
  const height = Math.max(1, Math.min(60, Number(editorPhotoScale.dataset.baseHeight) * factor));
  editorPhotoScaleValue.textContent = `${Math.round(factor * 100)}%`;
  editorPhotoWidth.value = width.toFixed(1);
  editorPhotoHeight.value = height.toFixed(1);
  updatePhotoOverride(selectedPhoto, {photoWidth: width, photoHeight: height, fullPage: false});
}

function photoAlignment(file, config = optionsFor(file)) {
  return ['left', 'center', 'right'].includes(config.alignment) ? config.alignment : 'left';
}

function setSelectedPhotoAlignment() {
  if (!selectedPhoto) return;
  updatePhotoOverride(selectedPhoto, {alignment: editorPhotoAlign.value, x: undefined, y: undefined, fullPage: false});
}

function rotateSelectedPhoto() {
  if (!selectedPhoto || cropEditMode) return;
  photoRotations.set(selectedPhoto, ((photoRotations.get(selectedPhoto) || 0) + 90) % 360);
  workStarted = true;
  render();
}

for (const input of [editorPhotoWidth, editorPhotoHeight]) input.addEventListener('change', updateSelectedPhotoDimensions);
editorPhotoScale.addEventListener('input', scaleSelectedPhoto);
editorPhotoFit.addEventListener('change', () => {
  if (!selectedPhoto) return;
  const resetCrop = editorPhotoFit.value === 'contain' ? {cropZoom: 1, cropX: 50, cropY: 50} : {};
  updatePhotoOverride(selectedPhoto, {fit: editorPhotoFit.value, ...resetCrop});
});
editorPhotoAlign.addEventListener('change', setSelectedPhotoAlignment);
editorPhotoRotate.addEventListener('click', rotateSelectedPhoto);
editorPhotoOrient.addEventListener('click', () => {
  if (!selectedPhoto) return;
  const current = optionsFor(selectedPhoto);
  updatePhotoOverride(selectedPhoto, {photoWidth: current.photoHeight, photoHeight: current.photoWidth, fullPage: false});
});
editorPhotoFilter.addEventListener('change', () => {
  if (selectedPhoto) updatePhotoOverride(selectedPhoto, {photoFilter: editorPhotoFilter.value});
});
editorCropMode.addEventListener('click', () => toggleCropMode());
editorPhotoFullPage.addEventListener('click', () => {
  if (!selectedPhoto) return;
  const current = optionsFor(selectedPhoto);
  updatePhotoOverride(selectedPhoto, {fullPage: !current.fullPage});
});
editorPhotoReset.addEventListener('click', () => {
  if (selectedPhoto && photoOriginals.has(selectedPhoto)) restoreOriginalPhoto();
});
photoEditorClose.addEventListener('click', deselectPhoto);
photoEditorRemove.addEventListener('click', removeSelectedPhoto);
cropCancel.addEventListener('click', closeCrop);
cropAccept.addEventListener('click', () => {
  if (!cropBoxHasArea()) { showToast('Ajustá el área que querés conservar antes de aceptar.'); return; }
  cropApplying = true;
  cropAccept.disabled = true;
  applyManualCrop().catch(error => showToast(error.message || 'No se pudo aplicar el recorte.'))
    .finally(() => { cropApplying = false; cropAccept.disabled = false; });
});
cropOverlay.addEventListener('pointerdown', event => { if (event.target === cropOverlay) closeCrop(); });

photoEditorStage.addEventListener('pointerdown', event => {
  if (!cropEditMode || !selectedPhoto || cropApplying || !cropPreviewReady(selectedPhoto)) return;
  if (event.button !== undefined && event.button !== 0) return;
  const {x, y} = cropPointFromPointer(event);
  const handle = event.target.closest?.('.crop-handle');
  if (handle) {
    cropBoxDrag = {pointerId: event.pointerId, mode: 'resize', handle: handle.dataset.handle, x, y, box: {...cropBox}};
  } else if (event.target === cropSelection) {
    cropBoxDrag = {pointerId: event.pointerId, mode: 'move', x, y, box: {...cropBox}};
  } else {
    cropBox = {x, y, width: 0, height: 0};
    cropBoxDrag = {pointerId: event.pointerId, mode: 'draw', x, y, box: {...cropBox}};
  }
  cropSelection.hidden = false;
  renderCropBox();
  photoEditorStage.setPointerCapture(event.pointerId);
  event.preventDefault();
});

photoEditorStage.addEventListener('pointermove', event => {
  if (!cropBoxDrag || cropBoxDrag.pointerId !== event.pointerId) return;
  const {x, y} = cropPointFromPointer(event);
  const drag = cropBoxDrag;
  if (drag.mode === 'draw') {
    cropBox = {x: Math.min(drag.x, x), y: Math.min(drag.y, y), width: Math.abs(x - drag.x), height: Math.abs(y - drag.y)};
  } else if (drag.mode === 'move') {
    cropBox = {...drag.box, x: Math.max(0, Math.min(1 - drag.box.width, drag.box.x + x - drag.x)), y: Math.max(0, Math.min(1 - drag.box.height, drag.box.y + y - drag.y))};
  } else {
    const minSize = 0.01, b = drag.box, handle = drag.handle;
    let left = b.x, top = b.y, right = b.x + b.width, bottom = b.y + b.height;
    if (handle.includes('w')) left = Math.max(0, Math.min(right - minSize, x));
    if (handle.includes('e')) right = Math.min(1, Math.max(left + minSize, x));
    if (handle.includes('n')) top = Math.max(0, Math.min(bottom - minSize, y));
    if (handle.includes('s')) bottom = Math.min(1, Math.max(top + minSize, y));
    cropBox = {x:left, y:top, width:right-left, height:bottom-top};
  }
  renderCropBox();
});

// Al soltar solo conserva la selección. El usuario la confirma con «Aceptar recorte».
function finishCropBoxDrag(event) {
  if (!cropBoxDrag || cropBoxDrag.pointerId !== event.pointerId) return;
  cropBoxDrag = null;
  if (event.type === 'pointercancel' || cropBox.width < 0.02 || cropBox.height < 0.02) cropBox = {x:0,y:0,width:1,height:1};
  renderCropBox();
}
photoEditorStage.addEventListener('pointerup', finishCropBoxDrag);
photoEditorStage.addEventListener('pointercancel', finishCropBoxDrag);

// Tocar el fondo gris de la hoja deja de editar la foto.
document.addEventListener('pointerdown', event => {
  if (!selectedPhoto || cropEditMode) return;
  if (event.target.closest('.photo-tile, #photoToolbar, #photoPanel, #scalePreviewBar, #cropOverlay')) return;
  if (!event.target.closest('#sheetsScroll, .print-sheet, #sheets')) return;
  deselectPhoto();
});

document.addEventListener('keydown', event => {
  if (event.key !== 'Delete' || !selectedPhoto || cropEditMode) return;
  if (event.target.closest?.('input, select, textarea')) return;
  event.preventDefault();
  removeSelectedPhoto();
});

async function applyManualCrop() {
  if (!selectedPhoto || !cropEditMode || !photoEditorImage.naturalWidth) return;
  if (cropBox.width < 0.02 || cropBox.height < 0.02) {
    throw new Error('Marca un área más grande para aplicar el recorte.');
  }
  const sourceFile = selectedPhoto;
  const sourceRotation = ((photoRotations.get(sourceFile) || 0) % 360 + 360) % 360;
  // En modo manual la vista ya está orientada; no se vuelve a girar al aplicar el recorte.
  const orientedWidth = photoEditorImage.naturalWidth;
  const orientedHeight = photoEditorImage.naturalHeight;
  const left = Math.max(0, Math.min(orientedWidth - 1, Math.round(cropBox.x * orientedWidth)));
  const top = Math.max(0, Math.min(orientedHeight - 1, Math.round(cropBox.y * orientedHeight)));
  const cropWidth = Math.max(1, Math.min(orientedWidth - left, Math.round(cropBox.width * orientedWidth)));
  const cropHeight = Math.max(1, Math.min(orientedHeight - top, Math.round(cropBox.height * orientedHeight)));
  const oriented = document.createElement('canvas');
  oriented.width = orientedWidth;
  oriented.height = orientedHeight;
  const orientedContext = oriented.getContext('2d');
  orientedContext.drawImage(photoEditorImage, 0, 0);
  const cropped = document.createElement('canvas');
  cropped.width = cropWidth;
  cropped.height = cropHeight;
  cropped.getContext('2d').drawImage(oriented, left, top, cropWidth, cropHeight, 0, 0, cropWidth, cropHeight);
  const blob = await new Promise(resolve => cropped.toBlob(resolve, 'image/png'));
  if (!blob) throw new Error('No se pudo preparar el recorte.');
  const baseName = sourceFile.name.replace(/(?:_recortada)?\.[^.]+$/i, '');
  const croppedFile = new File([blob], `${baseName}_recortada.png`, {type: 'image/png', lastModified: Date.now()});
  const current = optionsFor(sourceFile);
  const rootOriginal = photoOriginals.get(sourceFile) || sourceFile;
  const rootRotation = photoOriginalRotations.get(sourceFile) ?? sourceRotation;
  photoOriginals.set(croppedFile, rootOriginal);
  photoOriginalRotations.set(croppedFile, rootRotation);
  photoRotations.set(croppedFile, 0);
  photoOverrides.set(croppedFile, {
    photoWidth: current.photoWidth,
    photoHeight: current.photoHeight,
    fit: current.fit,
    cropX: 50,
    cropY: 50,
    cropZoom: 1,
    alignment: current.alignment || 'auto',
    fullPage: current.fullPage,
    photoFilter: current.photoFilter || 'none',
    x: current.x,
    y: current.y
  });
  photoOverrides.delete(sourceFile);
  const photoIndex = photos.indexOf(sourceFile);
  if (photoIndex >= 0) photos[photoIndex] = croppedFile;
  selectedPhoto = croppedFile;
  cropEditMode = false;
  cropBox = {x: 0, y: 0, width: 1, height: 1};
  workStarted = true;
  render();
  renderSheets();
  showToast('Recorte aplicado. Si no te gusta, usá “Restaurar original”.');
}

function restoreOriginalPhoto() {
  if (!selectedPhoto || !photoOriginals.has(selectedPhoto)) return;
  const editedFile = selectedPhoto;
  const originalFile = photoOriginals.get(editedFile);
  const current = optionsFor(editedFile);
  const originalRotation = photoOriginalRotations.get(editedFile) || 0;
  const index = photos.indexOf(editedFile);
  if (index >= 0) photos[index] = originalFile;
  photoRotations.set(originalFile, originalRotation);
  photoOverrides.set(originalFile, {
    photoWidth: current.photoWidth,
    photoHeight: current.photoHeight,
    fit: current.fit,
    cropX: 50,
    cropY: 50,
    cropZoom: 1,
    alignment: current.alignment || 'auto',
    fullPage: current.fullPage,
    photoFilter: current.photoFilter || 'none',
    x: current.x,
    y: current.y
  });
  photoOverrides.delete(editedFile);
  selectedPhoto = originalFile;
  workStarted = true;
  render();
  renderSheets();
  showToast('Se restauró la foto original.');
}

function render() {
  renderPhotoEditor();
  photoStatus.textContent = photos.length
    ? `${photos.length} imagen${photos.length === 1 ? '' : 'es'} cargada${photos.length === 1 ? '' : 's'}.`
    : 'Arrastrá imágenes o un archivo ZIP/RAR a cualquier parte de la página.';
  photoStatus.className = photos.length ? 'photo-count' : 'empty';
  printButton.disabled = wordButton.disabled = photos.length === 0;
  enterScalePreviewButton.disabled = photos.length === 0;
  renderSheets();
}

function renderSheets() {
  // El tamaño inline solo sirve para calcular el área desplazable del zoom;
  // se limpia antes de calcular el ajuste responsivo de la hoja.
  sheets.style.width = '';
  sheets.style.transform = 'none';
  sheetZoomStage.style.width = '';
  sheetZoomStage.style.height = '';
  sheets.replaceChildren();
  if (!photos.length) {
    layoutInfo.textContent = 'La hoja y las fotos aparecerán aquí.';
    layoutInfo.className = 'photo-layout-info muted';
    layoutInfo.hidden = false;
    requestAnimationFrame(applySheetZoom);
    return;
  }
  const config = settings();
  const layout = makePhotoPlacements(config);
  if (layout.error) {
    layoutInfo.textContent = layout.error;
    layoutInfo.className = 'photo-layout-info tip';
    layoutInfo.hidden = false;
    printButton.disabled = wordButton.disabled = true;
    enterScalePreviewButton.disabled = true;
    requestAnimationFrame(applySheetZoom);
    return;
  }
  printButton.disabled = wordButton.disabled = false;
  enterScalePreviewButton.disabled = false;
  const pageCount = layout.pages.length;
  const colorMode = colorModeButton.getAttribute('aria-pressed') === 'true' ? 'blanco y negro' : 'color';
  layoutInfo.textContent = `${photos.length} fotos · ${pageCount} hoja${pageCount === 1 ? '' : 's'} · tamaños individuales · margen ${config.margin ? '1 cm' : 'sin margen'} · ${colorMode}`;
  layoutInfo.className = 'photo-layout-info photo-count';
  layoutInfo.hidden = false;
  document.documentElement.style.setProperty('--paper-w', `${config.paperWidth}cm`);
  document.documentElement.style.setProperty('--paper-h', `${config.paperHeight}cm`);
  printPageStyle.textContent = `@page{size:${config.paperWidth}cm ${config.paperHeight}cm;margin:0}@media print{html,body{width:${config.paperWidth}cm!important;margin:0!important;padding:0!important;background:#fff!important}.photo-layout,.preview-section,.sheets{margin:0!important;padding:0!important}.sheet-shell{width:${config.paperWidth}cm!important;height:${config.paperHeight}cm!important;margin:0!important;padding:0!important}.print-sheet{width:${config.paperWidth}cm!important;height:${config.paperHeight}cm!important;box-sizing:border-box!important;margin:0!important}}`;
  const isScalePreview = document.body.classList.contains('photo-preview-100');
  const scale = isScalePreview ? 1 : Math.min(1, (sheets.clientWidth - 12) / cmToPx(config.paperWidth));
  for (let page = 0; page < layout.pages.length; page++) {
    const shell = document.createElement('div');
    shell.className = 'sheet-shell';
    shell.style.width = `${cmToPx(config.paperWidth) * scale}px`;
    shell.style.height = `${cmToPx(config.paperHeight) * scale}px`;
    const sheet = document.createElement('div');
    sheet.className = 'print-sheet';
    sheet.style.width = `${config.paperWidth}cm`;
    sheet.style.height = `${config.paperHeight}cm`;
    sheet.style.display = 'block';
    sheet.style.position = 'relative';
    sheet.style.padding = '0';
    sheet.style.transform = `scale(${scale})`;
    sheet.style.transformOrigin = 'top center';
    for (const placement of layout.pages[page]) {
      const {file, photoWidth, photoHeight, fit, cropX, cropY, cropZoom, x, y, fullPage} = placement;
      const frame = document.createElement('div');
      frame.className = `photo-tile${fit === 'contain' ? ' contain' : ''}${photoBorderInput.checked && !fullPage ? '' : ' no-border'}${file === selectedPhoto ? ' selected-for-edit' : ''}`;
      if (file === selectedPhoto && cropEditMode) frame.classList.add('crop-edit-active');
      Object.assign(frame.style, {position: 'absolute', left: `${x}cm`, top: `${y}cm`, width: `${photoWidth}cm`, height: `${photoHeight}cm`});
      frame.dataset.photoName = file.name;
      frame.addEventListener('pointerdown', event => {
        if (event.button !== undefined && event.button !== 0) return;
        if (isCropDoubleTap(event, file)) {
          event.preventDefault();
          event.stopPropagation();
          toggleCropMode(file);
          return;
        }
        if (cropEditMode) {
          selectedPhoto = file;
          renderPhotoEditor();
          event.preventDefault();
          return;
        }
        tileDrag = {pointerId: event.pointerId, file, frame, sheet, startX: event.clientX, startY: event.clientY, originX: x, originY: y, moved: false};
        frame.setPointerCapture(event.pointerId);
        event.preventDefault();
      });
      frame.addEventListener('pointermove', event => {
        if (!tileDrag || tileDrag.pointerId !== event.pointerId || tileDrag.frame !== frame) return;
        const deltaX = event.clientX - tileDrag.startX;
        const deltaY = event.clientY - tileDrag.startY;
        if (Math.abs(deltaX) + Math.abs(deltaY) > 3) tileDrag.moved = true;
        if (!tileDrag.moved) return;
        const sheetRect = sheet.getBoundingClientRect();
        const scaleX = config.paperWidth / Math.max(1, sheetRect.width);
        const scaleY = config.paperHeight / Math.max(1, sheetRect.height);
        const nextX = Math.max(0, Math.min(config.paperWidth - photoWidth, tileDrag.originX + deltaX * scaleX));
        const nextY = Math.max(0, Math.min(config.paperHeight - photoHeight, tileDrag.originY + deltaY * scaleY));
        frame.style.left = `${nextX}cm`;
        frame.style.top = `${nextY}cm`;
      });
      const finishTileDrag = event => {
        if (!tileDrag || tileDrag.pointerId !== event.pointerId || tileDrag.frame !== frame) return;
        const drag = tileDrag;
        tileDrag = null;
        if (!drag.moved) return;
        const sheetRect = sheet.getBoundingClientRect();
        const scaleX = config.paperWidth / Math.max(1, sheetRect.width);
        const scaleY = config.paperHeight / Math.max(1, sheetRect.height);
        const nextX = Math.max(0, Math.min(config.paperWidth - photoWidth, drag.originX + (event.clientX - drag.startX) * scaleX));
        const nextY = Math.max(0, Math.min(config.paperHeight - photoHeight, drag.originY + (event.clientY - drag.startY) * scaleY));
        savePhotoOverride(file, {x: nextX, y: nextY, alignment: 'auto'});
        workStarted = true;
        render();
      };
      frame.addEventListener('pointerup', finishTileDrag);
      frame.addEventListener('pointercancel', finishTileDrag);
      frame.addEventListener('click', event => {
        event.stopPropagation();
        if (event.target.closest('.photo-resize-handle')) return;
        selectPhoto(file);
      });
      const image = document.createElement('img');
      image.src = objectUrl(file);
      image.alt = file.name;
      image.draggable = false;
      const rotation = photoRotations.get(file) || 0;
      image.style.filter = [colorModeButton.getAttribute('aria-pressed') === 'true' ? 'grayscale(1)' : '', photoFilterCss(placement.photoFilter)].filter(Boolean).join(' ') || 'none';
      const imageViewport = document.createElement('div');
      imageViewport.className = 'photo-tile-image-viewport';
      imageViewport.append(image);
      frame.append(imageViewport);
      applyImageCropPresentation(image, file, Math.max(1, cmToPx(photoWidth) - 2), Math.max(1, cmToPx(photoHeight) - 2), rotation);
      if (file === selectedPhoto && !cropEditMode) {
        for (const handleName of ['nw', 'n', 'ne', 'e', 'se', 's', 'sw', 'w']) {
          const handle = document.createElement('button');
          handle.type = 'button';
          handle.className = `photo-resize-handle handle-${handleName}`;
          handle.setAttribute('aria-label', `Cambiar tamaño desde ${handleName} de ${file.name}`);
          handle.dataset.handle = handleName;
          handle.addEventListener('pointerdown', event => {
            event.stopPropagation();
            if (isCropDoubleTap(event, file)) {
              event.preventDefault();
              toggleCropMode(file);
              return;
            }
            if (cropEditMode) return;
            resizeDrag = {pointerId: event.pointerId, file, frame, handle, startX: event.clientX, startY: event.clientY, x, y, width: photoWidth, height: photoHeight};
            handle.setPointerCapture(event.pointerId);
          });
          handle.addEventListener('pointermove', event => {
            if (!resizeDrag || resizeDrag.pointerId !== event.pointerId || resizeDrag.handle !== handle) return;
            const sheetRect = sheet.getBoundingClientRect();
            const scaleX = config.paperWidth / Math.max(1, sheetRect.width);
            const scaleY = config.paperHeight / Math.max(1, sheetRect.height);
            const dx = (event.clientX - resizeDrag.startX) * scaleX;
            const dy = (event.clientY - resizeDrag.startY) * scaleY;
            const edge = handle.dataset.handle;
            let nextX = resizeDrag.x, nextY = resizeDrag.y;
            let nextWidth = resizeDrag.width, nextHeight = resizeDrag.height;
            if (edge.includes('w')) {
              nextX = Math.max(0, Math.min(resizeDrag.x + resizeDrag.width - 1, resizeDrag.x + dx));
              nextWidth = resizeDrag.width + resizeDrag.x - nextX;
            } else if (edge.includes('e')) nextWidth = Math.max(1, Math.min(config.paperWidth - resizeDrag.x, resizeDrag.width + dx));
            if (edge.includes('n')) {
              nextY = Math.max(0, Math.min(resizeDrag.y + resizeDrag.height - 1, resizeDrag.y + dy));
              nextHeight = resizeDrag.height + resizeDrag.y - nextY;
            } else if (edge.includes('s')) nextHeight = Math.max(1, Math.min(config.paperHeight - resizeDrag.y, resizeDrag.height + dy));
            frame.style.left = `${nextX}cm`;
            frame.style.top = `${nextY}cm`;
            frame.style.width = `${nextWidth}cm`;
            frame.style.height = `${nextHeight}cm`;
          });
          const finishResize = event => {
            if (!resizeDrag || resizeDrag.pointerId !== event.pointerId || resizeDrag.handle !== handle) return;
            const drag = resizeDrag;
            resizeDrag = null;
            const sheetRect = sheet.getBoundingClientRect();
            const scaleX = config.paperWidth / Math.max(1, sheetRect.width);
            const scaleY = config.paperHeight / Math.max(1, sheetRect.height);
            const dx = (event.clientX - drag.startX) * scaleX;
            const dy = (event.clientY - drag.startY) * scaleY;
            if (Math.abs(dx) + Math.abs(dy) < 0.05) return;
            const edge = handle.dataset.handle;
            let nextX = drag.x, nextY = drag.y;
            let nextWidth = drag.width, nextHeight = drag.height;
            if (edge.includes('w')) {
              nextX = Math.max(0, Math.min(drag.x + drag.width - 1, drag.x + dx));
              nextWidth = drag.width + drag.x - nextX;
            } else if (edge.includes('e')) nextWidth = Math.max(1, Math.min(config.paperWidth - drag.x, drag.width + dx));
            if (edge.includes('n')) {
              nextY = Math.max(0, Math.min(drag.y + drag.height - 1, drag.y + dy));
              nextHeight = drag.height + drag.y - nextY;
            } else if (edge.includes('s')) nextHeight = Math.max(1, Math.min(config.paperHeight - drag.y, drag.height + dy));
            savePhotoOverride(file, {photoWidth: nextWidth, photoHeight: nextHeight, x: nextX, y: nextY, alignment: 'auto', fullPage: false});
            workStarted = true;
            render();
          };
          handle.addEventListener('pointerup', finishResize);
          handle.addEventListener('pointercancel', finishResize);
          frame.append(handle);
        }
      }
      sheet.append(frame);
    }
    shell.append(sheet);
    sheets.append(shell);
  }
  renderPhotoEditor();
  requestAnimationFrame(applySheetZoom);
}

// El zoom solo amplía el espacio de trabajo. Las medidas físicas de cada hoja
// y las reglas de impresión/Word permanecen intactas.
function applySheetZoom() {
  if (!sheetZoomStage || !sheets) return;
  sheets.style.transform = 'none';
  const width = Math.max(1, sheets.offsetWidth);
  const height = Math.max(1, sheets.offsetHeight);
  const factor = sheetZoom / 100;
  sheets.style.width = `${width}px`;
  sheets.style.transformOrigin = 'top left';
  sheets.style.transform = factor === 1 ? 'none' : `scale(${factor})`;
  sheetZoomStage.style.width = `${width * factor}px`;
  sheetZoomStage.style.height = `${height * factor}px`;
  sheetZoomValue.textContent = `${sheetZoom}%`;
}

function setSheetZoom(value) {
  sheetZoom = Math.max(50, Math.min(200, Math.round(Number(value) / 10) * 10));
  sheetZoomRange.value = String(sheetZoom);
  applySheetZoom();
}

document.getElementById('sheetZoomOut').addEventListener('click', () => setSheetZoom(sheetZoom - 10));
document.getElementById('sheetZoomIn').addEventListener('click', () => setSheetZoom(sheetZoom + 10));
document.getElementById('sheetZoomReset').addEventListener('click', () => setSheetZoom(100));
sheetZoomRange.addEventListener('input', () => setSheetZoom(sheetZoomRange.value));

function makePhotoPlacements(config) {
  const pages = [];
  const maxX = config.paperWidth - config.margin;
  const maxY = config.paperHeight - config.margin;
  let page = [], x = config.margin, y = config.margin, rowHeight = 0;
  for (const file of photos) {
    const current = optionsFor(file, config);
    if (current.fullPage) {
      if (page.length) pages.push(page);
      page = [];
      pages.push([{file, photoWidth: config.paperWidth, photoHeight: config.paperHeight, fit: current.fit || 'cover', cropX: current.cropX ?? 50, cropY: current.cropY ?? 50, cropZoom: current.cropZoom ?? 1, photoFilter: current.photoFilter || 'none', x: 0, y: 0, fullPage: true}]);
      x = config.margin; y = config.margin; rowHeight = 0;
      continue;
    }
    const photoWidth = Number(current.photoWidth) || config.photoWidth;
    const photoHeight = Number(current.photoHeight) || config.photoHeight;
    if (photoWidth > config.paperWidth - config.margin * 2 || photoHeight > config.paperHeight - config.margin * 2) {
      return {pages: [], error: `${file.name}: ${photoWidth.toFixed(1)} × ${photoHeight.toFixed(1)} cm no cabe en la hoja con el margen elegido.`};
    }
    if (current.alignment && current.alignment !== 'auto') {
      if (rowHeight > 0) {
        y += rowHeight + config.gap;
        x = config.margin;
        rowHeight = 0;
      }
      if (y + photoHeight > maxY + 0.001) {
        if (page.length) pages.push(page);
        page = []; x = config.margin; y = config.margin; rowHeight = 0;
      }
      const alignedX = current.alignment === 'center'
        ? (config.paperWidth - photoWidth) / 2
        : current.alignment === 'right'
          ? config.paperWidth - config.margin - photoWidth
          : config.margin;
      page.push({file, photoWidth, photoHeight, fit: current.fit || fitInput.value, cropX: current.cropX ?? 50, cropY: current.cropY ?? 50, cropZoom: current.cropZoom ?? 1, photoFilter: current.photoFilter || 'none', x: alignedX, y});
      y += photoHeight + config.gap;
      x = config.margin;
      rowHeight = 0;
      continue;
    }
    if (x + photoWidth > maxX + 0.001) {
      x = config.margin; y += rowHeight + config.gap; rowHeight = 0;
    }
    if (y + photoHeight > maxY + 0.001) {
      if (page.length) pages.push(page);
      page = []; x = config.margin; y = config.margin; rowHeight = 0;
    }
    const placementX = current.x === undefined ? x : Math.max(0, Math.min(config.paperWidth - photoWidth, Number(current.x)));
    const placementY = current.y === undefined ? y : Math.max(0, Math.min(config.paperHeight - photoHeight, Number(current.y)));
    page.push({file, photoWidth, photoHeight, fit: current.fit || fitInput.value, cropX: current.cropX ?? 50, cropY: current.cropY ?? 50, cropZoom: current.cropZoom ?? 1, photoFilter: current.photoFilter || 'none', x: placementX, y: placementY});
    x += photoWidth + config.gap;
    rowHeight = Math.max(rowHeight, photoHeight);
  }
  if (page.length) pages.push(page);
  return {pages};
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
  render();
});

photoOrientationInput.addEventListener('change', () => {
  if (photoPresetInput.value !== 'photo') return;
  workStarted = true;
  const [width, height] = photoOrientationInput.value === 'landscape' ? [15, 10] : [10, 15];
  widthInput.value = String(width);
  heightInput.value = String(height);
  syncDimensionControls();
  render();
});

for (const control of [paperInput, fitInput]) control.addEventListener('input', () => { workStarted = true; renderSheets(); });
for (const control of [paperInput, paperOrientationInput, includeMarginInput, pageFitInput]) control.addEventListener('change', () => { workStarted = true; syncDimensionControls(); renderSheets(); });
fitInput.addEventListener('change', () => render());
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
photoWidthScale.addEventListener('change', () => render());
heightInput.addEventListener('input', () => {
  workStarted = true;
  photoPresetInput.value = 'custom';
  squareModeInput.checked = false;
  ratioInput.value = 'none';
  syncDimensionControls();
  render();
});
for (const control of [ratioInput, ratioAInput, ratioBInput, widthInput, heightInput, squareModeInput, paperOrientationInput, includeMarginInput, pageFitInput]) {
  control.addEventListener('change', () => render());
}
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
  scalePreviewSlot.append(photoScaleField, squareModeField);
  scalePreviewOutputActions.append(outputControls);
  document.body.classList.add('photo-preview-100');
  renderSheets();
  exitScalePreviewButton.focus();
}

function exitScalePreview() {
  if (scaleFieldNextSibling && scaleFieldNextSibling.parentNode === scaleFieldHome) scaleFieldHome.insertBefore(photoScaleField, scaleFieldNextSibling);
  else scaleFieldHome.append(photoScaleField);
  if (squareModeFieldNextSibling && squareModeFieldNextSibling.parentNode === squareModeFieldHome) squareModeFieldHome.insertBefore(squareModeField, squareModeFieldNextSibling);
  else squareModeFieldHome.append(squareModeField);
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
  if (event.key !== 'Escape') return;
  if (cropEditMode) { closeCrop(); return; }
  if (document.body.classList.contains('photo-preview-100')) exitScalePreview();
});

function preparePhoto(file, widthCm, heightCm, fit, blackWhite, border, rotation = 0, cropX = 50, cropY = 50, cropZoom = 1, photoFilter = 'none') {
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
      const scale = (fit === 'cover'
        ? Math.max((widthPixels - 6) / orientedWidth, (heightPixels - 6) / orientedHeight)
        : Math.min((widthPixels - 6) / orientedWidth, (heightPixels - 6) / orientedHeight)) * Math.max(1, Number(cropZoom) || 1);
      const width = image.naturalWidth * scale;
      const height = image.naturalHeight * scale;
      const visualWidth = swapsAxes ? height : width;
      const visualHeight = swapsAxes ? width : height;
      const horizontal = Math.max(0, Math.min(100, Number(cropX) || 0)) / 100;
      const vertical = Math.max(0, Math.min(100, Number(cropY) || 0)) / 100;
      const drawX = (widthPixels - visualWidth) * horizontal;
      const drawY = (heightPixels - visualHeight) * vertical;
      ctx.filter = [blackWhite ? 'grayscale(1)' : '', filterForPrint(photoFilter)].filter(Boolean).join(' ') || 'none';
      ctx.save();
      ctx.translate(drawX + visualWidth / 2, drawY + visualHeight / 2);
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

function imageDrawing(id, placement) {
  const width = cmToEmu(placement.photoWidth), height = cmToEmu(placement.photoHeight);
  const left = cmToEmu(placement.x), top = cmToEmu(placement.y);
  return `<w:r><w:drawing><wp:anchor distT="0" distB="0" distL="0" distR="0" simplePos="0" relativeHeight="${id}" behindDoc="0" locked="0" layoutInCell="1" allowOverlap="1"><wp:simplePos x="0" y="0"/><wp:positionH relativeFrom="page"><wp:posOffset>${left}</wp:posOffset></wp:positionH><wp:positionV relativeFrom="page"><wp:posOffset>${top}</wp:posOffset></wp:positionV><wp:extent cx="${width}" cy="${height}"/><wp:effectExtent l="0" t="0" r="0" b="0"/><wp:wrapNone/><wp:docPr id="${id}" name="Foto ${id}"/><wp:cNvGraphicFramePr><a:graphicFrameLocks noChangeAspect="1"/></wp:cNvGraphicFramePr><a:graphic><a:graphicData uri="http://schemas.openxmlformats.org/drawingml/2006/picture"><pic:pic><pic:nvPicPr><pic:cNvPr id="${id}" name="Foto ${id}"/><pic:cNvPicPr/></pic:nvPicPr><pic:blipFill><a:blip r:embed="rId${id}"/><a:stretch><a:fillRect/></a:stretch></pic:blipFill><pic:spPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="${width}" cy="${height}"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom></pic:spPr></pic:pic></a:graphicData></a:graphic></wp:anchor></w:drawing></w:r>`;
}

async function exportWord() {
  const config = settings();
  const layout = makePhotoPlacements(config);
  if (layout.error) throw new Error(layout.error);
  const processed = [];
  const blackWhite = colorModeButton.getAttribute('aria-pressed') === 'true';
  const placements = layout.pages.flat();
  for (let index = 0; index < placements.length; index++) {
    const placement = placements[index];
    photoStatus.textContent = `Preparando foto ${index + 1} de ${placements.length} para Word…`;
    processed.push(await preparePhoto(placement.file, placement.photoWidth, placement.photoHeight, placement.fit, blackWhite, photoBorderInput.checked && !placement.fullPage, photoRotations.get(placement.file) || 0, placement.cropX, placement.cropY, placement.cropZoom, placement.photoFilter));
  }
  const relationships = processed.map((_, index) => `<Relationship Id="rId${index + 1}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/image" Target="media/image${index + 1}.jpg"/>`).join('');
  const body = [];
  let placementIndex = 0;
  for (let page = 0; page < layout.pages.length; page++) {
    const anchors = layout.pages[page].map(placement => imageDrawing(++placementIndex, placement)).join('');
    body.push(`<w:p><w:pPr><w:spacing w:before="0" w:after="0"/>${page ? '<w:pageBreakBefore/>' : ''}</w:pPr>${anchors}</w:p>`);
  }
  const paperW = cmToTwips(config.paperWidth), paperH = cmToTwips(config.paperHeight);
  const pageOrientation = config.paperWidth > config.paperHeight ? ' w:orient="landscape"' : '';
  const documentXml = `<?xml version="1.0" encoding="UTF-8" standalone="yes"?><w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:wp="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing" xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:pic="http://schemas.openxmlformats.org/drawingml/2006/picture"><w:body>${body.join('')}<w:sectPr><w:pgSz w:w="${paperW}" w:h="${paperH}"${pageOrientation}/><w:pgMar w:top="0" w:right="0" w:bottom="0" w:left="0" w:header="0" w:footer="0" w:gutter="0"/></w:sectPr></w:body></w:document>`;
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
