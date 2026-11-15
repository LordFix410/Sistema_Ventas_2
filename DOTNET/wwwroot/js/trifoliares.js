(() => {
  const $ = selector => document.querySelector(selector);
  const fileInput = $('#pdfFile');
  const dropZone = $('#pdfDrop');
  const sourceStatus = $('#sourceStatus');
  const processStatus = $('#processStatus');
  const prepareButton = $('#preparePdf');
  const printButton = $('#printPdf');
  const resultSection = $('#resultSection');
  const downloadLink = $('#downloadPdf');
  const wordButton = $('#downloadWord');
  const preview = $('#pdfPreview');
  let sourceFile = null;
  let outputUrl = null;
  let outputBytes = null;
  let busy = false;
  let makingWord = false;

  function clearOutput() {
    resultSection.hidden = true;
    preview.onload = null;
    preview.removeAttribute('src');
    downloadLink.removeAttribute('href');
    printButton.disabled = true;
    wordButton.disabled = true;
    if (outputUrl) URL.revokeObjectURL(outputUrl);
    outputUrl = null;
    outputBytes = null;
  }

  function showSource(file) {
    clearOutput();
    if (!file || (file.type !== 'application/pdf' && !/\.pdf$/i.test(file.name))) {
      sourceFile = null;
      prepareButton.disabled = true;
      sourceStatus.textContent = 'Selecciona un archivo PDF válido.';
      return;
    }
    sourceFile = file;
    prepareButton.disabled = false;
    sourceStatus.textContent = `${file.name} · ${(file.size / 1024 / 1024).toFixed(2)} MB`;
    processStatus.textContent = '';
  }

  fileInput.addEventListener('change', () => {
    showSource(fileInput.files?.[0]);
    fileInput.value = '';
  });

  for (const eventName of ['dragenter', 'dragover']) {
    dropZone.addEventListener(eventName, event => {
      event.preventDefault();
      dropZone.classList.add('drag-active');
    });
  }
  for (const eventName of ['dragleave', 'drop']) {
    dropZone.addEventListener(eventName, event => {
      event.preventDefault();
      dropZone.classList.remove('drag-active');
    });
  }
  dropZone.addEventListener('drop', event => showSource(event.dataTransfer?.files?.[0]));

  document.addEventListener('dragover', event => {
    if ([...(event.dataTransfer?.types || [])].includes('Files')) event.preventDefault();
  });
  document.addEventListener('drop', event => {
    if (![...(event.dataTransfer?.types || [])].includes('Files')) return;
    event.preventDefault();
    const file = [...(event.dataTransfer.files || [])].find(item => item.type === 'application/pdf' || /\.pdf$/i.test(item.name));
    if (file) showSource(file);
  });

  async function takeTransferredFiles() {
    if (!window.LocalFileTransfer) return;
    try {
      const files = await window.LocalFileTransfer.take();
      if (files.length) showSource(files.find(file => file.type === 'application/pdf' || /\.pdf$/i.test(file.name)));
    } catch (error) {
      sourceStatus.textContent = error.message || 'No se pudo recibir el PDF.';
    }
  }

  function outputPaper(size, orientation) {
    const mmToPoints = mm => mm * 72 / 25.4;
    const width = mmToPoints(215.9); // 8.5 pulgadas
    const height = mmToPoints(size === 'oficio' ? 329 : 279.4); // Oficio local o Carta
    return orientation === 'landscape' ? [height, width] : [width, height];
  }

  async function preparePdf() {
    if (!sourceFile || busy) return;
    if (!window.PDFLib?.PDFDocument) {
      processStatus.textContent = 'No se pudo cargar el componente para preparar PDF. Revisa la conexión e inténtalo de nuevo.';
      return;
    }
    const marginCm = Number($('#pageMargin').value);
    if (!Number.isFinite(marginCm) || marginCm < 0 || marginCm > 3) {
      processStatus.textContent = 'El margen debe estar entre 0 y 3 cm.';
      $('#pageMargin').focus();
      return;
    }

    busy = true;
    prepareButton.disabled = true;
    processStatus.textContent = 'Abriendo el PDF…';
    clearOutput();
    try {
      const {PDFDocument} = window.PDFLib;
      const original = await PDFDocument.load(await sourceFile.arrayBuffer());
      const originalPages = original.getPages();
      if (!originalPages.length) throw new Error('El PDF no contiene páginas.');

      const orientationChoice = $('#paperOrientation').value;
      const firstPage = originalPages[0];
      const landscape = orientationChoice === 'landscape' || (orientationChoice === 'auto' && firstPage.getWidth() > firstPage.getHeight());
      const [targetWidth, targetHeight] = outputPaper($('#paperSize').value, landscape ? 'landscape' : 'portrait');
      const margin = marginCm * 72 / 2.54;
      const availableWidth = targetWidth - margin * 2;
      const availableHeight = targetHeight - margin * 2;
      const prepared = await PDFDocument.create();
      prepared.setTitle(`Trifoliar ajustado · ${sourceFile.name}`);
      prepared.setSubject('Documento ajustado a papel con márgenes simétricos para impresión dúplex');

      for (let index = 0; index < originalPages.length; index++) {
        processStatus.textContent = `Ajustando página ${index + 1} de ${originalPages.length}…`;
        const [embedded] = await prepared.embedPages([originalPages[index]]);
        const scale = Math.min(availableWidth / embedded.width, availableHeight / embedded.height);
        const drawWidth = embedded.width * scale;
        const drawHeight = embedded.height * scale;
        const outputPage = prepared.addPage([targetWidth, targetHeight]);
        outputPage.drawPage(embedded, {
          x: (targetWidth - drawWidth) / 2,
          y: (targetHeight - drawHeight) / 2,
          width: drawWidth,
          height: drawHeight
        });
      }

      const bytes = await prepared.save();
      outputBytes = bytes;
      if (outputUrl) URL.revokeObjectURL(outputUrl);
      outputUrl = URL.createObjectURL(new Blob([bytes], {type: 'application/pdf'}));
      printButton.disabled = true;
      preview.onload = () => {
        if (outputUrl && preview.src === outputUrl) printButton.disabled = false;
      };
      preview.src = outputUrl;
      const paperLabel = $('#paperSize').value === 'oficio' ? 'Oficio' : 'Carta';
      const orientationLabel = landscape ? 'horizontal' : 'vertical';
      const paperDimensions = $('#paperSize').value === 'oficio' ? '329 × 215.9 mm' : '279.4 × 215.9 mm';
      $('#resultInfo').textContent = `${originalPages.length} página${originalPages.length === 1 ? '' : 's'} · ${paperLabel} ${paperDimensions} · ${orientationLabel} · margen mínimo ${marginCm.toFixed(1)} cm · contenido centrado sin recorte.`;
      const safeBaseName = sourceFile.name.replace(/\.pdf$/i, '').replace(/[\\/:*?"<>|]+/g, '-').trim() || 'trifoliar';
      downloadLink.href = outputUrl;
      downloadLink.download = `${safeBaseName}_${paperLabel.toLowerCase()}_duplex.pdf`;
      wordButton.disabled = false;
      resultSection.hidden = false;
      processStatus.textContent = 'PDF preparado. Imprime a tamaño real para conservar los márgenes iguales.';
      resultSection.scrollIntoView({behavior: 'smooth', block: 'start'});
    } catch (error) {
      processStatus.textContent = error?.message?.includes('encrypted')
        ? 'Este PDF tiene contraseña o protección. Guarda una copia sin protección e inténtalo de nuevo.'
        : `No se pudo preparar el PDF: ${error?.message || 'formato no compatible.'}`;
    } finally {
      busy = false;
      prepareButton.disabled = !sourceFile;
    }
  }

  prepareButton.addEventListener('click', preparePdf);
  printButton.addEventListener('click', () => {
    if (!outputUrl) return;
    try {
      preview.contentWindow.focus();
      preview.contentWindow.print();
    } catch {
      window.open(outputUrl, '_blank', 'noopener');
    }
  });

  function makeDocumentXml(pageCount, widthEmu, heightEmu) {
    const orientationAttribute = widthEmu > heightEmu ? ' w:orient="landscape"' : '';
    const paragraphs = Array.from({length: pageCount}, (_, index) => {
      const imageId = `rId${index + 1}`;
      const breakBefore = index ? '<w:pageBreakBefore/>' : '';
      return `<w:p><w:pPr>${breakBefore}<w:spacing w:before="0" w:after="0" w:line="1" w:lineRule="exact"/></w:pPr><w:r><w:drawing><wp:anchor distT="0" distB="0" distL="0" distR="0" simplePos="0" relativeHeight="251658240" behindDoc="1" locked="0" layoutInCell="1" allowOverlap="1"><wp:simplePos x="0" y="0"/><wp:positionH relativeFrom="page"><wp:posOffset>0</wp:posOffset></wp:positionH><wp:positionV relativeFrom="page"><wp:posOffset>0</wp:posOffset></wp:positionV><wp:extent cx="${widthEmu}" cy="${heightEmu}"/><wp:effectExtent l="0" t="0" r="0" b="0"/><wp:wrapNone/><wp:docPr id="${index + 1}" name="Página ${index + 1}"/><wp:cNvGraphicFramePr><a:graphicFrameLocks noChangeAspect="1"/></wp:cNvGraphicFramePr><a:graphic><a:graphicData uri="http://schemas.openxmlformats.org/drawingml/2006/picture"><pic:pic><pic:nvPicPr><pic:cNvPr id="${index + 1}" name="Página ${index + 1}"/><pic:cNvPicPr/></pic:nvPicPr><pic:blipFill><a:blip r:embed="${imageId}"/><a:stretch><a:fillRect/></a:stretch></pic:blipFill><pic:spPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="${widthEmu}" cy="${heightEmu}"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom></pic:spPr></pic:pic></a:graphicData></a:graphic></wp:anchor></w:drawing></w:r></w:p>`;
    }).join('');
    return `<?xml version="1.0" encoding="UTF-8" standalone="yes"?><w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:wp="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing" xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:pic="http://schemas.openxmlformats.org/drawingml/2006/picture"><w:body>${paragraphs}<w:sectPr><w:pgSz w:w="${Math.round(widthEmu / 635)}" w:h="${Math.round(heightEmu / 635)}"${orientationAttribute}/><w:pgMar w:top="0" w:right="0" w:bottom="0" w:left="0" w:header="0" w:footer="0" w:gutter="0"/></w:sectPr></w:body></w:document>`;
  }

  async function prepareWord() {
    if (!outputBytes || makingWord) return;
    if (!window.pdfjsLib?.getDocument || !window.JSZip) {
      processStatus.textContent = 'No se cargaron los componentes para crear Word. Revisa la conexión e inténtalo de nuevo.';
      return;
    }
    makingWord = true;
    wordButton.disabled = true;
    processStatus.textContent = 'Preparando páginas para Word en alta resolución…';
    try {
      const PDFJS_VERSION = '3.11.174';
      window.pdfjsLib.GlobalWorkerOptions.workerSrc = `https://cdnjs.cloudflare.com/ajax/libs/pdf.js/${PDFJS_VERSION}/pdf.worker.min.js`;
      const pdf = await window.pdfjsLib.getDocument({data: outputBytes.slice()}).promise;
      const isOficio = $('#paperSize').value === 'oficio';
      const landscape = $('#paperOrientation').value === 'landscape' || ($('#paperOrientation').value === 'auto' && pdf.getPage(1).then(page => page.getViewport({scale: 1}).width > page.getViewport({scale: 1}).height));
      const isLandscape = typeof landscape === 'boolean' ? landscape : await landscape;
      const widthMm = isOficio ? (isLandscape ? 329 : 215.9) : (isLandscape ? 279.4 : 215.9);
      const heightMm = isOficio ? (isLandscape ? 215.9 : 329) : (isLandscape ? 215.9 : 279.4);
      const widthEmu = Math.round(widthMm / 25.4 * 914400);
      const heightEmu = Math.round(heightMm / 25.4 * 914400);
      const widthDxa = Math.round(widthEmu / 635);
      const heightDxa = Math.round(heightEmu / 635);
      const targetWidth = Math.round(widthMm / 25.4 * 300);
      const targetHeight = Math.round(heightMm / 25.4 * 300);
      const zip = new window.JSZip();
      const imageRels = [];

      for (let pageNumber = 1; pageNumber <= pdf.numPages; pageNumber++) {
        processStatus.textContent = `Convirtiendo página ${pageNumber} de ${pdf.numPages} para Word…`;
        const page = await pdf.getPage(pageNumber);
        const viewport = page.getViewport({scale: 300 / 72});
        const sourceCanvas = document.createElement('canvas');
        sourceCanvas.width = Math.ceil(viewport.width);
        sourceCanvas.height = Math.ceil(viewport.height);
        await page.render({canvasContext: sourceCanvas.getContext('2d', {alpha: false}), viewport}).promise;
        const pageCanvas = document.createElement('canvas');
        pageCanvas.width = targetWidth;
        pageCanvas.height = targetHeight;
        const context = pageCanvas.getContext('2d', {alpha: false});
        context.fillStyle = '#ffffff';
        context.fillRect(0, 0, targetWidth, targetHeight);
        const fit = Math.min(targetWidth / sourceCanvas.width, targetHeight / sourceCanvas.height);
        const imageWidth = sourceCanvas.width * fit;
        const imageHeight = sourceCanvas.height * fit;
        context.drawImage(sourceCanvas, (targetWidth - imageWidth) / 2, (targetHeight - imageHeight) / 2, imageWidth, imageHeight);
        const imageBlob = await new Promise((resolve, reject) => pageCanvas.toBlob(blob => blob ? resolve(blob) : reject(new Error('No se pudo convertir una página.')), 'image/png'));
        zip.file(`word/media/page${pageNumber}.png`, await imageBlob.arrayBuffer());
        imageRels.push(`<Relationship Id="rId${pageNumber}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/image" Target="media/page${pageNumber}.png"/>`);
        sourceCanvas.width = sourceCanvas.height = 0;
        pageCanvas.width = pageCanvas.height = 0;
      }

      zip.file('[Content_Types].xml', '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Default Extension="png" ContentType="image/png"/><Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/></Types>');
      zip.folder('_rels').file('.rels', '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/></Relationships>');
      zip.folder('word').file('document.xml', makeDocumentXml(pdf.numPages, widthEmu, heightEmu));
      zip.folder('word').folder('_rels').file('document.xml.rels', `<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">${imageRels.join('')}</Relationships>`);
      const docx = await zip.generateAsync({type: 'blob', mimeType: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document'});
      const wordUrl = URL.createObjectURL(docx);
      const link = document.createElement('a');
      const base = sourceFile?.name.replace(/\.pdf$/i, '').replace(/[\\/:*?"<>|]+/g, '-').trim() || 'trifoliar';
      link.href = wordUrl;
      link.download = `${base}_${isOficio ? 'oficio' : 'carta'}_word.docx`;
      document.body.append(link);
      link.click();
      link.remove();
      setTimeout(() => URL.revokeObjectURL(wordUrl), 60000);
      processStatus.textContent = 'Documento Word descargado. Ábrelo en Word y revisa la vista de impresión antes de imprimir.';
    } catch (error) {
      processStatus.textContent = `No se pudo preparar el documento de Word: ${error?.message || 'error al convertir las páginas.'}`;
    } finally {
      makingWord = false;
      wordButton.disabled = !outputBytes;
    }
  }
  wordButton.addEventListener('click', prepareWord);
  window.addEventListener('beforeunload', () => {
    if (outputUrl) URL.revokeObjectURL(outputUrl);
  });
  takeTransferredFiles();
})();
