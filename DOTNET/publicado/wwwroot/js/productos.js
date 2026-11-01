(() => {
  const $ = (selector, root = document) => root.querySelector(selector);
  const api = '/api/productos';
  const loginPanel = $('#loginPanel'), productApp = $('#productApp');
  let stream = null, detector = null, detected = new Set(), adminData = null;
  let catalogWarnings = [];
  const managerConfig = {
    categoryManageForm: { endpoint: 'categorias', list: 'categorias', rows: 'categoryRows', select: 'categoria', fields: ['nombre'] },
    productGroupManageForm: { endpoint: 'grupos-reposicion', list: 'gruposProducto', rows: 'productGroupRows', select: 'replenishmentGroup', fields: ['nombre', 'cantidadMinima', 'cantidadMaxima'] },
    supplierGroupManageForm: { endpoint: 'grupos-proveedor', list: 'gruposProveedor', rows: 'supplierGroupRows', select: 'supplierGroup', fields: ['nombre', 'proveedores'] }
  };
  async function request(path, options = {}) {
    const response = await fetch(`${api}${path}`, { credentials: 'same-origin', headers: { 'Content-Type': 'application/json', ...(options.headers || {}) }, ...options });
    const body = response.status === 204 ? null : await response.json().catch(() => ({}));
    if (!response.ok) { const error = new Error(body?.mensaje || `Error ${response.status}`); error.status = response.status; throw error; }
    return body;
  }
  function message(element, text, success = false) { if (!element) return; element.textContent = text || ''; element.classList.toggle('success', success); }
  function showSignedIn(name) { loginPanel.hidden = true; productApp.hidden = false; $('#signedIn').hidden = false; $('#userName').textContent = name || 'Sesión iniciada'; }
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
    const summary = `Categorías: ${data.categorias.length} · Grupos: ${data.gruposReposicion.length} · Grupos de proveedores: ${data.gruposProveedor.length} · Proveedores: ${data.proveedores.length}`;
    message($('#catalogMessage'), warnings.length ? `${summary}. ${warnings.join(' ')}` : summary, warnings.length === 0);
  }
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
    try { const status = await request('/sesion'); if (status.autenticado) { showSignedIn(status.nombre); message($('#catalogMessage'), 'Cargando catálogos…'); await refreshCatalogs(); } }
    catch (error) { if (productApp.hidden) message($('#loginMessage'), error.message); else message($('#catalogMessage'), error.message); }
  }
  $('#loginForm').addEventListener('submit', async event => {
    event.preventDefault(); const form = new FormData(event.currentTarget); const button = event.currentTarget.querySelector('button'); button.disabled = true;
    message($('#loginMessage'), 'Verificando contraseña…');
    try { const session = await request('/sesion', { method: 'POST', body: JSON.stringify({ clave: form.get('clave') }) }); showSignedIn(session.nombre); message($('#catalogMessage'), 'Cargando catálogos…'); await refreshCatalogs(); }
    catch (error) { if (productApp.hidden) message($('#loginMessage'), error.message); else message($('#catalogMessage'), error.message); }
    finally { button.disabled = false; }
  });
  $('#logoutButton').addEventListener('click', async () => { await stopScanner(); await request('/sesion', { method: 'DELETE' }); location.reload(); });
  document.querySelectorAll('.product-tab').forEach(button => button.addEventListener('click', () => {
    document.querySelectorAll('.product-tab').forEach(tab => tab.classList.toggle('active', tab === button));
    document.querySelectorAll('.workspace-view').forEach(view => { view.hidden = view.id !== button.dataset.view; });
    if (button.dataset.view !== 'viewProduct' && adminData) refreshAdministration().catch(error => message($('#catalogMessage'), `No se pudieron actualizar las listas: ${error.message}`));
  }));
  $('#replenishmentGroup').addEventListener('change', event => { const useGroup = !!event.target.value; $('#individualLimits').hidden = useGroup; $('#individualLimits').querySelectorAll('input').forEach(input => { input.disabled = useGroup; if (useGroup) input.value = ''; }); });
  const purchaseInput = $('#precioCompra'), saleInput = $('#precioVenta'), percentInput = $('#porcentaje');
  const productForm = $('#productForm');
  const minimumInput = $('input[name="cantidadMinima"]', productForm);
  const maximumInput = $('input[name="cantidadMaxima"]', productForm);
  const submitProduct = productForm.querySelector('[type="submit"]');
  let autoPurchaseCost = true;

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
    if (event.target === $('#replenishmentGroup') && event.target.value) {
      submitProduct.focus();
      return;
    }
    next.focus();
    if (next instanceof HTMLInputElement && next.type === 'number') next.select();
  });
  function updatePricePreview() {
    const cost = Number(purchaseInput.value) || 0;
    const sale = Number(saleInput.value) || 0;
    const percent = Number(percentInput.value) || 0;
    const hint = $('#priceHint');
    if (autoPurchaseCost) {
      saleInput.readOnly = false;
      if (sale > 0 && percent >= 0) {
        const calculatedCost = Math.round((sale / (1 + percent / 100) + Number.EPSILON) * 100) / 100;
        purchaseInput.value = calculatedCost.toFixed(2);
        if (calculatedCost > 0) {
          // Refleja el margen real tras redondear el costo a centavos, como hace SQL Server.
          const effectivePercent = (sale - calculatedCost) / calculatedCost * 100;
          percentInput.value = effectivePercent.toFixed(2).replace(/\.?0+$/, '');
        }
        hint.textContent = `Costo calculado: Q${calculatedCost.toFixed(2)}. Si editás este costo, el precio de venta se calculará desde él.`;
      } else {
        purchaseInput.value = '0';
        hint.textContent = 'Escribe el precio de venta y el porcentaje para calcular el costo automáticamente.';
      }
      return;
    }
    if (cost > 0 && percent >= 0) {
      // Add a tiny epsilon before ceiling to neutralize binary floating point noise.
      const calculatedSale = Math.ceil((cost * (1 + percent / 100) * 4) - 1e-8) / 4;
      saleInput.readOnly = true;
      saleInput.value = calculatedSale.toFixed(2);
      hint.textContent = `Venta calculada: Q${calculatedSale.toFixed(2)}. Se redondea hacia arriba a intervalos de Q0.25.`;
    } else {
      saleInput.readOnly = false;
      autoPurchaseCost = true;
      hint.textContent = 'Escribe el precio de venta y el porcentaje para calcular el costo automáticamente.';
    }
  }
  purchaseInput.addEventListener('input', () => { autoPurchaseCost = (Number(purchaseInput.value) || 0) <= 0; updatePricePreview(); });
  percentInput.addEventListener('input', updatePricePreview);
  saleInput.addEventListener('input', updatePricePreview);
  updatePricePreview();
  productForm.addEventListener('reset', () => setTimeout(() => { autoPurchaseCost = true; message($('#productMessage'), ''); updatePricePreview(); }, 0));
  productForm.addEventListener('submit', async event => {
    event.preventDefault(); message($('#productMessage'), ''); const form = event.currentTarget, data = new FormData(form), button = form.querySelector('[type=submit]');
    const numeric = name => { const value = data.get(name); return value === '' || value == null ? null : Number(value); };
    if (!$('#replenishmentGroup').value) {
      const minimum = numeric('cantidadMinima'), maximum = numeric('cantidadMaxima');
      if (!Number.isInteger(minimum) || minimum < 0 || minimum > 2147483647) { message($('#productMessage'), 'Ingresa una cantidad mínima entera entre 0 y 2,147,483,647.'); minimumInput.focus(); minimumInput.select(); return; }
      if (!Number.isInteger(maximum) || maximum < 0 || maximum > 2147483647 || maximum < minimum) { message($('#productMessage'), 'Ingresa una cantidad máxima entera, igual o mayor que el mínimo y no superior a 2,147,483,647.'); maximumInput.focus(); maximumInput.select(); return; }
    }
    const payload = { codigo: data.get('codigo'), nombre: data.get('nombre'), descripcion: data.get('descripcion') || null, idCategoria: Number(data.get('idCategoria')), precioCompra: numeric('precioCompra') || 0, precioVenta: numeric('precioVenta') || 0, porcentaje: numeric('porcentaje') || 0, calcularCosto: autoPurchaseCost, estado: data.has('estado'), idGrupoReposicion: $('#replenishmentGroup').value ? Number($('#replenishmentGroup').value) : null, cantidadMinima: numeric('cantidadMinima'), cantidadMaxima: numeric('cantidadMaxima'), idGrupoProveedor: $('#supplierGroup').value ? Number($('#supplierGroup').value) : null };
    button.disabled = true; button.textContent = 'Guardando…';
    try { const result = await request('', { method: 'POST', body: JSON.stringify(payload) }); message($('#productMessage'), result.mensaje, true); form.reset(); $('#replenishmentGroup').dispatchEvent(new Event('change')); $('#codigo').focus(); }
    catch (error) { if (error.status === 401) { location.reload(); return; } message($('#productMessage'), error.message); }
    finally { button.disabled = false; button.textContent = 'Crear producto'; }
  });

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
