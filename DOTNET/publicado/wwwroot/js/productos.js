(() => {
  const $ = (selector, root = document) => root.querySelector(selector);
  const api = '/api/productos';
  const loginPanel = $('#loginPanel'), productApp = $('#productApp');
  let stream = null, detector = null, detected = new Set(), adminData = null;
  let catalogWarnings = [];
  const productDraftKey = 'ld-product-draft-v1';
  let navigationApproved = false;
  const managerConfig = {
    categoryManageForm: { endpoint: 'categorias', list: 'categorias', rows: 'categoryRows', select: 'categoria', fields: ['nombre'] },
    productGroupManageForm: { endpoint: 'grupos-reposicion', list: 'gruposProducto', rows: 'productGroupRows', select: 'replenishmentGroup', fields: ['nombre', 'cantidadMinima', 'cantidadMaxima'] },
    supplierGroupManageForm: { endpoint: 'grupos-proveedor', list: 'gruposProveedor', rows: 'supplierGroupRows', select: 'supplierGroup', fields: ['nombre', 'proveedores'] }
  };
  async function request(path, options = {}) {
    const response = await fetch(`${api}${path}`, { credentials: 'same-origin', headers: { 'Content-Type': 'application/json', ...(options.headers || {}) }, ...options });
    const body = response.status === 204 ? null : await response.json().catch(() => ({}));
    if (!response.ok) {
      const method = (options.method || 'GET').toUpperCase();
      const fallback = response.status === 404
        ? `No existe ${method} ${api}${path} en el servidor publicado. Actualiza/reinicia el servidor web para cargar la versión que incluye esta ruta.`
        : `Error ${response.status}`;
      const error = new Error(body?.mensaje || fallback); error.status = response.status; throw error;
    }
    return body;
  }
  function message(element, text, success = false) { if (!element) return; element.textContent = text || ''; element.classList.toggle('success', success); }
  function showSignedIn() { loginPanel.hidden = true; productApp.hidden = false; $('#logoutButton').hidden = false; }
  function selectOptions(select, items, emptyLabel) { select.replaceChildren(new Option(emptyLabel, '')); for (const item of items) select.add(new Option(item.nombre, item.id)); }
  async function refreshCatalogs() {
    const data = await request('/catalogos');
    selectOptions($('#categoria'), data.categorias, 'Elegí una categoría');
    selectOptions($('#supplierGroup'), data.gruposProveedor, 'Sin grupo');
    $('#replenishmentGroup').replaceChildren(new Option('Límites propios del producto', ''));
    for (const group of data.gruposReposicion) $('#replenishmentGroup').add(new Option(`${group.nombre} · mín. ${group.cantidadMinima} / máx. ${group.cantidadMaxima}`, group.id));
    const warnings = [...(data.avisos || [])];
    if (!data.proveedores.length) warnings.push('No hay proveedores activos en dbo.PROVEEDOR.');
    catalogWarnings = warnings;
    try { await refreshAdministration(); } catch (error) { warnings.push(`No se pudieron cargar los catálogos de edición: ${error.message}`); }
    restoreProductDraft();
    const summary = `Categorías: ${data.categorias.length} · Grupos: ${data.gruposReposicion.length} · Grupos de proveedores: ${data.gruposProveedor.length} · Proveedores: ${data.proveedores.length}`;
    message($('#catalogMessage'), warnings.length ? `${summary}. ${warnings.join(' ')}` : summary, warnings.length === 0);
  }
  $('#addCategoryButton').addEventListener('click', () => {
    $('#quickCategoryForm').reset();
    message($('#quickCategoryMessage'), '');
    $('#categoryDialog').showModal();
    $('#quickCategoryName').focus();
  });
  function closeCategoryDialog() { if ($('#categoryDialog').open) $('#categoryDialog').close(); }
  $('#closeCategoryDialog').addEventListener('click', closeCategoryDialog);
  $('#cancelCategoryDialog').addEventListener('click', closeCategoryDialog);
  $('#quickCategoryForm').addEventListener('submit', async event => {
    event.preventDefault();
    const form = event.currentTarget;
    const name = $('#quickCategoryName').value.trim().toLocaleUpperCase('es-GT');
    const active = $('#quickCategoryState').value === 'true';
    const button = form.querySelector('[type="submit"]');
    if (!name) { message($('#quickCategoryMessage'), 'Escribe la descripción de la categoría.'); $('#quickCategoryName').focus(); return; }
    button.disabled = true;
    message($('#quickCategoryMessage'), 'Guardando categoría…');
    try {
      const category = await request('/categorias', { method: 'POST', body: JSON.stringify({ nombre: name, estado: active }) });
      if (category.activo) {
        const option = new Option(category.nombre, category.id, true, true);
        $('#categoria').add(option);
        $('#categoria').value = String(category.id);
        persistProductDraft();
      }
      closeCategoryDialog();
      form.reset();
      message($('#productMessage'), category.activo
        ? `Categoría ${category.nombre} creada y seleccionada.`
        : `Categoría ${category.nombre} creada como inactiva. Actívala desde Categorías para poder usarla.`, true);
      $('#addCategoryButton').focus();
    } catch (error) {
      if (error.status === 401) { location.reload(); return; }
      message($('#quickCategoryMessage'), error.message);
      $('#quickCategoryName').focus();
    } finally { button.disabled = false; }
  });
  async function refreshAdministration() {
    adminData = await request('/administracion');
    const supplierMap = new Map(adminData.proveedores.map(item => [item.id, item.nombre]));
    const render = (config, items) => {
      const tbody = $(`#${config.rows}`); tbody.replaceChildren();
      for (const item of items) {
        const row = document.createElement('tr');
        const name = document.createElement('td'); name.textContent = item.nombre; name.dataset.label = 'Nombre';
        const active = document.createElement('td'); active.dataset.label = 'Estado'; active.append(statusBadge(item.activo));
        if (config.endpoint === 'grupos-proveedor') { const suppliers = document.createElement('td'); suppliers.dataset.label = 'Proveedores'; suppliers.textContent = item.proveedores.map(id => supplierMap.get(id)).filter(Boolean).join(', ') || 'Sin proveedores'; row.append(name, suppliers, active); }
        else if (config.endpoint === 'grupos-reposicion') { const minimum = document.createElement('td'); minimum.dataset.label = 'Mínimo'; minimum.textContent = item.cantidadMinima; const maximum = document.createElement('td'); maximum.dataset.label = 'Máximo'; maximum.textContent = item.cantidadMaxima; row.append(name, minimum, maximum, active); }
        else row.append(name, active);
        const actions = document.createElement('td'); actions.className = 'row-actions'; actions.dataset.label = 'Acción';
        const edit = document.createElement('button'); edit.type = 'button'; edit.className = 'button button-quiet button-small'; edit.textContent = 'Editar'; edit.dataset.action = 'edit'; edit.dataset.endpoint = config.endpoint; edit.dataset.id = item.id;
        actions.append(edit);
        row.append(actions); tbody.append(row);
      }
      const select = $(`#${config.select}`);
      const selected = select.value;
      if (config.endpoint === 'grupos-reposicion') { select.replaceChildren(new Option('Límites propios del producto', '')); for (const item of items.filter(group => group.activo)) select.add(new Option(`${item.nombre} · mín. ${item.cantidadMinima} / máx. ${item.cantidadMaxima}`, item.id)); }
      else selectOptions(select, items.filter(item => item.activo), config.endpoint === 'categorias' ? 'Elegí una categoría' : 'Sin grupo');
      if ([...select.options].some(option => option.value === selected)) select.value = selected;
    };
    render(managerConfig.categoryManageForm, adminData.categorias);
    render(managerConfig.productGroupManageForm, adminData.gruposProducto);
    render(managerConfig.supplierGroupManageForm, adminData.gruposProveedor);
    renderSupplierChoices();
    if (adminData.avisos?.length) catalogWarnings.push(...adminData.avisos);
    const summary = `Categorías: ${adminData.categorias.length} · Grupos: ${adminData.gruposProducto.length} · Grupos de proveedores: ${adminData.gruposProveedor.length} · Proveedores: ${adminData.proveedores.length}`;
    message($('#catalogMessage'), catalogWarnings.length ? `${summary}. ${[...new Set(catalogWarnings)].join(' ')}` : summary, catalogWarnings.length === 0);
  }
  function statusBadge(active) { const badge = document.createElement('span'); badge.className = `status-badge ${active ? 'is-active' : 'is-inactive'}`; badge.textContent = active ? 'Activo' : 'Inactivo'; return badge; }
  function renderSupplierChoices(selected = []) {
    const choices = $('#supplierChoices'); if (!choices || !adminData) return;
    choices.replaceChildren();
    for (const supplier of adminData.proveedores) {
      const label = document.createElement('label'); const box = document.createElement('input');
      box.type = 'checkbox'; box.name = 'proveedores'; box.value = supplier.id; box.checked = selected.includes(supplier.id);
      label.append(box, document.createTextNode(supplier.nombre)); choices.append(label);
    }
    if (!adminData.proveedores.length) choices.textContent = 'No hay proveedores activos registrados.';
  }
  async function start() {
    try { const status = await request('/sesion'); if (status.autenticado) { showSignedIn(); message($('#catalogMessage'), 'Cargando catálogos…'); await refreshCatalogs(); } }
    catch (error) { if (productApp.hidden) message($('#loginMessage'), error.message); else message($('#catalogMessage'), error.message); }
  }
  $('#loginForm').addEventListener('submit', async event => {
    event.preventDefault(); const form = new FormData(event.currentTarget); const button = event.currentTarget.querySelector('button'); button.disabled = true;
    message($('#loginMessage'), 'Verificando contraseña…');
    try { await request('/sesion', { method: 'POST', body: JSON.stringify({ clave: form.get('clave') }) }); showSignedIn(); message($('#catalogMessage'), 'Cargando catálogos…'); await refreshCatalogs(); }
    catch (error) { if (productApp.hidden) message($('#loginMessage'), error.message); else message($('#catalogMessage'), error.message); }
    finally { button.disabled = false; }
  });
  $('#logoutButton').addEventListener('click', async () => { if (hasProductDraft() && !confirm('Hay un producto sin guardar. El borrador quedó guardado en esta pestaña. ¿Quieres cerrar sesión?')) return; await stopScanner(); await request('/sesion', { method: 'DELETE' }); navigationApproved = true; location.reload(); });
  document.querySelectorAll('.product-tab').forEach(button => button.addEventListener('click', () => {
    if (button.dataset.view !== 'viewProduct' && !$('#scannerPanel').hidden) stopScanner();
    document.querySelectorAll('.product-tab').forEach(tab => tab.classList.toggle('active', tab === button));
    document.querySelectorAll('.workspace-view').forEach(view => { view.hidden = view.id !== button.dataset.view; });
    if (button.dataset.view !== 'viewProduct' && adminData) refreshAdministration().catch(error => message($('#catalogMessage'), `No se pudieron actualizar las listas: ${error.message}`));
  }));
  $('#replenishmentGroup').addEventListener('change', event => { const useGroup = !!event.target.value; $('#individualLimits').hidden = useGroup; $('#individualLimits').querySelectorAll('input').forEach(input => { input.disabled = useGroup; if (useGroup) input.value = ''; }); });
  const purchaseInput = $('#precioCompra'), saleInput = $('#precioVenta'), percentInput = $('#porcentaje');
  const productForm = $('#productForm');
  const productPhoto = $('#productPhoto');
  const productPhotoPreview = $('#productPhotoPreview');
  const productPhotoControls = $('#productPhotoControls');
  const productPhotoStatus = $('#productPhotoStatus');
  const startProductBackgroundButton = $('#startProductBackgroundRemoval');
  const reviewProductPhotoButton = $('#reviewProductPhoto');
  const productPhotoDialog = $('#productPhotoEditorDialog');
  const productPhotoCanvas = $('#productPhotoCanvas');
  const productPhotoContext = productPhotoCanvas.getContext('2d');
  const productPhotoZoom = $('#productPhotoZoom');
  const productPhotoEditorMessage = $('#productPhotoEditorMessage');
  const productPhotoProcessing = $('#productPhotoProcessing');
  const productPhotoProcessingMessage = $('#productPhotoProcessingMessage');
  const compareProductBackgroundButton = $('#compareProductBackground');
  const closeProductPhotoEditorButton = $('#closeProductPhotoEditor');
  const cancelProductPhotoButton = $('#cancelProductPhoto');
  const rotateProductPhotoButton = $('#rotateProductPhoto');
  const acceptProductPhotoButton = $('#acceptProductPhoto');
  let acceptedProductPhoto = null;
  let originalProductPhoto = null;
  let processedProductPhoto = null;
  let showProcessedProductPhoto = false;
  let productPhotoMime = 'image/jpeg';
  let productPhotoSourceName = 'foto-producto';
  let productPhotoZoomValue = 1;
  let productPhotoPanX = 0, productPhotoPanY = 0;
  let productPhotoRotation = 0;
  const productPhotoPointers = new Map();
  let productPhotoGesture = null;
  let productPhotoEditGeneration = 0;
  let productPhotoProcessingActive = false;
  let productPhotoPreviewUrl = '';
  let productPhotoNeedsReview = false;
  let productPhotoEditorSnapshot = null;
  const minimumInput = $('input[name="cantidadMinima"]', productForm);
  const maximumInput = $('input[name="cantidadMaxima"]', productForm);
  const submitProduct = productForm.querySelector('[type="submit"]');
  let autoPurchaseCost = true;
  let pendingPriceField = null;

  function productFormHasData() {
    return !!($('#codigo').value.trim() || $('#nombre').value.trim() || $('input[name="descripcion"]', productForm).value.trim() ||
      $('#categoria').value || $('#supplierGroup').value || $('#replenishmentGroup').value ||
      (Number(purchaseInput.value) || 0) > 0 || (Number(saleInput.value) || 0) > 0 ||
      (Number(percentInput.value) || 0) !== 61 || minimumInput.value !== '' || maximumInput.value !== '');
  }
  function hasProductDraft() { return !!sessionStorage.getItem(productDraftKey); }
  function persistProductDraft() {
    if (!productFormHasData()) { sessionStorage.removeItem(productDraftKey); return; }
    const values = [...productForm.elements].filter(element => element.name && element.type !== 'submit' && element.type !== 'reset')
      .map(element => ({ name: element.name, type: element.type, value: element.value, checked: element.type === 'checkbox' ? element.checked : undefined }));
    sessionStorage.setItem(productDraftKey, JSON.stringify({ campos: values, costoAutomatico: autoPurchaseCost }));
  }
  function restoreProductDraft() {
    const saved = sessionStorage.getItem(productDraftKey);
    if (!saved) return;
    try {
      const draft = JSON.parse(saved);
      const values = Array.isArray(draft) ? draft : draft.campos;
      if (!Array.isArray(values)) throw new Error('Borrador inválido');
      for (const item of values) {
        const element = [...productForm.elements].find(candidate => candidate.name === item.name && candidate.type === item.type);
        if (!element) continue;
        if (item.type === 'checkbox') element.checked = !!item.checked;
        else if (element.tagName !== 'SELECT' || [...element.options].some(option => option.value === item.value)) element.value = item.value;
      }
      autoPurchaseCost = typeof draft.costoAutomatico === 'boolean' ? draft.costoAutomatico : (Number(purchaseInput.value) || 0) <= 0;
      $('#replenishmentGroup').dispatchEvent(new Event('change'));
      refreshPriceHint();
      message($('#productMessage'), 'Borrador del producto recuperado. Puedes continuar donde lo dejaste.', true);
    } catch { sessionStorage.removeItem(productDraftKey); }
  }
  function displayAcceptedProductPhoto(file) {
    acceptedProductPhoto = file;
    setProductPhotoPreview(file);
  }
  function setProductPhotoPreview(source) {
    if (productPhotoPreviewUrl) URL.revokeObjectURL(productPhotoPreviewUrl);
    productPhotoPreviewUrl = source ? URL.createObjectURL(source) : '';
    productPhotoPreview.src = productPhotoPreviewUrl;
    productPhotoPreview.hidden = !source;
    productPhotoControls.hidden = !source;
    startProductBackgroundButton.disabled = !originalProductPhoto || productPhotoProcessingActive || !!processedProductPhoto;
    reviewProductPhotoButton.disabled = !originalProductPhoto || productPhotoProcessingActive;
  }
  async function setPreviewFromDrawable(drawable, generation) {
    const canvas = document.createElement('canvas');
    canvas.width = drawableWidth(drawable);
    canvas.height = drawableHeight(drawable);
    canvas.getContext('2d').drawImage(drawable, 0, 0);
    const blob = await new Promise((resolve, reject) => canvas.toBlob(value => value ? resolve(value) : reject(new Error('No se pudo preparar la vista previa.')), 'image/png'));
    if (generation !== productPhotoEditGeneration) return;
    setProductPhotoPreview(blob);
  }
  const drawableWidth = image => image?.naturalWidth || image?.width || 0;
  const drawableHeight = image => image?.naturalHeight || image?.height || 0;
  function currentProductPhotoDrawable() {
    return showProcessedProductPhoto && processedProductPhoto ? processedProductPhoto : originalProductPhoto;
  }
  function drawProductPhotoEditor() {
    if (!originalProductPhoto) return;
    const source = currentProductPhotoDrawable();
    const width = drawableWidth(source), height = drawableHeight(source), side = productPhotoCanvas.width;
    const scale = Math.max(side / width, side / height) * productPhotoZoomValue;
    const drawWidth = width * scale, drawHeight = height * scale;
    productPhotoContext.clearRect(0, 0, side, side);
    productPhotoContext.save();
    productPhotoContext.translate(side / 2 + productPhotoPanX, side / 2 + productPhotoPanY);
    productPhotoContext.rotate(productPhotoRotation);
    productPhotoContext.drawImage(source, -drawWidth / 2, -drawHeight / 2, drawWidth, drawHeight);
    productPhotoContext.restore();
  }
  function setProductPhotoEditorMessage(text, success = false) {
    productPhotoEditorMessage.textContent = text || '';
    productPhotoEditorMessage.classList.toggle('success', success);
  }
  function setProductPhotoProcessing(active, text = 'Preparando el recorte…') {
    productPhotoProcessingActive = active;
    productPhotoProcessing.hidden = !active;
    productPhotoProcessingMessage.textContent = text;
    startProductBackgroundButton.disabled = active || !originalProductPhoto || !!processedProductPhoto;
    reviewProductPhotoButton.disabled = active || !originalProductPhoto;
  }
  function clearProductPhotoEditor() {
    productPhotoEditGeneration++;
    productPhotoProcessingActive = false;
    productPhotoProcessing.hidden = true;
    productPhotoEditorSnapshot = null;
    originalProductPhoto = null;
    processedProductPhoto = null;
    showProcessedProductPhoto = false;
    productPhotoPointers.clear();
    productPhotoGesture = null;
  }
  function cancelProductPhotoEdit() {
    if (productPhotoEditorSnapshot) {
      ({originalProductPhoto, processedProductPhoto, showProcessedProductPhoto, productPhotoZoomValue, productPhotoPanX, productPhotoPanY, productPhotoRotation} = productPhotoEditorSnapshot);
      productPhotoZoom.value = String(productPhotoZoomValue);
      productPhotoEditorSnapshot = null;
      compareProductBackgroundButton.hidden = !processedProductPhoto;
      drawProductPhotoEditor();
    }
    if (productPhotoDialog.open) productPhotoDialog.close();
  }
  function openProductPhotoEditor() {
    if (!originalProductPhoto || productPhotoProcessingActive) return;
    productPhotoEditorSnapshot = {originalProductPhoto, processedProductPhoto, showProcessedProductPhoto, productPhotoZoomValue, productPhotoPanX, productPhotoPanY, productPhotoRotation};
    productPhotoZoomValue = 1;
    productPhotoPanX = productPhotoPanY = 0;
    productPhotoZoom.value = '1';
    compareProductBackgroundButton.hidden = !processedProductPhoto;
    compareProductBackgroundButton.textContent = showProcessedProductPhoto ? 'Ver original' : 'Ver fondo quitado';
    setProductPhotoEditorMessage('Arrastrá la foto para ajustar su posición. Podés girarla y comparar el fondo.');
    drawProductPhotoEditor();
    if (!productPhotoDialog.open) productPhotoDialog.showModal();
  }
  function loadProductPhoto(file) {
    clearProductPhotoEditor();
    productPhotoMime = file.type;
    productPhotoSourceName = file.name.replace(/\.[^.]+$/, '') || 'foto-producto';
    productPhotoNeedsReview = false;
    productPhotoZoomValue = 1;
    productPhotoPanX = productPhotoPanY = 0;
    productPhotoRotation = 0;
    productPhotoZoom.value = '1';
    compareProductBackgroundButton.hidden = true;
    compareProductBackgroundButton.textContent = 'Ver original';
    if (productPhotoDialog.open) productPhotoDialog.close();
    acceptedProductPhoto = file;
    setProductPhotoPreview(file);
    productPhotoStatus.textContent = 'Foto lista. Ahora podés escanear el código y completar los datos.';
    productPhotoStatus.classList.remove('success');
    const generation = productPhotoEditGeneration;
    const url = URL.createObjectURL(file);
    const image = new Image();
    image.onload = () => {
      URL.revokeObjectURL(url);
      if (generation !== productPhotoEditGeneration) return;
      originalProductPhoto = image;
      setProductPhotoProcessing(false);
      productPhotoStatus.textContent = 'Foto cargada. Si querés, quita el fondo ahora; podés seguir llenando el formulario mientras se procesa.';
      startProductBackgroundButton.disabled = false;
      reviewProductPhotoButton.disabled = false;
      drawProductPhotoEditor();
    };
    image.onerror = () => {
      URL.revokeObjectURL(url);
      if (generation === productPhotoEditGeneration) setProductPhotoEditorMessage('No se pudo abrir esta foto. Elegí otra imagen.');
    };
    image.src = url;
  }

  startProductBackgroundButton.addEventListener('click', async () => {
    if (!originalProductPhoto || processedProductPhoto) return;
    const generation = productPhotoEditGeneration;
    productPhotoStatus.textContent = 'Quitando el fondo en segundo plano. Podés seguir completando el producto.';
    productPhotoStatus.classList.remove('success');
    setProductPhotoProcessing(true, 'Iniciando el modelo de recorte…');
    try {
      const {removeProductBackground} = await import('./background-removal.js?v=2');
      const cutout = await removeProductBackground(originalProductPhoto, text => {
        if (generation === productPhotoEditGeneration) productPhotoProcessingMessage.textContent = text;
      });
      if (generation !== productPhotoEditGeneration) return;
      processedProductPhoto = cutout;
      showProcessedProductPhoto = true;
      productPhotoNeedsReview = true;
      compareProductBackgroundButton.hidden = false;
      compareProductBackgroundButton.textContent = 'Ver original';
      startProductBackgroundButton.textContent = 'Fondo quitado';
      productPhotoStatus.textContent = 'Fondo quitado. Cuando termines los datos, toca “Revisar y ajustar” para centrar, girar y aceptar la foto.';
      productPhotoStatus.classList.add('success');
      reviewProductPhotoButton.disabled = false;
      await setPreviewFromDrawable(processedProductPhoto, generation);
      drawProductPhotoEditor();
    } catch (error) {
      if (generation === productPhotoEditGeneration) {
        productPhotoStatus.textContent = error.message || 'No se pudo quitar el fondo. Revisá tu conexión e intentá de nuevo.';
        productPhotoStatus.classList.remove('success');
      }
    } finally {
      if (generation === productPhotoEditGeneration) setProductPhotoProcessing(false);
    }
  });
  compareProductBackgroundButton.addEventListener('click', () => {
    if (!processedProductPhoto) return;
    showProcessedProductPhoto = !showProcessedProductPhoto;
    compareProductBackgroundButton.textContent = showProcessedProductPhoto ? 'Ver original' : 'Ver fondo quitado';
    setProductPhotoEditorMessage(showProcessedProductPhoto ? 'Vista sin fondo. El original sigue guardado para comparar.' : 'Vista original. El recorte sin fondo se conserva.', true);
    drawProductPhotoEditor();
  });
  rotateProductPhotoButton.addEventListener('click', () => {
    if (!originalProductPhoto) return;
    productPhotoRotation += Math.PI / 2;
    setProductPhotoEditorMessage('Foto girada 90°. También podés girarla libremente con dos dedos.', true);
    drawProductPhotoEditor();
  });
  productPhotoZoom.addEventListener('input', () => {
    productPhotoZoomValue = Number(productPhotoZoom.value) || 1;
    drawProductPhotoEditor();
  });
  const pointerPair = () => [...productPhotoPointers.values()].slice(0, 2);
  function beginProductPhotoGesture() {
    const [first, second] = pointerPair();
    if (!first || !second) { productPhotoGesture = null; return; }
    productPhotoGesture = {
      distance: Math.hypot(second.x - first.x, second.y - first.y),
      angle: Math.atan2(second.y - first.y, second.x - first.x),
      centerX: (first.x + second.x) / 2,
      centerY: (first.y + second.y) / 2,
      zoom: productPhotoZoomValue,
      rotation: productPhotoRotation,
      panX: productPhotoPanX,
      panY: productPhotoPanY
    };
  }
  productPhotoCanvas.addEventListener('pointerdown', event => {
    if (!originalProductPhoto) return;
    productPhotoCanvas.setPointerCapture(event.pointerId);
    productPhotoPointers.set(event.pointerId, {x: event.clientX, y: event.clientY});
    if (productPhotoPointers.size >= 2) beginProductPhotoGesture();
  });
  productPhotoCanvas.addEventListener('pointermove', event => {
    const previous = productPhotoPointers.get(event.pointerId);
    if (!previous || !productPhotoCanvas.clientWidth) return;
    productPhotoPointers.set(event.pointerId, {x: event.clientX, y: event.clientY});
    const factor = productPhotoCanvas.width / productPhotoCanvas.clientWidth;
    if (productPhotoPointers.size >= 2 && productPhotoGesture) {
      const [first, second] = pointerPair();
      const distance = Math.hypot(second.x - first.x, second.y - first.y);
      const angle = Math.atan2(second.y - first.y, second.x - first.x);
      const centerX = (first.x + second.x) / 2, centerY = (first.y + second.y) / 2;
      let angleDelta = angle - productPhotoGesture.angle;
      while (angleDelta > Math.PI) angleDelta -= Math.PI * 2;
      while (angleDelta < -Math.PI) angleDelta += Math.PI * 2;
      productPhotoZoomValue = Math.max(1, Math.min(3, productPhotoGesture.zoom * distance / Math.max(1, productPhotoGesture.distance)));
      productPhotoZoom.value = String(productPhotoZoomValue);
      productPhotoRotation = productPhotoGesture.rotation + angleDelta;
      productPhotoPanX = productPhotoGesture.panX + (centerX - productPhotoGesture.centerX) * factor;
      productPhotoPanY = productPhotoGesture.panY + (centerY - productPhotoGesture.centerY) * factor;
    } else {
      productPhotoPanX += (event.clientX - previous.x) * factor;
      productPhotoPanY += (event.clientY - previous.y) * factor;
    }
    drawProductPhotoEditor();
  });
  function endProductPhotoGesture(event) {
    productPhotoPointers.delete(event.pointerId);
    productPhotoGesture = null;
    if (productPhotoPointers.size >= 2) beginProductPhotoGesture();
  }
  productPhotoCanvas.addEventListener('pointerup', endProductPhotoGesture);
  productPhotoCanvas.addEventListener('pointercancel', endProductPhotoGesture);
  async function acceptProductPhotoEdit() {
    if (!originalProductPhoto) return;
    const generation = productPhotoEditGeneration;
    const selectedIsCutout = showProcessedProductPhoto && !!processedProductPhoto;
    const outputType = selectedIsCutout || productPhotoMime === 'image/png' ? 'image/png' : 'image/jpeg';
    const acceptButton = $('#acceptProductPhoto');
    acceptButton.disabled = true;
    acceptButton.textContent = 'Preparando…';
    try {
      let blob = null;
      let acceptedCanvas = null;
      for (let side = 720; side >= 360; side -= 90) {
        const output = document.createElement('canvas');
        output.width = output.height = side;
        const context = output.getContext('2d');
        if (outputType === 'image/jpeg') { context.fillStyle = '#fff'; context.fillRect(0, 0, side, side); }
        context.drawImage(productPhotoCanvas, 0, 0, side, side);
        blob = await new Promise((resolve, reject) => output.toBlob(value => value ? resolve(value) : reject(new Error('No se pudo preparar la foto.')), outputType, .9));
        acceptedCanvas = output;
        if (blob.size <= 5 * 1024 * 1024) break;
      }
      if (!blob || blob.size > 5 * 1024 * 1024) throw new Error('La foto editada supera 5 MB. Reduce el tamaño del archivo original.');
      if (generation !== productPhotoEditGeneration) return;
      const extension = outputType === 'image/png' ? 'png' : 'jpg';
      const filename = `${productPhotoSourceName}.${extension}`.replace(/[^\w.-]/g, '_');
      displayAcceptedProductPhoto(new File([blob], filename, {type: outputType}));
      productPhotoMime = outputType;
      originalProductPhoto = acceptedCanvas;
      processedProductPhoto = null;
      showProcessedProductPhoto = false;
      productPhotoZoomValue = 1;
      productPhotoPanX = productPhotoPanY = 0;
      productPhotoRotation = 0;
      productPhotoNeedsReview = false;
      productPhotoEditorSnapshot = null;
      compareProductBackgroundButton.hidden = true;
      startProductBackgroundButton.textContent = 'Quitar fondo';
      productPhotoStatus.textContent = 'Foto revisada y lista. Se guardará junto con el producto.';
      productPhotoStatus.classList.add('success');
      productPhotoDialog.close();
      message($('#productMessage'), 'Foto preparada. Se guardará junto con el producto.', true);
    } catch (error) {
      setProductPhotoEditorMessage(error.message || 'No se pudo aceptar esta foto.');
    } finally {
      acceptButton.disabled = false;
      acceptButton.textContent = 'Aceptar foto';
    }
  }
  $('#acceptProductPhoto').addEventListener('click', acceptProductPhotoEdit);
  reviewProductPhotoButton.addEventListener('click', openProductPhotoEditor);
  $('#cancelProductPhoto').addEventListener('click', cancelProductPhotoEdit);
  $('#closeProductPhotoEditor').addEventListener('click', cancelProductPhotoEdit);
  productPhotoDialog.addEventListener('cancel', event => {
    event.preventDefault();
    cancelProductPhotoEdit();
  });
  productForm.addEventListener('input', persistProductDraft);
  productForm.addEventListener('change', persistProductDraft);
  productForm.addEventListener('reset', () => setTimeout(() => {
    sessionStorage.removeItem(productDraftKey);
    cancelProductPhotoEdit();
    clearProductPhotoEditor();
    if (productPhotoPreviewUrl) URL.revokeObjectURL(productPhotoPreviewUrl);
    productPhotoPreviewUrl = '';
    acceptedProductPhoto = null;
    productPhotoNeedsReview = false;
    processedProductPhoto = null;
    originalProductPhoto = null;
    showProcessedProductPhoto = false;
    productPhotoProcessingActive = false;
    productPhotoProcessing.hidden = true;
    startProductBackgroundButton.textContent = 'Quitar fondo';
    productPhotoStatus.textContent = '';
    productPhoto.value = '';
    productPhotoPreview.hidden = true;
    productPhotoPreview.removeAttribute('src');
    productPhotoControls.hidden = true;
  }, 0));
  productPhoto.addEventListener('change', () => {
    const file = productPhoto.files?.[0];
    productPhoto.value = '';
    if (!file) return;
    if (!file.type.startsWith('image/') || file.size > 15 * 1024 * 1024) {
      message($('#productMessage'), 'Selecciona una foto compatible de hasta 15 MB.');
      return;
    }
    loadProductPhoto(file);
  });
  $('#removeProductPhoto').addEventListener('click', () => {
    clearProductPhotoEditor();
    displayAcceptedProductPhoto(null);
    productPhotoNeedsReview = false;
    productPhotoProcessingActive = false;
    productPhotoProcessing.hidden = true;
    startProductBackgroundButton.textContent = 'Quitar fondo';
    productPhotoStatus.textContent = '';
    persistProductDraft();
  });
  window.addEventListener('beforeunload', event => {
    if (navigationApproved || !hasProductDraft()) return;
    event.preventDefault(); event.returnValue = '';
  });
  document.addEventListener('click', event => {
    const link = event.target.closest('a[href]');
    if (!link || !hasProductDraft()) return;
    const destination = new URL(link.href, location.href);
    if (destination.href === location.href) return;
    if (confirm('Hay un producto sin guardar. El borrador quedó guardado en esta pestaña. ¿Quieres salir de la pantalla?')) { navigationApproved = true; return; }
    event.preventDefault();
  });

  // Enter funciona como "Siguiente" en teclado físico y en teclados móviles.
  const enterNext = new Map([
    [$('#codigo'), $('#nombre')],
    [$('#nombre'), $('input[name="descripcion"]', productForm)],
    [$('input[name="descripcion"]', productForm), $('#categoria')],
    [$('#categoria'), $('#supplierGroup')],
    [$('#supplierGroup'), saleInput],
    [saleInput, $('#replenishmentGroup')],
    [$('#replenishmentGroup'), minimumInput],
    [minimumInput, maximumInput],
    [maximumInput, submitProduct],
    [purchaseInput, percentInput],
    [percentInput, saleInput]
  ]);
  productForm.addEventListener('keydown', event => {
    if (event.key !== 'Enter' || event.isComposing || event.target === submitProduct) return;
    const next = enterNext.get(event.target);
    if (!next) return;
    event.preventDefault();
    if ([purchaseInput, percentInput, saleInput].includes(event.target)) commitPriceField(event.target);
    if (event.target === $('#replenishmentGroup') && event.target.value) {
      submitProduct.focus();
      return;
    }
    next.focus();
    if (next instanceof HTMLInputElement && next.type === 'number') next.select();
  });
  function refreshPriceHint(text) {
    const hint = $('#priceHint');
    saleInput.readOnly = false;
    hint.textContent = text || 'El cálculo se aplica al presionar Enter o salir del campo.';
  }
  function commitPriceField(source) {
    if (pendingPriceField && pendingPriceField !== source) return;
    pendingPriceField = null;
    if (source !== purchaseInput && source.value.trim() === '') {
      refreshPriceHint('El campo quedó vacío. Escribe el valor completo y confirma con Enter o sal del campo para calcular.');
      return;
    }
    const cost = purchaseInput.value.trim() === '' ? 0 : Number(purchaseInput.value);
    const sale = saleInput.value.trim() === '' ? null : Number(saleInput.value);
    const percent = percentInput.value.trim() === '' ? null : Number(percentInput.value);
    if (!Number.isFinite(cost) || cost < 0 || (sale !== null && (!Number.isFinite(sale) || sale < 0)) ||
        (percent !== null && (!Number.isFinite(percent) || percent < 0))) {
      refreshPriceHint('Revisa los importes y el porcentaje; deben ser valores válidos y no negativos.');
      return;
    }

    if (source === saleInput && sale !== null && cost > 0) {
      autoPurchaseCost = false;
      const effectivePercent = ((sale - cost) / cost) * 100;
      percentInput.value = effectivePercent.toFixed(2).replace(/\.?0+$/, '');
      refreshPriceHint(`Margen actualizado: ${percentInput.value || '0'}%. El costo se mantiene en Q${cost.toFixed(2)}.`);
      return;
    }

    if (cost > 0) {
      autoPurchaseCost = false;
      if (percent === null) { refreshPriceHint('Ingresa el porcentaje para calcular el precio de venta.'); return; }
      const calculatedSale = Math.ceil((cost * (1 + percent / 100) * 4) - 1e-8) / 4;
      saleInput.value = calculatedSale.toFixed(2);
      refreshPriceHint(`Precio de venta calculado: Q${calculatedSale.toFixed(2)} (redondeado hacia arriba a Q0.25).`);
      return;
    }

    autoPurchaseCost = true;
    if (sale === null || sale <= 0 || percent === null) {
      refreshPriceHint('Para calcular el costo, ingresa el precio de venta y el porcentaje.');
      return;
    }
    const calculatedCost = Math.round((sale / (1 + percent / 100) + Number.EPSILON) * 100) / 100;
    purchaseInput.value = calculatedCost.toFixed(2);
    if (calculatedCost > 0) {
      const effectivePercent = ((sale - calculatedCost) / calculatedCost) * 100;
      percentInput.value = effectivePercent.toFixed(2).replace(/\.?0+$/, '');
    }
    refreshPriceHint(`Costo calculado: Q${calculatedCost.toFixed(2)}. El costo se mantiene editable.`);
  }
  for (const field of [purchaseInput, saleInput, percentInput]) {
    field.addEventListener('input', () => {
      pendingPriceField = field;
      refreshPriceHint('Escribe el valor completo; se calculará al presionar Enter o salir del campo.');
    });
    field.addEventListener('blur', () => {
      if (pendingPriceField === field) commitPriceField(field);
    });
  }
  refreshPriceHint();
  productForm.addEventListener('reset', () => setTimeout(() => { autoPurchaseCost = true; pendingPriceField = null; message($('#productMessage'), ''); refreshPriceHint(); }, 0));
  productForm.addEventListener('submit', async event => {
    event.preventDefault(); message($('#productMessage'), ''); const form = event.currentTarget, data = new FormData(form), button = form.querySelector('[type=submit]');
    if (productPhotoProcessingActive) {
      message($('#productMessage'), 'La foto todavía se está procesando. Puedes seguir llenando el producto y guardarlo cuando termine.');
      productPhotoStatus.scrollIntoView({behavior: 'smooth', block: 'center'});
      return;
    }
    if (productPhotoNeedsReview) {
      message($('#productMessage'), 'El recorte está listo. Revisa y acepta la foto antes de guardar el producto.');
      reviewProductPhotoButton.focus();
      return;
    }
    const numeric = name => { const value = data.get(name); return value === '' || value == null ? null : Number(value); };
    if (!$('#replenishmentGroup').value) {
      const minimum = numeric('cantidadMinima'), maximum = numeric('cantidadMaxima');
      if (!Number.isInteger(minimum) || minimum < 0 || minimum > 2147483647) { message($('#productMessage'), 'Ingresa una cantidad mínima entera entre 0 y 2,147,483,647.'); minimumInput.focus(); minimumInput.select(); return; }
      if (!Number.isInteger(maximum) || maximum < 0 || maximum > 2147483647 || maximum < minimum) { message($('#productMessage'), 'Ingresa una cantidad máxima entera, igual o mayor que el mínimo y no superior a 2,147,483,647.'); maximumInput.focus(); maximumInput.select(); return; }
    }
    const payload = { codigo: data.get('codigo'), nombre: data.get('nombre'), descripcion: data.get('descripcion') || null, idCategoria: Number(data.get('idCategoria')), precioCompra: numeric('precioCompra') || 0, precioVenta: numeric('precioVenta') || 0, porcentaje: numeric('porcentaje') || 0, calcularCosto: autoPurchaseCost, estado: data.has('estado'), idGrupoReposicion: $('#replenishmentGroup').value ? Number($('#replenishmentGroup').value) : null, cantidadMinima: numeric('cantidadMinima'), cantidadMaxima: numeric('cantidadMaxima'), idGrupoProveedor: $('#supplierGroup').value ? Number($('#supplierGroup').value) : null };
    button.disabled = true; button.textContent = 'Guardando…';
    try {
      const photo = acceptedProductPhoto;
      if (photo) {
        if (photo.size > 5 * 1024 * 1024) throw new Error('La foto no puede superar los 5 MB.');
        payload.imagenBase64 = await readFileBase64(photo);
        payload.imagenTipo = photo.type;
      }
      const result = await request('', { method: 'POST', body: JSON.stringify(payload) });
      sessionStorage.removeItem(productDraftKey); message($('#productMessage'), result.mensaje, true); form.reset(); productPhotoPreview.hidden = true; productPhotoControls.hidden = true; productPhotoPreview.removeAttribute('src'); $('#replenishmentGroup').dispatchEvent(new Event('change')); $('#scanButton').focus();
    }
    catch (error) { if (error.status === 401) { location.reload(); return; } message($('#productMessage'), error.message); }
    finally { button.disabled = false; button.textContent = 'Crear producto'; }
  });

  function readFileBase64(file) {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => resolve(String(reader.result).split(',')[1] || '');
      reader.onerror = () => reject(new Error('No se pudo leer la fotografía.'));
      reader.readAsDataURL(file);
    });
  }

  Object.entries(managerConfig).forEach(([formId, config]) => {
    const form = $(`#${formId}`); const status = form.parentElement.querySelector('.form-message');
    form.addEventListener('submit', async event => {
      event.preventDefault(); const editId = form.dataset.editId; const values = new FormData(form); const payload = { nombre: values.get('nombre')?.trim() };
      if (config.endpoint === 'grupos-reposicion') { payload.cantidadMinima = Number(values.get('cantidadMinima')); payload.cantidadMaxima = Number(values.get('cantidadMaxima')); }
      if (config.endpoint === 'grupos-proveedor') payload.proveedores = [...form.querySelectorAll('[name=proveedores]:checked')].map(item => Number(item.value));
      const submit = form.querySelector('[type=submit]'); submit.disabled = true; message(status, 'Guardando…');
      try { await request(editId ? `/${config.endpoint}/${editId}` : `/${config.endpoint}`, { method: editId ? 'PUT' : 'POST', body: JSON.stringify(payload) }); message(status, editId ? 'Cambios guardados.' : 'Registro agregado.', true); clearEdit(form, config); await refreshCatalogs(); }
      catch (error) { message(status, error.message); }
      finally { submit.disabled = false; }
    });
    $('.cancel-edit', form).addEventListener('click', () => clearEdit(form, config));
  });
  function clearEdit(form, config) { form.reset(); delete form.dataset.editId; form.querySelector('[type=submit]').textContent = config.endpoint === 'categorias' ? 'Agregar categoría' : 'Agregar grupo'; $('.cancel-edit', form).hidden = true; if (config.endpoint === 'grupos-proveedor') renderSupplierChoices(); }
  document.addEventListener('click', async event => {
    const button = event.target.closest('[data-action]'); if (!button) return;
    const config = Object.values(managerConfig).find(item => item.endpoint === button.dataset.endpoint); if (!config) return;
    const id = Number(button.dataset.id); const collection = adminData[config.list]; const item = collection.find(value => value.id === id); if (!item) return;
    if (button.dataset.action === 'edit') {
      const form = $(`#${Object.keys(managerConfig).find(key => managerConfig[key] === config)}`); form.dataset.editId = id; form.elements.nombre.value = item.nombre;
      if (config.endpoint === 'grupos-reposicion') { form.elements.cantidadMinima.value = item.cantidadMinima; form.elements.cantidadMaxima.value = item.cantidadMaxima; }
      if (config.endpoint === 'grupos-proveedor') renderSupplierChoices(item.proveedores);
      form.querySelector('[type=submit]').textContent = 'Guardar cambios'; $('.cancel-edit', form).hidden = false; form.scrollIntoView({ behavior: 'smooth', block: 'center' }); form.elements.nombre.focus(); return;
    }
  });

  async function stopScanner() { if (stream) stream.getTracks().forEach(track => track.stop()); stream = null; $('#scannerVideo').srcObject = null; $('#scannerPanel').hidden = true; detected.clear(); }
  $('#scanButton').addEventListener('click', async () => {
    message($('#scannerMessage'), ''); $('#scanCandidates').replaceChildren(); $('#scannerPanel').hidden = false; $('#scannerPanel').scrollIntoView({ behavior: 'smooth', block: 'start' });
    if (!navigator.mediaDevices?.getUserMedia) { message($('#scannerMessage'), 'La cámara requiere HTTPS y permiso del navegador. También podés escribir el código manualmente.'); return; }
    if (!('BarcodeDetector' in window)) { message($('#scannerMessage'), 'Este navegador no trae lector de códigos compatible. Podés escribir el código manualmente.'); return; }
    try { const formats = await BarcodeDetector.getSupportedFormats(); detector = new BarcodeDetector({ formats: formats.filter(format => ['ean_13','ean_8','upc_a','upc_e','code_128','code_39','itf','codabar'].includes(format)) }); stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: { ideal: 'environment' } }, audio: false }); const video = $('#scannerVideo'); video.srcObject = stream; await video.play(); scanFrame(); }
    catch (error) { message($('#scannerMessage'), error.name === 'NotAllowedError' ? 'No se concedió permiso para usar la cámara.' : 'No se pudo iniciar la cámara. Verificá HTTPS y que otra aplicación no la esté usando.'); }
  });
  async function scanFrame() {
    if (!stream || $('#scannerPanel').hidden) return;
    try { for (const code of await detector.detect($('#scannerVideo'))) if (code.rawValue && !detected.has(code.rawValue)) { detected.add(code.rawValue); const button = document.createElement('button'); button.type = 'button'; button.className = 'button button-quiet'; button.textContent = `${code.rawValue} · ${code.format}`; button.addEventListener('click', async () => { $('#codigo').value = code.rawValue; await stopScanner(); $('#nombre').focus(); }); $('#scanCandidates').append(button); message($('#scannerMessage'), 'Elegí un código detectado para usarlo.'); } }
    catch { /* vuelve a intentar en el siguiente cuadro */ }
    requestAnimationFrame(scanFrame);
  }
  $('#closeScanner').addEventListener('click', stopScanner);
  start();
})();
