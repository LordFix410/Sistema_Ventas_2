(() => {
  const $ = id => document.getElementById(id);
  const pdfInput = $('pdfFile');
  const drop = $('pdfDrop');
  const sourceStatus = $('sourceStatus');
  const pageList = $('pageList');
  const loadSelected = $('loadSelected');
  const cropSection = $('cropSection');
  const cropCanvas = $('cropCanvas');
  const captureSelect = $('captureSelect');
  const zoneSelect = $('zoneSelect');
  const orientationSelect = $('orientationSelect');
  const layoutMode = $('layoutMode');
  const cropWidth = $('cropWidth');
  const cropWidthValue = $('cropWidthValue');
  const cropHeight = $('cropHeight');
  const cropHeightValue = $('cropHeightValue');
  const composeSection = $('composeSection');
  const composeCanvas = $('composeCanvas');
  const layerSelect = $('layerSelect');
  const layerScale = $('layerScale');
  const layerScaleValue = $('layerScaleValue');
  const layerBrightness=$('layerBrightness');
  const layerBrightnessValue=$('layerBrightnessValue');
  const composeZoom=$('composeZoom');
  const composeZoomValue=$('composeZoomValue');
  const composeViewport=document.querySelector('.compose-viewport');
  const finalSection = $('exportSection');
  const finalCanvas = $('finalCanvas');
  const status = $('exportStatus');
  const sourcePages = [];
  let pdf = null;
  let sourceFile = null;
  let captures = [];
  let activeCapture = 0;
  let activeLayer = 0;
  let draggingCrop = false;
  let cropStart = null;
  let composeDrag = null;
  let finalDrag = false;
  let finalStart = null;
  let finalCropVisible = true;
  let finalResizeAnchor = null;
  let finalDragOrigin = null;
  let finalLastPoint = null;
  let finalCropBefore = null;
  let finalCrop = null;
  let layoutWidth = 1;
  let layoutHeight = 1;
  let renderedComposite = null;
  let compositionPages = [];
  let activePage = 0;
  let activeResultPage = 0;
  let undoStack = [];
  let captureUndoStart = null;
  let layerUndoStart = null;
  let scaleUndoStart = null;
  const MAX_CAPTURES = 8;

  const fullRect = canvas => ({x: 0, y: 0, w: canvas.width, h: canvas.height});
  const clamp = (value, low, high) => Math.max(low, Math.min(high, value));
  const makeCanvas = (width, height) => {
    const canvas = document.createElement('canvas');
    canvas.width = Math.max(1, Math.round(width));
    canvas.height = Math.max(1, Math.round(height));
    return canvas;
  };

  function clearDocument() {
    pdf = null;
    sourceFile = null;
    sourcePages.splice(0);
    captures = [];
    compositionPages = [];
    activePage = 0;
    undoStack = [];
    composeZoom.value='1';composeZoomValue.value='100%';composeCanvas.style.width='';composeCanvas.style.maxWidth='';composeCanvas.style.maxHeight='';
    renderedComposite = null;
    finalCrop = null;
    pageList.replaceChildren();
    cropSection.hidden = true;
    composeSection.hidden = true;
    finalSection.hidden = true;
    loadSelected.disabled = true;
    $('cropStatus').textContent = 'La zona seleccionada se usará al unir las imágenes.';
    status.textContent = '';
  }

  function selectedPages() {
    return [...pageList.querySelectorAll('input[type="checkbox"]:checked')]
      .map(input => Number(input.value)).sort((a, b) => a - b);
  }

  function defaultZoneForSelection(index,total){
    if(total===1)return 'full-page';
    if(total===2)return index===0?'top-half':'bottom-half';
    if(index<4)return zonesForMode('corners')[index];
    if(index<6)return total===5?'full-page':index===4?'top-half':'bottom-half';
    return total===7?'full-page':index===6?'top-half':'bottom-half';
  }

  function updateSelectionGroups(){
    const selected=[...pageList.querySelectorAll('.page-option input[type="checkbox"]:checked')];
    selected.forEach((check,index)=>{
      const card=check.closest('.page-option'),zone=card?.querySelector('.capture-zone');
      const group=index<4?1:index<6?2:3;
      card?.classList.remove('capture-group-1','capture-group-2','capture-group-3');
      card?.classList.add(`capture-group-${group}`);
      const tag=card?.querySelector('.capture-group-tag');
      const single=(selected.length===1)||(selected.length===5&&index===4)||(selected.length===7&&index===6);
      const kind=single?'hoja completa':group===1&&selected.length>2?'esquinas':'mitades';
      if(tag)tag.textContent=`Grupo ${group} · ${kind}`;
      if(zone&&!zone.dataset.userSet)zone.value=defaultZoneForSelection(index,selected.length);
    });
    pageList.querySelectorAll('.page-option input[type="checkbox"]:not(:checked)').forEach(check=>{
      const card=check.closest('.page-option');card?.classList.remove('capture-group-1','capture-group-2','capture-group-3');
      const tag=card?.querySelector('.capture-group-tag');if(tag)tag.textContent='';
    });
  }

  async function renderPage(pageNumber, scale) {
    const page = await pdf.getPage(pageNumber);
    const viewport = page.getViewport({scale});
    const canvas = makeCanvas(viewport.width, viewport.height);
    await page.render({canvasContext: canvas.getContext('2d', {alpha: false}), viewport}).promise;
    return canvas;
  }

  function setSelectedCount() {
    const checked = selectedPages();
    loadSelected.disabled = checked.length < 1 || checked.length > MAX_CAPTURES;
    const warning = checked.length > MAX_CAPTURES ? ` El máximo es ${MAX_CAPTURES} capturas.` : '';
    if (checked.length > MAX_CAPTURES) sourceStatus.textContent = `Seleccionaste ${checked.length} páginas. El máximo es ${MAX_CAPTURES}; desmarca algunas para continuar.`;
    $('cropStatus').textContent = checked.length < 1
      ? 'Selecciona al menos una captura.'
      : `${checked.length} captura${checked.length === 1 ? '' : 's'} seleccionada${checked.length === 1 ? '' : 's'}. Puedes elegir hasta ${MAX_CAPTURES}.${warning}`;
  }

  async function openPdf(file) {
    if (!file || (!/\.pdf$/i.test(file.name) && file.type !== 'application/pdf')) {
      sourceStatus.textContent = 'Selecciona un archivo PDF válido.';
      return;
    }
    clearDocument();
    sourceFile = file;
    sourceStatus.textContent = 'Leyendo páginas del PDF…';
    try {
      if (!window.pdfjsLib?.getDocument) throw new Error('No se cargó el lector de PDF. Revisa tu conexión e inténtalo de nuevo.');
      window.pdfjsLib.GlobalWorkerOptions.workerSrc = 'https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.11.174/pdf.worker.min.js';
      pdf = await window.pdfjsLib.getDocument({data: await file.arrayBuffer()}).promise;
      const defaults = new Set(Array.from({length:pdf.numPages},(_,index)=>index+1));
      pageList.replaceChildren();
      for (let number = 1; number <= pdf.numPages; number++) {
        sourceStatus.textContent = `Preparando miniaturas: página ${number} de ${pdf.numPages}…`;
        const canvas = await renderPage(number, 0.25);
        sourcePages.push({number, thumb: canvas});
        const card = document.createElement('div');
        card.className = `page-option${defaults.has(number) ? ' selected' : ''}`;
        const label = document.createElement('label');
        const check = document.createElement('input');
        check.type = 'checkbox';
        check.value = String(number);
        check.checked = defaults.has(number);
        const text = document.createElement('span');
        text.textContent = `Página ${number}`;
        label.append(check, text);
        const groupTag=document.createElement('small');groupTag.className='capture-group-tag';label.append(groupTag);
        const preview = document.createElement('canvas');
        preview.width = canvas.width;
        preview.height = canvas.height;
        preview.getContext('2d').drawImage(canvas, 0, 0);
        card.append(label, preview);
        const zoneLabelElement=document.createElement('label');
        zoneLabelElement.className='page-zone-label';
        zoneLabelElement.textContent='Colocar en';
        const zone=document.createElement('select');
        zone.className='capture-zone';
        zonesForMode('manual').forEach(value=>{const option=document.createElement('option');option.value=value;option.textContent=zoneLabel(value);zone.append(option);});
        zone.value=number===1?'top-left':zonesForMode('corners')[(number-2)%4];
        zone.addEventListener('change',()=>{zone.dataset.userSet='true';});
        zoneLabelElement.append(zone);
        card.append(zoneLabelElement);
        const rotate=document.createElement('button');rotate.type='button';rotate.className='rotate-page';rotate.textContent='Girar 90°';card.dataset.rotation='0';
        rotate.addEventListener('click',event=>{
          event.preventDefault();event.stopPropagation();
          const angle=(Number(card.dataset.rotation||0)+90)%360;card.dataset.rotation=String(angle);
          const pageThumb=sourcePages.find(item=>item.number===number)?.thumb,turned=rotateCanvas(pageThumb,angle);
          preview.width=turned.width;preview.height=turned.height;preview.getContext('2d').clearRect(0,0,preview.width,preview.height);preview.getContext('2d').drawImage(turned,0,0);
          rotate.textContent=`Girar 90° · ${angle}°`;
        });
        card.append(rotate);
        pageList.append(card);
        check.addEventListener('change', () => {
          card.classList.toggle('selected', check.checked);
          updateSelectionGroups();
          setSelectedCount();
        });
      }
      updateSelectionGroups();
      sourceStatus.textContent = `${file.name} · ${pdf.numPages} páginas · ${(file.size / 1024 / 1024).toFixed(2)} MB. Selecciona las páginas que corresponden a las capturas.`;
      setSelectedCount();
    } catch (error) {
      clearDocument();
      sourceStatus.textContent = error.message || 'No se pudo abrir el PDF.';
    }
  }

  pdfInput.addEventListener('change', () => {
    const file = pdfInput.files?.[0];
    pdfInput.value = '';
    openPdf(file);
  });
  for (const name of ['dragenter', 'dragover']) drop.addEventListener(name, event => { event.preventDefault(); drop.classList.add('drag-active'); });
  for (const name of ['dragleave', 'drop']) drop.addEventListener(name, event => { event.preventDefault(); drop.classList.remove('drag-active'); });
  drop.addEventListener('drop', event => { event.stopPropagation(); openPdf([...(event.dataTransfer?.files || [])].find(file => file.type === 'application/pdf' || /\.pdf$/i.test(file.name))); });
  document.addEventListener('dragover', event => { if ([...(event.dataTransfer?.types || [])].includes('Files')) event.preventDefault(); });
  document.addEventListener('drop', event => {
    if (![...(event.dataTransfer?.types || [])].includes('Files')) return;
    event.preventDefault();
    const file = [...(event.dataTransfer?.files || [])].find(item => item.type === 'application/pdf' || /\.pdf$/i.test(item.name));
    if (file) openPdf(file);
  });

  function cropRectFor(capture) {
    if (capture.manualCrop && capture.crop) return capture.crop;
    const source = capture.oriented;
    const zone = capture.zone || 'manual';
    const width = source.width * (Number(capture.coverageX ?? 70) / 100);
    const height = source.height * (Number(capture.coverageY ?? 70) / 100);
    if (zone === 'full-page') return fullRect(source);
    if (zone === 'manual') return fullRect(source);
    if (zone === 'top-half') return {x: 0, y: 0, w: width, h: height};
    if (zone === 'bottom-half') return {x: 0, y: source.height - height, w: width, h: height};
    if (zone === 'left-half') return {x: 0, y: 0, w: width, h: height};
    if (zone === 'right-half') return {x: source.width - width, y: 0, w: width, h: height};
    const x = zone === 'top-right' || zone === 'bottom-right' ? source.width - width : 0;
    const y = zone === 'bottom-left' || zone === 'bottom-right' ? source.height - height : 0;
    return {x, y, w: width, h: height};
  }

  function zonesForMode(mode) {
    if (mode === 'corners') return ['top-left', 'top-right', 'bottom-right', 'bottom-left'];
    if (mode === 'top-bottom') return ['top-half', 'bottom-half'];
    if (mode === 'left-right') return ['left-half', 'right-half'];
    if (mode === 'full-page') return ['full-page'];
    return ['top-left', 'top-right', 'bottom-right', 'bottom-left', 'top-half', 'bottom-half', 'left-half', 'right-half', 'full-page'];
  }

  function zoneLabel(zone) {
    return ({'top-left':'Superior izquierda','top-right':'Superior derecha','bottom-right':'Inferior derecha','bottom-left':'Inferior izquierda','top-half':'Mitad superior','bottom-half':'Mitad inferior','left-half':'Mitad izquierda','right-half':'Mitad derecha','full-page':'Hoja completa','manual':'Recorte manual'})[zone] || 'Recorte manual';
  }

  function setDefaultZones(mode = layoutMode.value) {
    const zones = zonesForMode(mode);
    captures.forEach((capture, index) => {
      capture.zone = zones[index % zones.length] || 'manual';
      capture.rotation=['bottom-half','bottom-right','bottom-left'].includes(capture.zone)?180:0;
      capture.oriented=rotateCanvas(capture.original,capture.rotation);
      setDefaultCoverage(capture);
      capture.manualCrop = false;
      capture.crop = cropRectFor(capture);
    });
    drawCropPreview();
  }

  function setDefaultCoverage(capture) {
    if (capture.zone === 'top-half' || capture.zone === 'bottom-half') { capture.coverageX = 100; capture.coverageY = 60; }
    else if (capture.zone === 'left-half' || capture.zone === 'right-half') { capture.coverageX = 60; capture.coverageY = 100; }
    else if (capture.zone === 'full-page') { capture.coverageX = 100; capture.coverageY = 100; }
    else { capture.coverageX = 70; capture.coverageY = 70; }
  }

  function rotateCanvas(source, degrees) {
    const normalized = ((degrees % 360) + 360) % 360;
    if (normalized === 0) return source;
    const swap = normalized === 90 || normalized === 270;
    const rotated = makeCanvas(swap ? source.height : source.width, swap ? source.width : source.height);
    const context = rotated.getContext('2d');
    context.translate(rotated.width / 2, rotated.height / 2);
    context.rotate(normalized * Math.PI / 180);
    context.drawImage(source, -source.width / 2, -source.height / 2);
    return rotated;
  }

  function drawCropPreview() {
    const capture = captures[activeCapture];
    if (!capture) return;
    if (cropCanvas.width !== capture.oriented.width) cropCanvas.width = capture.oriented.width;
    if (cropCanvas.height !== capture.oriented.height) cropCanvas.height = capture.oriented.height;
    const context = cropCanvas.getContext('2d');
    context.drawImage(capture.oriented, 0, 0);
    const rect = cropRectFor(capture);
    context.save();
    context.fillStyle = '#0b253b8c';
    context.fillRect(0, 0, cropCanvas.width, cropCanvas.height);
    context.clearRect(rect.x, rect.y, rect.w, rect.h);
    context.drawImage(capture.oriented, rect.x, rect.y, rect.w, rect.h, rect.x, rect.y, rect.w, rect.h);
    context.strokeStyle = '#fff';
    context.lineWidth = Math.max(3, cropCanvas.width / 450);
    context.shadowColor = '#10283b';
    context.shadowBlur = 7;
    context.strokeRect(rect.x, rect.y, rect.w, rect.h);
    const handle = Math.max(7, cropCanvas.width / 100);
    for (const [x, y] of [[rect.x,rect.y],[rect.x+rect.w,rect.y],[rect.x,rect.y+rect.h],[rect.x+rect.w,rect.y+rect.h]]) {
      context.beginPath(); context.arc(x, y, handle, 0, Math.PI * 2); context.fillStyle = '#176fa8'; context.fill(); context.strokeStyle = '#fff'; context.lineWidth = 3; context.stroke();
    }
    context.restore();
    captureSelect.value = String(activeCapture);
    zoneSelect.replaceChildren();
    zonesForMode(layoutMode.value).concat(capture.zone && !zonesForMode(layoutMode.value).includes(capture.zone) ? [capture.zone] : []).forEach(zone => {
      const option = document.createElement('option'); option.value = zone; option.textContent = zoneLabel(zone); zoneSelect.append(option);
    });
    zoneSelect.value = capture.zone || 'manual';
    orientationSelect.value = String(capture.rotation || 0);
    cropWidth.value = String(capture.coverageX ?? 70); cropWidthValue.value = `${cropWidth.value}%`;
    cropHeight.value = String(capture.coverageY ?? 70); cropHeightValue.value = `${cropHeight.value}%`;
    $('cropStatus').textContent = `Página ${capture.pageNumber} · ${zoneLabel(capture.zone)} · arrastra para ajustar el área azul.`;
  }

  function populateCaptureSelect() {
    captureSelect.replaceChildren();
    captures.forEach((capture, index) => {
      const option = document.createElement('option'); option.value = String(index); option.textContent = `Captura ${index + 1} · página ${capture.pageNumber}`; captureSelect.append(option);
    });
    activeCapture = 0;
    drawCropPreview();
  }

  loadSelected.addEventListener('click', async () => {
    const numbers = selectedPages();
    if (numbers.length < 1 || numbers.length > MAX_CAPTURES || !pdf) return;
    loadSelected.disabled = true;
    sourceStatus.textContent = 'Cargando capturas en alta resolución…';
    try {
      captures = [];
      for (const number of numbers) {
        sourceStatus.textContent = `Cargando captura de la página ${number}…`;
        const source = await renderPage(number, 2);
        const pageCard=[...pageList.querySelectorAll('.page-option')].find(card=>Number(card.querySelector('input').value)===number);
        const zone=pageCard?.querySelector('.capture-zone')?.value||defaultZoneForSelection(captures.length,numbers.length);
        const rotation=Number(pageCard?.dataset.rotation||0);
        const oriented=rotateCanvas(source,rotation);
        const capture={pageNumber:number,original:source,oriented,rotation,zone,coverageX:70,coverageY:70,crop:null,manualCrop:false,image:source,processed:false,scale:1,x:0,y:0,userBrightness:1};
        setDefaultCoverage(capture);capture.crop=cropRectFor(capture);captures.push(capture);
      }
      cropSection.hidden = true;
      composeSection.hidden = false;
      finalSection.hidden = false;
      prepareCaptureSections();
      sourceStatus.textContent = `${numbers.length} capturas preparadas en ${compositionPages.length} sección${compositionPages.length===1?'':'es'}.`;
      composeSection.scrollIntoView({behavior: 'smooth', block: 'start'});
    } catch (error) {
      sourceStatus.textContent = `No se pudieron cargar las páginas: ${error.message || 'error al leer el PDF.'}`;
    } finally {
      const count = selectedPages().length;
      loadSelected.disabled = count < 1 || count > MAX_CAPTURES;
    }
  });

  captureSelect.addEventListener('change', () => { activeCapture = Number(captureSelect.value); drawCropPreview(); });
  layoutMode.addEventListener('change', () => {
    if (layoutMode.value !== 'manual') setDefaultZones(layoutMode.value);
    else drawCropPreview();
  });
  zoneSelect.addEventListener('change', () => {
    const capture = captures[activeCapture]; if (!capture) return;
    capture.zone = zoneSelect.value;capture.rotation=['bottom-half','bottom-right','bottom-left'].includes(capture.zone)?180:0;capture.oriented=rotateCanvas(capture.original,capture.rotation); setDefaultCoverage(capture); capture.manualCrop = false; capture.crop = cropRectFor(capture); drawCropPreview();
  });
  orientationSelect.addEventListener('change', () => {
    const capture = captures[activeCapture]; if (!capture) return;
    capture.rotation = Number(orientationSelect.value); capture.oriented = rotateCanvas(capture.original, capture.rotation);
    capture.manualCrop = false; capture.crop = cropRectFor(capture); drawCropPreview();
  });
  cropWidth.addEventListener('input', () => {
    const capture = captures[activeCapture]; if (!capture) return;
    capture.coverageX = Number(cropWidth.value); capture.manualCrop = false; capture.crop = cropRectFor(capture);
    cropWidthValue.value = `${cropWidth.value}%`; drawCropPreview();
  });
  cropHeight.addEventListener('input', () => {
    const capture = captures[activeCapture]; if (!capture) return;
    capture.coverageY = Number(cropHeight.value); capture.manualCrop = false; capture.crop = cropRectFor(capture);
    cropHeightValue.value = `${cropHeight.value}%`; drawCropPreview();
  });

  function pointOnCanvas(event, canvas) {
    const rect = canvas.getBoundingClientRect();
    return {x: (event.clientX - rect.left) * canvas.width / rect.width, y: (event.clientY - rect.top) * canvas.height / rect.height};
  }
  cropCanvas.addEventListener('pointerdown', event => {
    if (!captures[activeCapture]) return;
    event.preventDefault(); cropCanvas.setPointerCapture(event.pointerId); draggingCrop = true; cropStart = pointOnCanvas(event, cropCanvas);
    captures[activeCapture].manualCrop = true;
    captures[activeCapture].crop = {x: cropStart.x, y: cropStart.y, w: 1, h: 1}; drawCropPreview();
  });
  cropCanvas.addEventListener('pointermove', event => {
    if (!draggingCrop || !captures[activeCapture]) return;
    const current = pointOnCanvas(event, cropCanvas);
    const left = clamp(Math.min(cropStart.x, current.x), 0, cropCanvas.width - 1);
    const top = clamp(Math.min(cropStart.y, current.y), 0, cropCanvas.height - 1);
    captures[activeCapture].crop = {x: left, y: top, w: Math.max(1, Math.min(cropCanvas.width - left, Math.abs(current.x - cropStart.x))), h: Math.max(1, Math.min(cropCanvas.height - top, Math.abs(current.y - cropStart.y)))};
    drawCropPreview();
  });
  cropCanvas.addEventListener('pointerup', () => {
    draggingCrop = false;
    const capture = captures[activeCapture];
    if (capture && (capture.crop.w < 5 || capture.crop.h < 5)) { capture.manualCrop = false; capture.crop = cropRectFor(capture); drawCropPreview(); }
  });
  cropCanvas.addEventListener('pointercancel', () => { draggingCrop = false; });

  $('rotateCapture').addEventListener('click', () => {
    const capture = captures[activeCapture]; if (!capture) return;
    const next = (capture.rotation + 90) % 360; orientationSelect.value = String(next); orientationSelect.dispatchEvent(new Event('change'));
  });
  $('resetCrop').addEventListener('click', () => { const capture = captures[activeCapture]; if (capture) { capture.manualCrop = false; capture.crop = cropRectFor(capture); drawCropPreview(); } });
  $('applyCrop').addEventListener('click', () => {
    const capture = captures[activeCapture]; if (!capture) return;
    const rect = cropRectFor(capture);
    const x = Math.round(rect.x), y = Math.round(rect.y), w = Math.max(1, Math.round(rect.w)), h = Math.max(1, Math.round(rect.h));
    const cropped = makeCanvas(w, h); cropped.getContext('2d').drawImage(capture.oriented, x, y, w, h, 0, 0, w, h);
    capture.image = cropped; capture.crop = {x, y, w, h}; capture.processed = true;
    $('cropStatus').textContent = `Recorte guardado · ${w} × ${h} px. Puedes elegir otra captura o continuar con la unión.`;
    const nextUnprocessed = captures.findIndex(item => !item.processed);
    if (nextUnprocessed >= 0) { activeCapture = nextUnprocessed; drawCropPreview(); }
    else if (!composeSection.hidden) { setLayout(); activeLayer = 0; updateLayerControls(); buildComposite(); }
    else $('autoStitch').click();
  });

  function setLayout() {
    const sourceWidth = Math.max(...captures.map(item => item.oriented.width));
    const sourceHeight = Math.max(...captures.map(item => item.oriented.height));
    const halfWidth = sourceWidth / 2, halfHeight = sourceHeight / 2;
    const padX = sourceWidth * 0.35, padY = sourceHeight * 0.35;
    const fallback = [[padX, padY], [padX + halfWidth, padY], [padX + halfWidth, padY + halfHeight], [padX, padY + halfHeight]];
    layoutWidth = sourceWidth * 1.7; layoutHeight = sourceHeight * 1.7;
    captures.forEach((item, index) => {
      const zone = item.zone;
      item.scale = 1;
      if (zone === 'top-left' || zone === 'top-half' || zone === 'left-half' || zone === 'full-page') [item.x, item.y] = [padX, padY];
      else if (zone === 'top-right') [item.x, item.y] = [padX + halfWidth, padY];
      else if (zone === 'bottom-right') [item.x, item.y] = [padX + halfWidth, padY + halfHeight];
      else if (zone === 'bottom-left' || zone === 'bottom-half') [item.x, item.y] = [padX, padY + halfHeight];
      else if (zone === 'right-half') [item.x, item.y] = [padX + halfWidth, padY];
      else [item.x, item.y] = fallback[index % fallback.length];
      layoutWidth = Math.max(layoutWidth, Math.ceil(item.x + item.image.width));
      layoutHeight = Math.max(layoutHeight, Math.ceil(item.y + item.image.height));
    });
  }

  function ensureLayoutFits() {
    for (const item of captures) {
      layoutWidth = Math.max(layoutWidth, Math.ceil(item.x + item.image.width * item.scale));
      layoutHeight = Math.max(layoutHeight, Math.ceil(item.y + item.image.height * item.scale));
    }
  }

  function prepareCaptureSections(){
    const allCaptures=[...captures];
    allCaptures.forEach(capture=>{
      const rect=cropRectFor(capture),x=Math.round(rect.x),y=Math.round(rect.y),width=Math.max(1,Math.round(rect.w)),height=Math.max(1,Math.round(rect.h));
      const cropped=makeCanvas(width,height);cropped.getContext('2d').drawImage(capture.oriented,x,y,width,height,0,0,width,height);
      capture.image=cropped;capture.processed=true;capture.scale=1;capture.userBrightness=capture.userBrightness||1;
    });
    compositionPages=[];
    let offset=0;
    for(const [groupIndex,limit] of [4,2,2].entries()){
      if(offset>=allCaptures.length)break;
      const group=allCaptures.slice(offset,offset+limit);offset+=group.length;
      const kind=group.length===1?'hoja completa':group.length===4||groupIndex===0?'esquinas':'mitades';
      const label=`Grupo ${groupIndex+1} · ${kind}`;
      compositionPages.push({captures:group,result:null,width:1,height:1,label});
    }
    const pageSelect=$('composePage');pageSelect.replaceChildren();$('composePageNav').replaceChildren();
    compositionPages.forEach((page,index)=>{
      const option=document.createElement('option');option.value=String(index);option.textContent=`${page.label} (${page.captures.length})`;pageSelect.append(option);
      const button=document.createElement('button');button.type='button';button.className=`layer-chip${index===0?' active':''}`;button.textContent=`${page.label} · ${page.captures.length}`;
      button.addEventListener('click',()=>activateCompositionPage(index));$('composePageNav').append(button);
    });
    $('composePageLabel').hidden=true;
    composeSection.hidden=false;finalSection.hidden=false;activePage=0;activeLayer=0;undoStack=[];
    compositionPages.forEach((page,index)=>{
      captures=page.captures;activePage=index;setLayout();page.width=layoutWidth;page.height=layoutHeight;
      applyToneMatch();updateLayerControls();buildComposite();
    });
    captures=compositionPages[0].captures;activePage=0;activeLayer=0;layoutWidth=compositionPages[0].width;layoutHeight=compositionPages[0].height;
    pageSelect.value='0';updateLayerControls();loadResultPage(0);applyComposeZoom();
    $('composeStatus').textContent=`${compositionPages.length} sección${compositionPages.length===1?'':'es'} lista${compositionPages.length===1?'':'s'}. Selecciona una captura para moverla.`;
  }

  function drawComposition(showSelection=true) {
    if (!captures.length) return;
    const width = Math.ceil(layoutWidth), height = Math.ceil(layoutHeight);
    if (composeCanvas.width !== width) composeCanvas.width = width;
    if (composeCanvas.height !== height) composeCanvas.height = height;
    const context = composeCanvas.getContext('2d', {alpha: false});
    context.fillStyle = '#fff'; context.fillRect(0, 0, width, height);
    captures.forEach((item, index) => {
      const w = item.image.width * item.scale, h = item.image.height * item.scale;
      context.save();
      const autoTone=item.toneFactor||1;
      context.filter=`brightness(${Math.round(autoTone*(item.userBrightness||1)*100)}%)`;
      context.drawImage(item.image, item.x, item.y, w, h);
      context.restore();
      if (showSelection && index === activeLayer) { context.save(); context.strokeStyle = '#eb8a00'; context.lineWidth = Math.max(2, width / 800); context.setLineDash([14, 8]); context.strokeRect(item.x, item.y, w, h); context.restore(); }
    });
  }

  function drawFinalPreview() {
    if (!renderedComposite) return;
    if (finalCanvas.width !== renderedComposite.width) finalCanvas.width = renderedComposite.width;
    if (finalCanvas.height !== renderedComposite.height) finalCanvas.height = renderedComposite.height;
    const context = finalCanvas.getContext('2d'); context.drawImage(renderedComposite, 0, 0);
    if(!finalCropVisible)return;
    const rect = finalCrop || fullRect(renderedComposite);
    context.fillStyle = '#0b253b88';
    context.fillRect(0, 0, finalCanvas.width, finalCanvas.height);
    context.clearRect(rect.x, rect.y, rect.w, rect.h);
    context.drawImage(renderedComposite, rect.x, rect.y, rect.w, rect.h, rect.x, rect.y, rect.w, rect.h);
    context.strokeStyle = '#fff'; context.lineWidth = Math.max(3, finalCanvas.width / 500); context.shadowColor = '#10283b'; context.shadowBlur = 8; context.strokeRect(rect.x, rect.y, rect.w, rect.h);
    const radius=Math.max(9,finalCanvas.width/120);
    for(const [x,y] of [[rect.x,rect.y],[rect.x+rect.w,rect.y],[rect.x,rect.y+rect.h],[rect.x+rect.w,rect.y+rect.h]]){
      context.beginPath();context.arc(x,y,radius,0,Math.PI*2);context.fillStyle='#176fa8';context.fill();context.strokeStyle='#fff';context.lineWidth=Math.max(2,radius*.22);context.stroke();
    }
  }

  function buildComposite() {
    if (!captures.length) return;
    finalCropVisible=true;
    drawComposition(false);
    renderedComposite = makeCanvas(composeCanvas.width, composeCanvas.height);
    renderedComposite.getContext('2d', {alpha: false}).drawImage(composeCanvas, 0, 0);
    const minX = Math.min(...captures.map(item => item.x));
    const minY = Math.min(...captures.map(item => item.y));
    const maxX = Math.max(...captures.map(item => item.x + item.image.width * item.scale));
    const maxY = Math.max(...captures.map(item => item.y + item.image.height * item.scale));
    finalCrop = {x: minX, y: minY, w: maxX - minX, h: maxY - minY};
    if (compositionPages[activePage]) compositionPages[activePage].result = {canvas: renderedComposite, crop: {...finalCrop}, defaultCrop:{...finalCrop}};
    drawComposition(true);
    refreshResultPageOptions();
    finalSection.hidden = false;
    drawFinalPreview();
    $('exportStatus').textContent = `Unión lista · ${renderedComposite.width} × ${renderedComposite.height} px. Arrastra en la vista final para quitar cualquier borde sobrante.`;
  }

  function updateLayerControls() {
    layerSelect.replaceChildren();
    $('layerList').replaceChildren();
    captures.forEach((capture, index) => {
      const option = document.createElement('option'); option.value = String(index); option.textContent = `Captura ${index + 1} · página ${capture.pageNumber}`; layerSelect.append(option);
      const chip = document.createElement('button'); chip.type = 'button'; chip.className = `layer-chip${index === activeLayer ? ' active' : ''}`; chip.textContent = `Captura ${index + 1}`;
      chip.addEventListener('click', () => { activeLayer = index; layerSelect.value = String(index); updateLayerControls(); drawComposition(); });
      $('layerList').append(chip);
    });
    if (captures.length) {
      layerSelect.value = String(activeLayer);
      layerScale.value = String(captures[activeLayer].scale);
      layerScaleValue.value = `${Math.round(captures[activeLayer].scale * 100)}%`;
      layerBrightness.value=String(Math.round((captures[activeLayer].userBrightness||1)*100));
      layerBrightnessValue.value=`${layerBrightness.value}%`;
    }
  }

  function snapshotComposition() {
    if (!captures.length) return;
    undoStack.push(captures.map(item => ({x:item.x,y:item.y,scale:item.scale,rotation:item.layerRotation||0,image:item.image,userBrightness:item.userBrightness||1})));
    if (undoStack.length > 40) undoStack.shift();
  }

  function undoComposition() {
    const previous = undoStack.pop();
    if (!previous) return;
    previous.forEach((state,index) => {
      const item = captures[index]; if (!item) return;
      item.x=state.x; item.y=state.y; item.scale=state.scale; item.layerRotation=state.rotation; item.image=state.image;item.userBrightness=state.userBrightness;
    });
    ensureLayoutFits(); updateLayerControls(); buildComposite();
    $('composeStatus').textContent='Se deshizo el último cambio.';
  }

  function applyToneMatch() {
    const luminances = captures.map(item => {
      const sample = makeCanvas(32,32), ctx=sample.getContext('2d',{willReadFrequently:true});
      ctx.drawImage(item.image,0,0,32,32);
      const data=ctx.getImageData(0,0,32,32).data; let sum=0,count=0;
      for(let i=0;i<data.length;i+=16){const luminance=.2126*data[i]+.7152*data[i+1]+.0722*data[i+2];if(luminance>150){sum+=luminance;count++;}}
      return sum/Math.max(1,count);
    });
    const target=luminances.reduce((sum,value)=>sum+value,0)/Math.max(1,luminances.length);
    captures.forEach((item,index)=>{ item.toneFactor=clamp(Math.pow(target/Math.max(1,luminances[index]),.4),.88,1.14); });
  }

  function refreshResultPageOptions() {
    const select=$('resultPage'), nav=$('resultPageNav');
    const results=compositionPages.map((page,index)=>page.result?index:-1).filter(index=>index>=0);
    select.replaceChildren();
    nav.replaceChildren();
    results.forEach(index=>{
      const label=compositionPages[index].label||`Página ${index+1}`;
      const option=document.createElement('option');option.value=String(index);option.textContent=label;select.append(option);
      const button=document.createElement('button');button.type='button';button.className=`layer-chip${index===activeResultPage?' active':''}`;button.textContent=label;
      button.addEventListener('click',()=>loadResultPage(index));nav.append(button);
    });
    $('resultPageLabel').hidden=true;
    if(results.length){activeResultPage=results.includes(activePage)?activePage:results[0];select.value=String(activeResultPage);loadResultPage(activeResultPage);}
  }

  function loadResultPage(index) {
    const result=compositionPages[index]?.result; if(!result)return;
    activeResultPage=index;renderedComposite=result.canvas;finalCrop={...result.crop};finalCropVisible=true;$('resultPage').value=String(index);
    $('resultPageNav').querySelectorAll('.layer-chip').forEach((button,buttonIndex)=>button.classList.toggle('active',buttonIndex===index));drawFinalPreview();
  }

  function activateCompositionPage(index) {
    if(index===activePage || !compositionPages[index])return;
    if(compositionPages[activePage])compositionPages[activePage].captures=captures;
    activePage=index; captures=compositionPages[index].captures;
    layoutWidth=compositionPages[index].width; layoutHeight=compositionPages[index].height;
    $('composePage').value=String(index);activeLayer=0;undoStack=[];updateLayerControls();
    $('composePageNav').querySelectorAll('.layer-chip').forEach((button,buttonIndex)=>button.classList.toggle('active',buttonIndex===index));buildComposite();
    $('resultPage').value=String(index);
    $('composeStatus').textContent=`Editando página ${index+1}. Las flechas mueven la captura activa y Ctrl+Z deshace cambios.`;
  }

  function initializeComposition() {
    if (captures.length < 1 || captures.some(item => !item.image || !item.processed)) return;
    if (!composeSection.hidden) return;
    setLayout(); activeLayer = 0; updateLayerControls(); composeSection.hidden = false; buildComposite();
  }

  $('applyCrop').addEventListener('dblclick', event => event.preventDefault());
  layerSelect.addEventListener('change', () => { activeLayer = Number(layerSelect.value); updateLayerControls(); drawComposition(); });
  layerBrightness.addEventListener('pointerdown',()=>{if(!scaleUndoStart){snapshotComposition();scaleUndoStart=true;}});
  layerBrightness.addEventListener('input',()=>{
    const item=captures[activeLayer];if(!item)return;
    if(!scaleUndoStart){snapshotComposition();scaleUndoStart=true;}
    item.userBrightness=Number(layerBrightness.value)/100;layerBrightnessValue.value=`${layerBrightness.value}%`;drawComposition();
  });
  layerBrightness.addEventListener('change',()=>{scaleUndoStart=false;buildComposite();});
  layerScale.addEventListener('input', () => {
    const item = captures[activeLayer]; if (!item) return;
    const centerX = item.x + item.image.width * item.scale / 2;
    const centerY = item.y + item.image.height * item.scale / 2;
    if (!scaleUndoStart) { snapshotComposition(); scaleUndoStart = true; }
    item.scale = Number(layerScale.value);
    item.x = clamp(centerX - item.image.width * item.scale / 2, 0, Math.max(0, layoutWidth - item.image.width * item.scale));
    item.y = clamp(centerY - item.image.height * item.scale / 2, 0, Math.max(0, layoutHeight - item.image.height * item.scale));
    ensureLayoutFits(); layerScaleValue.value = `${Math.round(item.scale * 100)}%`; drawComposition();
  });
  layerScale.addEventListener('change', () => {scaleUndoStart=false;buildComposite();});

  function applyComposeZoom(keepPoint=null,previous=Number(composeZoom.value)){
    const zoom=Number(composeZoom.value);
    composeZoomValue.value=`${Math.round(zoom*100)}%`;
    composeCanvas.style.width=`${zoom*100}%`;
    composeCanvas.style.maxWidth=zoom>1?'none':'100%';
    composeCanvas.style.maxHeight=zoom>1?'none':'';
    if(keepPoint)requestAnimationFrame(()=>{
      composeViewport.scrollLeft=(keepPoint.x+composeViewport.scrollLeft)*zoom/previous-keepPoint.x;
      composeViewport.scrollTop=(keepPoint.y+composeViewport.scrollTop)*zoom/previous-keepPoint.y;
    });
  }
  composeZoom.addEventListener('input',()=>applyComposeZoom());
  composeViewport.addEventListener('wheel',event=>{
    if(!event.ctrlKey&&!event.metaKey)return;
    event.preventDefault();
    const previous=Number(composeZoom.value),rect=composeViewport.getBoundingClientRect();
    composeZoom.value=String(clamp(previous+(event.deltaY < 0 ? 0.1 : -0.1),1,4));
    applyComposeZoom({x:event.clientX-rect.left,y:event.clientY-rect.top},previous);
  },{passive:false});
  $('composePage').addEventListener('change',()=>activateCompositionPage(Number($('composePage').value)));
  $('resultPage').addEventListener('change',()=>loadResultPage(Number($('resultPage').value)));
  $('uniteZones').addEventListener('click',()=>{
    const returnPage=activePage;
    compositionPages.forEach((page,index)=>{
      captures=page.captures;activePage=index;setLayout();page.width=layoutWidth;page.height=layoutHeight;
      applyToneMatch();updateLayerControls();buildComposite();
    });
    activePage=Math.min(returnPage,compositionPages.length-1);captures=compositionPages[activePage].captures;
    layoutWidth=compositionPages[activePage].width;layoutHeight=compositionPages[activePage].height;
    $('composePage').value=String(activePage);activeLayer=0;updateLayerControls();loadResultPage(activePage);
    $('composeStatus').textContent='Zonas unidas. Puedes arrastrar las capturas para ajustar los empalmes.';
  });
  $('fullscreenCompose').addEventListener('click',async()=>{
    try{
      if(!document.fullscreenElement){await composeSection.requestFullscreen();$('fullscreenCompose').textContent='Salir de pantalla completa';$('fullscreenCompose').setAttribute('aria-pressed','true');}
      else await document.exitFullscreen();
    }catch{$('composeStatus').textContent='El navegador no permitió pantalla completa. Puedes ampliar la ventana para editar.';}
  });
  document.addEventListener('fullscreenchange',()=>{
    const full=Boolean(document.fullscreenElement);$('fullscreenCompose').textContent=full?'Salir de pantalla completa':'Editar a pantalla completa';$('fullscreenCompose').setAttribute('aria-pressed',String(full));
    requestAnimationFrame(()=>applyComposeZoom());
  });

  document.addEventListener('keydown',event=>{
    if(composeSection.hidden || !captures.length)return;
    if((event.ctrlKey||event.metaKey)&&event.key.toLowerCase()==='z'){
      event.preventDefault();undoComposition();return;
    }
    const delta=event.shiftKey?10:1;
    const movement={ArrowLeft:[-delta,0],ArrowRight:[delta,0],ArrowUp:[0,-delta],ArrowDown:[0,delta]}[event.key];
    if(!movement)return;
    if(['INPUT','SELECT','TEXTAREA'].includes(document.activeElement?.tagName))return;
    event.preventDefault();const item=captures[activeLayer];if(!item)return;
    if(!layerUndoStart){snapshotComposition();layerUndoStart=true;}
    item.x=clamp(item.x+movement[0],0,Math.max(0,layoutWidth-item.image.width*item.scale));
    item.y=clamp(item.y+movement[1],0,Math.max(0,layoutHeight-item.image.height*item.scale));
    drawComposition();
  });
  window.addEventListener('keyup',event=>{if(event.key.startsWith('Arrow')){layerUndoStart=false;buildComposite();}});

  composeCanvas.addEventListener('pointerdown', event => {
    composeCanvas.focus({preventScroll:true});
    const point = pointOnCanvas(event, composeCanvas);
    for (let index = captures.length - 1; index >= 0; index--) {
      const item = captures[index], width = item.image.width * item.scale, height = item.image.height * item.scale;
      if (point.x >= item.x && point.x <= item.x + width && point.y >= item.y && point.y <= item.y + height) {
        snapshotComposition(); layerUndoStart=true; activeLayer = index; composeDrag = {pointerId: event.pointerId, x: point.x, y: point.y}; composeCanvas.setPointerCapture(event.pointerId); updateLayerControls(); drawComposition(); break;
      }
    }
  });
  composeCanvas.addEventListener('pointermove', event => {
    if (!composeDrag || composeDrag.pointerId !== event.pointerId) return;
    const point = pointOnCanvas(event, composeCanvas), item = captures[activeLayer];
    item.x = clamp(item.x + point.x - composeDrag.x, 0, Math.max(0, layoutWidth - item.image.width * item.scale));
    item.y = clamp(item.y + point.y - composeDrag.y, 0, Math.max(0, layoutHeight - item.image.height * item.scale));
    composeDrag.x = point.x; composeDrag.y = point.y;
    drawComposition();
  });
  composeCanvas.addEventListener('pointerup', event => { if (composeDrag) { composeDrag = null; layerUndoStart=false; buildComposite(); } });
  composeCanvas.addEventListener('pointercancel', () => { composeDrag = null; layerUndoStart=false; });

  finalCanvas.addEventListener('pointerdown', event => {
    event.preventDefault();finalCanvas.setPointerCapture(event.pointerId);finalDrag=true;finalStart=pointOnCanvas(event,finalCanvas);finalDragOrigin={...finalStart};finalLastPoint={...finalStart};finalCropBefore={...(finalCrop||fullRect(renderedComposite))};const hadVisibleSelection=finalCropVisible;finalCropVisible=true;
    const rect=finalCropBefore,radius=Math.max(14,finalCanvas.width/70);
    const handles=[{x:rect.x,y:rect.y,opposite:{x:rect.x+rect.w,y:rect.y+rect.h}},{x:rect.x+rect.w,y:rect.y,opposite:{x:rect.x,y:rect.y+rect.h}},{x:rect.x,y:rect.y+rect.h,opposite:{x:rect.x+rect.w,y:rect.y}},{x:rect.x+rect.w,y:rect.y+rect.h,opposite:{x:rect.x,y:rect.y}}];
    const handle=hadVisibleSelection?handles.find(point=>Math.hypot(point.x-finalStart.x,point.y-finalStart.y)<=radius):null;
    if(handle){finalResizeAnchor=handle.opposite;}
    else{finalResizeAnchor=null;finalCrop={x:finalStart.x,y:finalStart.y,w:1,h:1};}
    drawFinalPreview();
  });
  finalCanvas.addEventListener('pointermove', event => {
    if (!finalDrag || !renderedComposite) return;
    const point = pointOnCanvas(event, finalCanvas), anchor=finalResizeAnchor||finalStart;
    finalLastPoint=point;
    const left=clamp(Math.min(anchor.x,point.x),0,finalCanvas.width-1),top=clamp(Math.min(anchor.y,point.y),0,finalCanvas.height-1);
    finalCrop = {x:left,y:top,w:Math.max(1,Math.min(finalCanvas.width-left,Math.abs(point.x-anchor.x))),h:Math.max(1,Math.min(finalCanvas.height-top,Math.abs(point.y-anchor.y)))};
    drawFinalPreview();
  });
  finalCanvas.addEventListener('pointerup', () => {
    finalDrag = false;
    const moved=finalLastPoint&&Math.hypot(finalStart.x-finalLastPoint.x,finalStart.y-finalLastPoint.y)>4;
    const wasResize=Boolean(finalResizeAnchor);
    if(!moved&&!wasResize){
      const old=finalCropBefore||fullRect(renderedComposite);
      const outside=finalStart.x<old.x||finalStart.x>old.x+old.w||finalStart.y<old.y||finalStart.y>old.y+old.h;
      finalCrop={...old};if(outside)finalCropVisible=false;drawFinalPreview();
    }
    if (moved&&finalCrop&&(finalCrop.w<5||finalCrop.h<5)){finalCrop={...(finalCropBefore||fullRect(renderedComposite))};drawFinalPreview();}
    if(compositionPages[activeResultPage]?.result)compositionPages[activeResultPage].result.crop={...finalCrop};
    finalResizeAnchor=null;finalDragOrigin=null;finalLastPoint=null;finalCropBefore=null;
  });
  finalCanvas.addEventListener('pointercancel', () => { finalDrag=false;if(finalCropBefore)finalCrop={...finalCropBefore};finalCropVisible=true;finalResizeAnchor=null;finalLastPoint=null;finalCropBefore=null;drawFinalPreview(); });
  $('resetFinalCrop').addEventListener('click', () => { if (renderedComposite) { const result=compositionPages[activeResultPage]?.result;finalCrop={...(result?.defaultCrop||fullRect(renderedComposite))};finalCropVisible=true;if(result)result.crop={...finalCrop};drawFinalPreview();$('exportStatus').textContent='Se usará toda la unión.'; } });

  document.addEventListener('pointerdown',event=>{
    if(finalSection.hidden||!finalCropVisible)return;
    if(!event.target.closest('.final-preview')){finalCropVisible=false;drawFinalPreview();}
  });

  function getFinalCanvas(pageIndex=activeResultPage) {
    const result=compositionPages[pageIndex]?.result;
    if (!result) throw new Error('Primero prepara la unión de todas las páginas.');
    const composite=result.canvas,rect=result.crop||fullRect(composite);
    const x = clamp(Math.round(rect.x), 0, composite.width - 1), y = clamp(Math.round(rect.y), 0, composite.height - 1);
    const width = clamp(Math.round(rect.w), 1, composite.width - x), height = clamp(Math.round(rect.h), 1, composite.height - y);
    const output = makeCanvas(width, height);
    output.getContext('2d', {alpha: false}).drawImage(composite, x, y, width, height, 0, 0, width, height);
    return output;
  }

  function saveBlob(blob, filename) {
    const url = URL.createObjectURL(blob), link = document.createElement('a'); link.href = url; link.download = filename; document.body.append(link); link.click(); link.remove(); setTimeout(() => URL.revokeObjectURL(url), 60000);
  }
  function baseName() { return (sourceFile?.name || 'escaneo.pdf').replace(/\.pdf$/i, '').replace(/[\\/:*?"<>|]+/g, '-').trim() || 'escaneo'; }
  function canvasBlob(canvas, type = 'image/png') { return new Promise((resolve, reject) => canvas.toBlob(blob => blob ? resolve(blob) : reject(new Error('El navegador no pudo preparar la imagen.')), type)); }

  $('downloadPng').addEventListener('click', async () => {
    try {
      const images=compositionPages.map((_,index)=>getFinalCanvas(index));
      if(images.length===1)saveBlob(await canvasBlob(images[0]),`${baseName()}_unido_sin_bordes.png`);
      else{
        if(!window.JSZip)throw new Error('No se cargó el empaquetador de imágenes. Revisa tu conexión.');
        const zip=new window.JSZip();
        for(let i=0;i<images.length;i++)zip.file(`${baseName()}_pagina_${i+1}.png`,await canvasBlob(images[i]));
        saveBlob(await zip.generateAsync({type:'blob'}),`${baseName()}_paginas_PNG.zip`);
      }
      $('exportStatus').textContent=images.length>1?`Se descargaron ${images.length} imágenes PNG dentro de un ZIP.`:'Imagen PNG descargada sin bordes ni guías de selección.';
    }
    catch (error) { $('exportStatus').textContent = error.message; }
  });

  function fitImage(image, pageWidth, pageHeight) {
    const scale = Math.min(pageWidth / image.width, pageHeight / image.height);
    return {width: image.width * scale, height: image.height * scale};
  }

  function wordDocumentXml(pageImages, pageWidth, pageHeight) {
    const orientation = pageWidth > pageHeight ? ' w:orient="landscape"' : '';
    const paragraphs=pageImages.map((entry,index)=>{
      const {image,fit}=entry,x=Math.round((pageWidth-fit.width)/2),y=Math.round((pageHeight-fit.height)/2),rid=`rId${index+1}`;
      const pageBreak=index?'<w:p><w:r><w:br w:type="page"/></w:r></w:p>':'';
      return `${pageBreak}<w:p><w:pPr><w:spacing w:before="0" w:after="0" w:line="1" w:lineRule="exact"/></w:pPr><w:r><w:drawing><wp:anchor distT="0" distB="0" distL="0" distR="0" simplePos="0" relativeHeight="251658240" behindDoc="1" locked="0" layoutInCell="1" allowOverlap="1"><wp:simplePos x="0" y="0"/><wp:positionH relativeFrom="page"><wp:posOffset>${x}</wp:posOffset></wp:positionH><wp:positionV relativeFrom="page"><wp:posOffset>${y}</wp:posOffset></wp:positionV><wp:extent cx="${Math.round(fit.width)}" cy="${Math.round(fit.height)}"/><wp:effectExtent l="0" t="0" r="0" b="0"/><wp:wrapNone/><wp:docPr id="${index+1}" name="Página ${index+1}"/><wp:cNvGraphicFramePr><a:graphicFrameLocks noChangeAspect="1"/></wp:cNvGraphicFramePr><a:graphic><a:graphicData uri="http://schemas.openxmlformats.org/drawingml/2006/picture"><pic:pic><pic:nvPicPr><pic:cNvPr id="${index+1}" name="Página ${index+1}"/><pic:cNvPicPr/></pic:nvPicPr><pic:blipFill><a:blip r:embed="${rid}"/><a:stretch><a:fillRect/></a:stretch></pic:blipFill><pic:spPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="${Math.round(fit.width)}" cy="${Math.round(fit.height)}"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom></pic:spPr></pic:pic></a:graphicData></a:graphic></wp:anchor></w:drawing></w:r></w:p>`;
    }).join('');
    return `<?xml version="1.0" encoding="UTF-8" standalone="yes"?><w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:wp="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing" xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:pic="http://schemas.openxmlformats.org/drawingml/2006/picture"><w:body>${paragraphs}<w:sectPr><w:pgSz w:w="${Math.round(pageWidth/635)}" w:h="${Math.round(pageHeight/635)}"${orientation}/><w:pgMar w:top="0" w:right="0" w:bottom="0" w:left="0" w:header="0" w:footer="0" w:gutter="0"/></w:sectPr></w:body></w:document>`;
  }

  $('downloadWord').addEventListener('click', async () => {
    try {
      if (!window.JSZip) throw new Error('No se cargó el creador de Word. Revisa tu conexión.');
      $('exportStatus').textContent = 'Preparando Word con la imagen recortada y sin márgenes de página…';
      const images=compositionPages.map((_,index)=>getFinalCanvas(index));
      const widthMm=215.9,heightMm=329;
      const pageWidth = widthMm / 25.4 * 914400, pageHeight = heightMm / 25.4 * 914400;
      const pageImages=images.map(image=>({image,fit:fitImage(image,pageWidth,pageHeight)}));
      const zip = new window.JSZip();
      zip.file('[Content_Types].xml', '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Default Extension="png" ContentType="image/png"/><Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/></Types>');
      zip.folder('_rels').file('.rels', '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/></Relationships>');
      zip.folder('word').file('document.xml', wordDocumentXml(pageImages,pageWidth,pageHeight));
      const relationships=images.map((_,index)=>`<Relationship Id="rId${index+1}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/image" Target="media/page-${index+1}.png"/>`).join('');
      zip.folder('word').folder('_rels').file('document.xml.rels', `<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">${relationships}</Relationships>`);
      for(let i=0;i<images.length;i++)zip.folder('word').folder('media').file(`page-${i+1}.png`,await canvasBlob(images[i]).then(blob=>blob.arrayBuffer()));
      const blob = await zip.generateAsync({type: 'blob', mimeType: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document'});
      saveBlob(blob, `${baseName()}_oficio_sin_bordes.docx`);
      $('exportStatus').textContent = `Word Oficio descargado con ${images.length} sección${images.length===1?'':'es'} y márgenes cero.`;
    } catch (error) { $('exportStatus').textContent = error.message || 'No se pudo preparar el documento de Word.'; }
  });

  async function receiveTransferredFile() {
    if (!window.LocalFileTransfer) return;
    try { const files = await window.LocalFileTransfer.take(); const file = files.find(item => item.type === 'application/pdf' || /\.pdf$/i.test(item.name)); if (file) openPdf(file); }
    catch (error) { sourceStatus.textContent = error.message || 'No se pudo recibir el PDF.'; }
  }
  receiveTransferredFile();
})();
