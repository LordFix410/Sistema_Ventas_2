let segmenterPromise = null;
let birefnetPromise = null;
const imageWidth = image => image.naturalWidth || image.width;
const imageHeight = image => image.naturalHeight || image.height;

async function getBiRefNet() {
  if (!birefnetPromise) {
    birefnetPromise = (async () => {
      const hf = await import('https://cdn.jsdelivr.net/npm/@huggingface/transformers@4.3.1');
      const modelId = 'studioludens/birefnet-lite-512';
      let model;
      const devices = navigator.gpu ? ['webgpu', 'wasm'] : ['wasm'];
      let lastError;
      for (const device of devices) {
        try {
          model = await hf.AutoModel.from_pretrained(modelId, {dtype: 'fp16', device});
          break;
        } catch (error) { lastError = error; }
      }
      if (!model) {
        try { model = await hf.AutoModel.from_pretrained(modelId, {dtype: 'fp32', device: 'wasm'}); }
        catch (error) { lastError = error; }
      }
      if (!model) throw lastError || Error('No se pudo iniciar BiRefNet.');
      const processor = await hf.AutoProcessor.from_pretrained(modelId);
      return {hf, model, processor};
    })();
    birefnetPromise.catch(() => { birefnetPromise = null; });
  }
  return birefnetPromise;
}

async function segmentWithBiRefNet(image) {
  const {hf, model, processor} = await getBiRefNet();
  const inputCanvas = document.createElement('canvas');
  inputCanvas.width = imageWidth(image);
  inputCanvas.height = imageHeight(image);
  inputCanvas.getContext('2d').drawImage(image, 0, 0);
  const rawImage = hf.RawImage.fromCanvas(inputCanvas).rgb();
  const {pixel_values} = await processor(rawImage);
  const result = await model({input_image: pixel_values});
  let logits = result.logits || result.output_image || result[0];
  if (Array.isArray(logits)) [logits] = logits;
  if (!logits?.data || !logits?.dims) throw Error('BiRefNet no devolvió el mate alfa.');
  const maskWidth = logits.dims[logits.dims.length - 1];
  const maskHeight = logits.dims[logits.dims.length - 2];
  const pixelOffset = logits.data.length - maskWidth * maskHeight;
  const pixelCount = maskWidth * maskHeight;
  const alpha = new Float32Array(pixelCount);
  for (let index = 0; index < pixelCount; index++) {
    const value = logits.data[pixelOffset + index];
    alpha[index] = 1 / (1 + Math.exp(-value));
  }

  // Preserve enclosed foreground details such as eyeglass lenses and a phone
  // screen. Only fill small holes that cannot be reached from the image edge.
  const outside = new Uint8Array(pixelCount);
  const queue = new Int32Array(pixelCount);
  let head = 0, tail = 0;
  const addOutside = index => {
    if (!outside[index] && alpha[index] < 0.5) {
      outside[index] = 1;
      queue[tail++] = index;
    }
  };
  for (let x = 0; x < maskWidth; x++) {
    addOutside(x);
    addOutside((maskHeight - 1) * maskWidth + x);
  }
  for (let y = 1; y < maskHeight - 1; y++) {
    addOutside(y * maskWidth);
    addOutside(y * maskWidth + maskWidth - 1);
  }
  while (head < tail) {
    const index = queue[head++];
    const x = index % maskWidth, y = Math.floor(index / maskWidth);
    if (x > 0) addOutside(index - 1);
    if (x + 1 < maskWidth) addOutside(index + 1);
    if (y > 0) addOutside(index - maskWidth);
    if (y + 1 < maskHeight) addOutside(index + maskWidth);
  }
  const visited = new Uint8Array(pixelCount);
  const maxHoleSize = Math.floor(pixelCount * 0.03);
  for (let start = 0; start < pixelCount; start++) {
    if (outside[start] || visited[start] || alpha[start] >= 0.5) continue;
    head = 0;
    tail = 0;
    queue[tail++] = start;
    visited[start] = 1;
    while (head < tail) {
      const index = queue[head++];
      const x = index % maskWidth, y = Math.floor(index / maskWidth);
      const neighbors = [x > 0 ? index - 1 : -1, x + 1 < maskWidth ? index + 1 : -1, y > 0 ? index - maskWidth : -1, y + 1 < maskHeight ? index + maskWidth : -1];
      for (const neighbor of neighbors) {
        if (neighbor < 0 || outside[neighbor] || visited[neighbor] || alpha[neighbor] >= 0.5) continue;
        visited[neighbor] = 1;
        queue[tail++] = neighbor;
      }
    }
    if (tail <= maxHoleSize) {
      for (let i = 0; i < tail; i++) alpha[queue[i]] = 1;
    }
  }

  const maskCanvas = document.createElement('canvas');
  maskCanvas.width = maskWidth;
  maskCanvas.height = maskHeight;
  const maskContext = maskCanvas.getContext('2d');
  const maskPixels = maskContext.createImageData(maskWidth, maskHeight);
  for (let index = 0; index < pixelCount; index++) {
    const alphaByte = Math.round(alpha[index] * 255);
    const p = index * 4;
    maskPixels.data[p] = maskPixels.data[p + 1] = maskPixels.data[p + 2] = 255;
    maskPixels.data[p + 3] = alphaByte;
  }
  maskContext.putImageData(maskPixels, 0, 0);

  const subject = document.createElement('canvas');
  subject.width = imageWidth(image);
  subject.height = imageHeight(image);
  const subjectContext = subject.getContext('2d');
  subjectContext.drawImage(image, 0, 0);
  subjectContext.globalCompositeOperation = 'destination-in';
  subjectContext.drawImage(maskCanvas, 0, 0, subject.width, subject.height);
  subjectContext.globalCompositeOperation = 'source-over';
  return subject;
}

async function getPersonSegmenter() {
  if (!segmenterPromise) {
    segmenterPromise = (async () => {
      const vision = await import('https://cdn.jsdelivr.net/npm/@mediapipe/tasks-vision@0.10.22-rc.20250304/vision_bundle.mjs');
      const fileset = await vision.FilesetResolver.forVisionTasks('https://cdn.jsdelivr.net/npm/@mediapipe/tasks-vision@0.10.22-rc.20250304/wasm');
      return vision.ImageSegmenter.createFromOptions(fileset, {
        baseOptions: {modelAssetPath: 'https://storage.googleapis.com/mediapipe-models/image_segmenter/selfie_multiclass_256x256/float32/latest/selfie_multiclass_256x256.tflite'},
        runningMode: 'IMAGE',
        outputCategoryMask: true,
        outputConfidenceMasks: true
      });
    })();
    segmenterPromise.catch(() => { segmenterPromise = null; });
  }
  return segmenterPromise;
}


export async function removeProductBackground(image, onProgress = () => {}) {
  try {
    onProgress('Preparando el recorte de alta precisión…');
    return await segmentWithBiRefNet(image);
  } catch (error) {
    onProgress('Usando el modelo compatible para separar la persona…');
  }
  const segmenter = await getPersonSegmenter();
  onProgress('Afinando el contorno…');
  await new Promise(resolve => requestAnimationFrame(resolve));
    const result = segmenter.segment(image);
    const masks = result.confidenceMasks || [];
    let maskWidth;
    let maskHeight;
    let confidence;
    if (masks.length > 1) {
      maskWidth = masks[0].width;
      maskHeight = masks[0].height;
      const pixelCount = maskWidth * maskHeight;
      confidence = new Float32Array(pixelCount);
      const primary = new Float32Array(pixelCount);
      let accessories = null;
      // Classes 1-4 are hair, skin and clothes. Class 5 is accessories and is
      // only included close to the main subject, avoiding room objects.
      for (let classIndex = 1; classIndex < masks.length; classIndex++) {
        const values = masks[classIndex].getAsFloat32Array();
        const destination = masks.length >= 6 && classIndex === 5 ? (accessories = values) : primary;
        for (let index = 0; index < pixelCount; index++) destination[index] = Math.max(destination[index], values[index]);
        masks[classIndex].close();
      }
      masks[0].close();
      confidence.set(primary);
      if (accessories) {
        const radius = 5;
        for (let index = 0; index < pixelCount; index++) {
          if (accessories[index] < 0.18) continue;
          const x = index % maskWidth, y = Math.floor(index / maskWidth);
          let nearSubject = false;
          for (let dy = -radius; dy <= radius && !nearSubject; dy++) {
            const ny = y + dy;
            if (ny < 0 || ny >= maskHeight) continue;
            for (let dx = -radius; dx <= radius; dx++) {
              const nx = x + dx;
              if (nx >= 0 && nx < maskWidth && primary[ny * maskWidth + nx] >= 0.18) {
                nearSubject = true;
                break;
              }
            }
          }
          if (nearSubject) confidence[index] = Math.max(confidence[index], accessories[index]);
        }
      }
    } else if (result.categoryMask) {
      const categoryMask = result.categoryMask;
      maskWidth = categoryMask.width;
      maskHeight = categoryMask.height;
      confidence = Float32Array.from(categoryMask.getAsUint8Array(), value => value > 0 ? 1 : 0);
      categoryMask.close();
    } else {
      throw Error('El modelo no generó una máscara para esta foto.');
    }
    // Keep the dominant connected subject, but use a low cutoff here so
    // uncertain real edges (hands, cuffs and hair) remain attached to it.
    const pixelCount = confidence.length;
    const componentLabels = new Int32Array(pixelCount);
    const componentQueue = new Int32Array(pixelCount);
    let componentId = 0;
    let largestComponent = 0;
    let largestSize = 0;
    for (let start = 0; start < pixelCount; start++) {
      if (confidence[start] < 0.20 || componentLabels[start]) continue;
      componentId++;
      let head = 0, tail = 0;
      componentQueue[tail++] = start;
      componentLabels[start] = componentId;
      while (head < tail) {
        const current = componentQueue[head++];
        const x = current % maskWidth;
        const y = Math.floor(current / maskWidth);
        for (let dy = -1; dy <= 1; dy++) {
          const ny = y + dy;
          if (ny < 0 || ny >= maskHeight) continue;
          for (let dx = -1; dx <= 1; dx++) {
            if (!dx && !dy) continue;
            const nx = x + dx;
            if (nx < 0 || nx >= maskWidth) continue;
            const neighbor = ny * maskWidth + nx;
            if (confidence[neighbor] < 0.20 || componentLabels[neighbor]) continue;
            componentLabels[neighbor] = componentId;
            componentQueue[tail++] = neighbor;
          }
        }
      }
      if (tail > largestSize) {
        largestComponent = componentId;
        largestSize = tail;
      }
    }
    if (largestComponent) {
      for (let index = 0; index < pixelCount; index++) {
        if (componentLabels[index] !== largestComponent) confidence[index] = 0;
      }
    }
    const maskCanvas = document.createElement('canvas');
    maskCanvas.width = maskWidth;
    maskCanvas.height = maskHeight;
    const maskContext = maskCanvas.getContext('2d');
    const maskPixels = maskContext.createImageData(maskWidth, maskHeight);
    for (let i = 0; i < confidence.length; i++) {
      const value = Math.max(0, Math.min(1, (confidence[i] - 0.20) / 0.40));
      const alpha = Math.round(value * value * (3 - 2 * value) * 255);
      const p = i * 4;
      maskPixels.data[p] = maskPixels.data[p + 1] = maskPixels.data[p + 2] = 255;
      maskPixels.data[p + 3] = alpha;
    }
    maskContext.putImageData(maskPixels, 0, 0);

    const subject = document.createElement('canvas');
    subject.width = imageWidth(image);
    subject.height = imageHeight(image);
    const subjectContext = subject.getContext('2d');
    subjectContext.drawImage(image, 0, 0);
    subjectContext.globalCompositeOperation = 'destination-in';
    subjectContext.drawImage(maskCanvas, 0, 0, subject.width, subject.height);
    subjectContext.globalCompositeOperation = 'source-over';


  return subject;
}
