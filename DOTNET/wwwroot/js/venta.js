(() => {
  const $ = (selector, root = document) => root.querySelector(selector);
  const api = '/api';
  const loginPanel = $('#loginPanel'), saleApp = $('#saleApp');
  const currency = new Intl.NumberFormat('es-GT', { style: 'currency', currency: 'GTQ' });
  const cart = new Map();
  let selectedClient = null;
  const saleDraftKey = 'ld-mobile-sale-draft-v1';
  let navigationApproved = false;
  let cameraStream = null, barcodeDetector = null, scanMode = '', scanFrameId = 0;
  const visibleCodes = new Set(), missingFrames = new Map();
  const parseDotAmount = value => {
    const text = String(value ?? '').trim();
    if (!/^[0-9]+(?:\.[0-9]{1,2})?$/.test(text)) return null;
    const amount = Number(text);
    return Number.isFinite(amount) ? amount : null;
  };

  async function request(path, options = {}) {
    const response = await fetch(`${api}${path}`, { credentials: 'same-origin', headers: { 'Content-Type': 'application/json', ...(options.headers || {}) }, ...options });
    const body = response.status === 204 ? null : await response.json().catch(() => ({}));
    if (!response.ok) { const error = new Error(body?.mensaje || `Error ${response.status}`); error.status = response.status; error.body = body; throw error; }
    return body;
  }
  function setMessage(element, text, success = false) {
    element.textContent = text || '';
    element.classList.toggle('success', success);
  }
  function signedIn() {
    loginPanel.hidden = true;
    saleApp.hidden = false;
    $('#logoutButton').hidden = false;
  }
  function money(value) { return currency.format(Number(value) || 0); }
  function switchView(id) {
    if (cameraStream || !$('#scannerPanel').hidden) stopScanner();
    document.querySelectorAll('.sale-tab').forEach(tab => tab.classList.toggle('active', tab.dataset.view === id));
    $('#saleView').hidden = id !== 'saleView';
    $('#priceView').hidden = id !== 'priceView';
  }
  function renderSelectedClient() {
    $('#selectedClientDocument').textContent = selectedClient?.documento || 'CF';
    $('#checkoutClientSummary').textContent = selectedClient
      ? `${selectedClient.nombreCompleto} · ${selectedClient.documento}`
      : 'CONSUMIDOR FINAL · CF';
    $('#changeClientButton').setAttribute('aria-label', selectedClient
      ? `Cliente ${selectedClient.nombreCompleto}. Cambiar cliente`
      : 'Buscar cliente por NIT');
    persistSaleDraft();
  }
  function saleDraftHasData() {
    return cart.size > 0 || !!selectedClient || !!$('#cashReceived').value.trim() || !!$('#saleSearch').value.trim() ||
      !!$('#temporaryDescription').value.trim() || !!$('#temporaryPrice').value.trim();
  }
  function hasSaleDraft() { return !!sessionStorage.getItem(saleDraftKey); }
  function persistSaleDraft() {
    if (saleApp.hidden) return;
    if (!saleDraftHasData()) { sessionStorage.removeItem(saleDraftKey); return; }
    sessionStorage.setItem(saleDraftKey, JSON.stringify({
      productos: [...cart.values()], cliente: selectedClient,
      efectivo: $('#cashReceived').value, busqueda: $('#saleSearch').value,
      articulo: { descripcion: $('#temporaryDescription').value, precio: $('#temporaryPrice').value, cantidad: $('#temporaryQuantity').value }
    }));
  }
  function restoreSaleDraft() {
    const saved = sessionStorage.getItem(saleDraftKey);
    if (!saved) return false;
    try {
      const draft = JSON.parse(saved);
      cart.clear();
      for (const item of draft.productos || []) {
        if (!item || typeof item.key !== 'string' || !Number.isInteger(item.cantidad) || item.cantidad < 1 || !Number.isFinite(Number(item.precioVenta))) continue;
        cart.set(item.key, { ...item, precioVenta: Number(item.precioVenta) });
      }
      selectedClient = draft.cliente && Number.isInteger(draft.cliente.id) ? draft.cliente : null;
      $('#cashReceived').value = typeof draft.efectivo === 'string' ? draft.efectivo : '';
      $('#saleSearch').value = typeof draft.busqueda === 'string' ? draft.busqueda : '';
      $('#temporaryDescription').value = draft.articulo?.descripcion || '';
      $('#temporaryPrice').value = draft.articulo?.precio || '';
      $('#temporaryQuantity').value = draft.articulo?.cantidad || '1';
      setMessage($('#saleSearchMessage'), 'Borrador de venta recuperado en este teléfono.', true);
      return true;
    } catch { sessionStorage.removeItem(saleDraftKey); return false; }
  }
  function closeClientDialog() {
    if ($('#clientDialog').open) $('#clientDialog').close();
  }
  $('#changeClientButton').addEventListener('click', () => {
    $('#clientSearchForm').reset();
    $('#clientResults').replaceChildren();
    $('#clientCreateForm').hidden = true;
    $('#clientCreateForm').reset();
    setMessage($('#clientSearchMessage'), 'Busca por NIT o documento. Si no existe, podrás registrarlo aquí.');
    setMessage($('#clientCreateMessage'), '');
    $('#clientDialog').showModal();
    $('#clientDocumentSearch').focus();
  });
  $('#closeClientDialog').addEventListener('click', closeClientDialog);
  $('#paymentChangeClientButton').addEventListener('click', () => {
    $('#changeClientButton').click();
  });
  $('#cancelClientCreate').addEventListener('click', () => {
    $('#clientCreateForm').hidden = true;
    setMessage($('#clientCreateMessage'), '');
    $('#clientDocumentSearch').focus();
  });
  $('#useCfButton').addEventListener('click', () => {
    selectedClient = null;
    renderSelectedClient();
    closeClientDialog();
  });
  $('#clientSearchForm').addEventListener('submit', async event => {
    event.preventDefault();
    const documentNumber = $('#clientDocumentSearch').value.trim();
    $('#clientCreateForm').hidden = true;
    $('#clientResults').replaceChildren();
    setMessage($('#clientSearchMessage'), 'Buscando cliente…');
    try {
      const clients = await request(`/ventas-moviles/clientes?q=${encodeURIComponent(documentNumber)}`);
      if (!clients.length) {
        $('#clientNewDocument').value = documentNumber.toLocaleUpperCase('es-GT');
        $('#clientCreateForm').hidden = false;
        setMessage($('#clientSearchMessage'), 'No se encontró el documento. Puedes registrar el cliente aquí.');
        $('#clientNewName').focus();
        return;
      }
      const client = clients[0];
      const card = document.createElement('article'); card.className = 'client-result-card';
      const detail = document.createElement('div');
      const name = document.createElement('strong'); name.textContent = client.nombreCompleto;
      const doc = document.createElement('small'); doc.textContent = client.documento;
      detail.append(name, doc);
      const choose = document.createElement('button'); choose.type = 'button'; choose.className = 'button button-primary'; choose.textContent = 'Usar este cliente';
      choose.addEventListener('click', () => {
        selectedClient = client;
        renderSelectedClient();
        closeClientDialog();
      });
      card.append(detail, choose);
      $('#clientResults').append(card);
      setMessage($('#clientSearchMessage'), 'Cliente encontrado. Confirma que sea la persona correcta.', true);
    } catch (error) {
      if (error.status === 401) { location.reload(); return; }
      setMessage($('#clientSearchMessage'), error.message);
    }
  });
  $('#clientCreateForm').addEventListener('submit', async event => {
    event.preventDefault();
    const button = event.currentTarget.querySelector('button[type="submit"]');
    button.disabled = true;
    setMessage($('#clientCreateMessage'), 'Guardando cliente…');
    const payload = {
      documento: $('#clientNewDocument').value.trim(),
      nombreCompleto: $('#clientNewName').value.trim(),
      correo: $('#clientNewEmail').value.trim(),
      telefono: $('#clientNewPhone').value.trim()
    };
    try {
      selectedClient = await request('/ventas-moviles/clientes', { method: 'POST', body: JSON.stringify(payload) });
      renderSelectedClient();
      closeClientDialog();
      setMessage($('#saleSearchMessage'), `Cliente ${selectedClient.nombreCompleto} agregado a esta venta.`, true);
    } catch (error) {
      if (error.status === 401) { location.reload(); return; }
      setMessage($('#clientCreateMessage'), error.message);
    } finally { button.disabled = false; }
  });
  document.querySelectorAll('.sale-tab').forEach(tab => tab.addEventListener('click', () => switchView(tab.dataset.view)));
  $('#saleSearch').addEventListener('input', persistSaleDraft);
  for (const field of [$('#temporaryDescription'), $('#temporaryQuantity')]) field.addEventListener('input', persistSaleDraft);
  window.addEventListener('beforeunload', event => {
    if (navigationApproved || !hasSaleDraft()) return;
    event.preventDefault(); event.returnValue = '';
  });
  document.addEventListener('click', event => {
    const link = event.target.closest('a[href]');
    if (!link || !hasSaleDraft()) return;
    const destination = new URL(link.href, location.href);
    if (destination.href === location.href) return;
    if (confirm('Hay una venta en curso. El borrador quedó guardado en esta pestaña. ¿Quieres salir de la pantalla?')) { navigationApproved = true; return; }
    event.preventDefault();
  });

  $('#loginForm').addEventListener('submit', async event => {
    event.preventDefault();
    const form = event.currentTarget, button = form.querySelector('button');
    button.disabled = true;
    setMessage($('#loginMessage'), 'Validando vendedor…');
    try {
      const session = await request('/productos/sesion', { method: 'POST', body: JSON.stringify({ clave: new FormData(form).get('clave') }) });
      signedIn();
      restoreSaleDraft();
      renderCart(); renderSelectedClient(); updatePayment();
    } catch (error) { setMessage($('#loginMessage'), error.message); }
    finally { button.disabled = false; }
  });

  $('#logoutButton').addEventListener('click', async () => {
    if (hasSaleDraft() && !confirm('Hay una venta en curso. El borrador quedó guardado en esta pestaña. ¿Quieres cerrar sesión?')) return;
    await stopScanner();
    await request('/productos/sesion', { method: 'DELETE' });
    navigationApproved = true;
    location.reload();
  });

  function total() {
    let result = 0;
    for (const item of cart.values()) result += item.precioVenta * item.cantidad;
    return Math.round((result + Number.EPSILON) * 100) / 100;
  }
  function updatePayment() {
    const raw = $('#cashReceived').value;
    const parsed = parseDotAmount(raw);
    const value = parsed ?? 0;
    const currentTotal = total();
    $('#saleTotal').textContent = money(currentTotal);
    $('#checkoutTotal').textContent = money(currentTotal);
    $('#changeAmount').textContent = money(Math.max(0, value - currentTotal));
    $('#openPaymentButton').disabled = cart.size === 0;
    $('#registerSaleButton').disabled = cart.size === 0 || parsed === null || value < currentTotal;
    if (raw.trim() && parsed === null) setMessage($('#saleMessage'), 'Escribe el importe con punto decimal, por ejemplo 30.50.');
    else if ($('#saleMessage').textContent.startsWith('Escribe el importe con punto decimal')) setMessage($('#saleMessage'), '');
  }
  $('#openPaymentButton').addEventListener('click', () => {
    if (!cart.size) return;
    updatePayment();
    $('#paymentDialog').showModal();
    $('#cashReceived').focus();
    $('#cashReceived').select();
  });
  $('#closePaymentDialog').addEventListener('click', () => $('#paymentDialog').close());
  $('#closeTemporaryDialog').addEventListener('click', () => $('#temporaryDialog').close());
  $('#cancelTemporaryDialog').addEventListener('click', () => $('#temporaryDialog').close());
  $('#addSpecialButton').addEventListener('click', () => {
    $('#temporaryDialog').showModal();
    $('#temporaryDescription').focus();
  });
  $('#closeReceiptDialog').addEventListener('click', () => {
    $('#saleReceiptDialog').close();
  });
  function renderCart() {
    const rows = $('#cartRows');
    rows.replaceChildren();
    $('#cartCount').textContent = String(cart.size);
    $('#stickyCartSummary').textContent = `${cart.size} artículo${cart.size === 1 ? '' : 's'} en la venta`;
    $('#clearCartButton').disabled = cart.size === 0;
    $('#emptyCart').hidden = cart.size > 0;
    for (const item of cart.values()) {
      const row = document.createElement('article');
      row.className = 'cart-line';
      const info = document.createElement('div');
      info.className = 'cart-line-main';
      const name = document.createElement('strong'); name.textContent = item.nombre;
      const details = document.createElement('small'); details.textContent = `${item.temporal ? 'ARTÍCULO FUERA DE CATÁLOGO' : item.codigo} · ${money(item.precioVenta)} c/u`;
      info.append(name, details);
      const subtotal = document.createElement('div');
      subtotal.className = 'cart-line-subtotal';
      subtotal.textContent = money(item.precioVenta * item.cantidad);
      const controls = document.createElement('div');
      controls.className = 'cart-line-controls';
      const quantityLabel = document.createElement('label'); quantityLabel.textContent = 'Cantidad';
      const quantity = document.createElement('input'); quantity.type = 'number'; quantity.min = '1'; quantity.max = '1000000'; quantity.step = '1'; quantity.inputMode = 'numeric'; quantity.value = String(item.cantidad); quantity.setAttribute('aria-label', `Cantidad de ${item.nombre}`);
      quantity.addEventListener('change', () => {
        const parsed = Number(quantity.value);
        if (!Number.isInteger(parsed) || parsed < 1 || parsed > 1000000) { quantity.value = String(item.cantidad); return; }
        item.cantidad = parsed; renderCart();
      });
      quantityLabel.append(quantity);
      const remove = document.createElement('button'); remove.type = 'button'; remove.className = 'remove-line'; remove.textContent = 'Quitar'; remove.addEventListener('click', () => { cart.delete(item.key); renderCart(); });
      controls.append(quantityLabel, remove);
      row.append(info, subtotal, controls);
      rows.append(row);
    }
    updatePayment();
    persistSaleDraft();
  }
  function addProduct(product, amount = 1) {
    const key = `p:${product.id}`;
    const old = cart.get(key);
    if (old) old.cantidad += amount;
    else cart.set(key, { key, idProducto: product.id, codigo: product.codigo, nombre: product.nombre, descripcion: product.descripcion || '', precioVenta: Number(product.precioVenta), cantidad: amount, temporal: false });
    renderCart();
    setMessage($('#saleSearchMessage'), old ? `Se actualizaron ${product.nombre}: ${old.cantidad} unidades.` : `${product.nombre} agregado: ${amount} unidad(es).`, true);
  }

  $('#temporaryItemForm').addEventListener('submit', event => {
    event.preventDefault();
    const description = $('#temporaryDescription').value.trim().toLocaleUpperCase('es-GT');
    const price = parseDotAmount($('#temporaryPrice').value);
    const quantity = Number($('#temporaryQuantity').value);
    if (!description || description.length > 30 || price === null || price <= 0 || !Number.isInteger(quantity) || quantity < 1) {
      setMessage($('#saleSearchMessage'), 'Revisa la descripción, el precio con punto decimal y la cantidad.');
      return;
    }
    const key = `t:${Date.now()}:${Math.random().toString(36).slice(2)}`;
    cart.set(key, { key, idProducto: null, codigo: '', nombre: description, descripcionTemporal: description, precioVenta: Math.round(price * 100) / 100, cantidad: quantity, temporal: true });
    renderCart();
    event.currentTarget.reset();
    $('#temporaryQuantity').value = '1';
    setMessage($('#saleSearchMessage'), `${description} agregado como artículo temporal.`, true);
    $('#temporaryDialog').close();
    persistSaleDraft();
  });
  for (const input of [$('#temporaryPrice'), $('#cashReceived')]) {
    input.addEventListener('input', () => {
      // Algunos teclados móviles muestran coma según el idioma; normalizamos el campo a punto.
      if (input.value.includes(',')) {
        const cursor = input.selectionStart;
        input.value = input.value.replace(/,/g, '.');
        if (cursor !== null) input.setSelectionRange(cursor, cursor);
      }
      if (input.id === 'cashReceived') updatePayment();
      persistSaleDraft();
    });
  }

  function renderResults(target, products, mode, append = false) {
    if (!append) target.replaceChildren();
    for (const product of products) {
      if (append && [...target.children].some(card => card.dataset.productId === String(product.id))) continue;
      const card = document.createElement('article'); card.className = `result-card${mode === 'price' ? ' price-result-card' : ''}`;
      card.dataset.productId = String(product.id);
      const info = document.createElement('div'); info.className = 'result-info';
      const name = document.createElement('strong'); name.textContent = product.nombre;
      info.append(name);
      if (product.descripcion) { const description = document.createElement('small'); description.className = 'result-description'; description.textContent = product.descripcion; info.append(description); }
      if (product.codigo) { const code = document.createElement('small'); code.className = 'result-code'; code.textContent = product.codigo; info.append(code); }
      const price = document.createElement('strong'); price.className = 'result-price'; price.textContent = money(product.precioVenta);
      card.append(info, price);
      if (mode === 'price') {
        const controls = document.createElement('div'); controls.className = 'price-result-actions';
        const decrement = document.createElement('button'); decrement.type = 'button'; decrement.className = 'quantity-step'; decrement.textContent = '−'; decrement.setAttribute('aria-label', 'Restar cantidad');
        const quantity = document.createElement('input'); quantity.type = 'number'; quantity.min = '1'; quantity.max = '1000000'; quantity.step = '1'; quantity.inputMode = 'numeric'; quantity.value = '1'; quantity.setAttribute('aria-label', `Cantidad de ${product.nombre}`);
        const increment = document.createElement('button'); increment.type = 'button'; increment.className = 'quantity-step'; increment.textContent = '+'; increment.setAttribute('aria-label', 'Sumar cantidad');
        decrement.addEventListener('click', () => { quantity.value = String(Math.max(1, (Number(quantity.value) || 1) - 1)); });
        increment.addEventListener('click', () => { quantity.value = String(Math.min(1000000, (Number(quantity.value) || 1) + 1)); });
        const add = document.createElement('button'); add.type = 'button'; add.className = 'button button-primary result-add'; add.textContent = 'Agregar';
        add.addEventListener('click', () => {
          const amount = Number(quantity.value);
          if (!Number.isInteger(amount) || amount < 1 || amount > 1000000) { quantity.value = '1'; quantity.focus(); return; }
          addProduct(product, amount);
          switchView('saleView');
        });
        controls.append(decrement, quantity, increment, add);
        card.append(controls);
      } else {
        const add = document.createElement('button'); add.type = 'button'; add.className = 'button button-primary'; add.textContent = 'Agregar · 1 unidad';
        add.addEventListener('click', () => addProduct(product));
        card.append(add);
      }
      target.append(card);
    }
  }
  async function findProducts(term, mode, fromCamera = false) {
    const messageElement = mode === 'sale' ? $('#saleSearchMessage') : $('#priceSearchMessage');
    const resultsElement = mode === 'sale' ? $('#saleResults') : $('#priceResults');
    if (!term.trim()) { setMessage(messageElement, 'Escribe un código o nombre de producto.'); return; }
    setMessage(messageElement, 'Buscando…');
    try {
      const products = await request(`/ventas-moviles/productos?q=${encodeURIComponent(term.trim())}`);
      const exact = products.find(product => product.codigo.localeCompare(term.trim(), undefined, { sensitivity: 'accent' }) === 0);
      if (mode === 'sale' && fromCamera && exact) {
        addProduct(exact);
        return;
      }
      renderResults(resultsElement, products, mode, mode === 'price');
      if (!products.length) setMessage(messageElement, 'No se encontró un producto activo con ese código o nombre.');
      else if (exact && mode === 'sale') {
        addProduct(exact);
        resultsElement.replaceChildren();
      } else setMessage(messageElement, `${products.length}${products.length === 20 ? ' o más' : ''} resultado(s).`, true);
    } catch (error) {
      if (error.status === 401) { location.reload(); return; }
      setMessage(messageElement, error.message);
    }
  }
  $('#saleSearchForm').addEventListener('submit', event => { event.preventDefault(); findProducts($('#saleSearch').value, 'sale'); });
  $('#priceSearchForm').addEventListener('submit', event => { event.preventDefault(); findProducts($('#priceSearch').value, 'price'); });
  $('#clearCartButton').addEventListener('click', () => { cart.clear(); $('#cashReceived').value = ''; setMessage($('#saleMessage'), ''); renderCart(); persistSaleDraft(); });

  $('#registerSaleButton').addEventListener('click', async () => {
    const button = $('#registerSaleButton');
    const received = parseDotAmount($('#cashReceived').value);
    if (!cart.size || received === null || received < total()) {
      if (received === null) setMessage($('#saleMessage'), 'Escribe el importe con punto decimal, por ejemplo 30.50.');
      return;
    }
    button.disabled = true; button.textContent = 'Registrando…';
    setMessage($('#saleMessage'), 'Registrando la venta…');
    try {
      const clientAtSale = selectedClient;
      const response = await request('/ventas-moviles/registrar', {
        method: 'POST',
        body: JSON.stringify({ montoPago: received, idCliente: selectedClient?.id ?? null, productos: [...cart.values()].map(item => item.temporal
          ? ({ descripcionTemporal: item.descripcionTemporal, precioUnitarioTemporal: item.precioVenta, cantidad: item.cantidad })
          : ({ idProducto: item.idProducto, cantidad: item.cantidad })) })
      });
      cart.clear(); $('#cashReceived').value = ''; $('#saleResults').replaceChildren(); $('#saleSearch').value = '';
      sessionStorage.removeItem(saleDraftKey);
      selectedClient = null; renderSelectedClient();
      renderCart();
      setMessage($('#saleMessage'), response.mensaje, true);
      $('#receiptNumber').textContent = response.numeroDocumento;
      $('#receiptSummary').textContent = `Total ${money(response.total)} · Vuelto ${money(response.cambio)} · Cliente ${clientAtSale?.documento || 'CF'}${clientAtSale ? ` · ${clientAtSale.nombreCompleto}` : ''}`;
      $('#paymentDialog').close();
      $('#saleReceiptDialog').showModal();
      try {
        await navigator.clipboard.writeText(response.numeroDocumento);
        setMessage($('#copyReceiptMessage'), 'Número copiado al portapapeles.', true);
      } catch {
        setMessage($('#copyReceiptMessage'), 'Toca “Copiar número” para copiarlo.');
      }
    } catch (error) {
      if (error.status === 401) { location.reload(); return; }
      setMessage($('#saleMessage'), error.message);
      if (error.body?.total != null) {
        $('#saleTotal').textContent = money(error.body.total);
        $('#cashReceived').focus(); $('#cashReceived').select();
      }
    } finally { button.textContent = 'Registrar venta'; updatePayment(); }
  });

  $('#copyReceiptButton').addEventListener('click', async () => {
    const number = $('#receiptNumber').textContent.trim();
    if (!number) return;
    try {
      await navigator.clipboard.writeText(number);
      setMessage($('#copyReceiptMessage'), `Boleta ${number} copiada.`, true);
    } catch {
      const helper = document.createElement('textarea');
      helper.value = number; helper.setAttribute('readonly', ''); helper.style.position = 'fixed'; helper.style.opacity = '0';
      document.body.append(helper); helper.select();
      const copied = document.execCommand('copy'); helper.remove();
      setMessage($('#copyReceiptMessage'), copied ? `Boleta ${number} copiada.` : `Número de boleta: ${number}` , copied);
    }
  });

  async function startScanner(mode) {
    scanMode = mode;
    $('#scannerPanel').hidden = false;
    $('#scannerPanel').scrollIntoView({ behavior: 'smooth', block: 'start' });
    setMessage($('#scannerMessage'), '');
    if (!window.isSecureContext || !navigator.mediaDevices?.getUserMedia) {
      setMessage($('#scannerMessage'), 'La cámara requiere abrir esta página mediante HTTPS y conceder permiso. También puedes escribir o escanear con un lector externo.');
      return;
    }
    if (!('BarcodeDetector' in window)) {
      setMessage($('#scannerMessage'), 'Este navegador no incluye lector de códigos compatible. Usa el campo de búsqueda o un lector externo.');
      return;
    }
    try {
      const supported = await BarcodeDetector.getSupportedFormats();
      const formats = supported.filter(format => ['ean_13', 'ean_8', 'upc_a', 'upc_e', 'code_128', 'code_39', 'itf', 'codabar'].includes(format));
      if (!formats.length) { setMessage($('#scannerMessage'), 'El navegador no reconoce los formatos comunes de código de barras.'); return; }
      barcodeDetector = new BarcodeDetector({ formats });
      cameraStream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: { ideal: 'environment' } }, audio: false });
      const video = $('#scannerVideo'); video.srcObject = cameraStream; await video.play();
      visibleCodes.clear(); missingFrames.clear(); scanFrameId = requestAnimationFrame(scanFrame);
      setMessage($('#scannerMessage'), mode === 'sale' ? 'Escanea cada producto. Si vuelves a escanearlo, se suma otra unidad.' : 'Escanea un producto para consultar su precio.');
    } catch (error) {
      setMessage($('#scannerMessage'), error.name === 'NotAllowedError' ? 'No se concedió permiso para usar la cámara.' : 'No se pudo iniciar la cámara. Verifica HTTPS y que otra aplicación no la esté usando.');
    }
  }
  async function scanFrame() {
    if (!cameraStream || $('#scannerPanel').hidden) return;
    try {
      const detected = await barcodeDetector.detect($('#scannerVideo'));
      const frameCodes = new Set(detected.map(item => item.rawValue).filter(Boolean));
      for (const code of frameCodes) {
        missingFrames.set(code, 0);
        if (!visibleCodes.has(code)) {
          visibleCodes.add(code);
          findProducts(code, scanMode, scanMode === 'sale');
        }
      }
      for (const code of [...visibleCodes]) {
        if (frameCodes.has(code)) continue;
        const misses = (missingFrames.get(code) || 0) + 1;
        if (misses >= 8) { visibleCodes.delete(code); missingFrames.delete(code); }
        else missingFrames.set(code, misses);
      }
    } catch { /* continúa intentando en el siguiente cuadro */ }
    scanFrameId = requestAnimationFrame(scanFrame);
  }
  async function stopScanner() {
    if (scanFrameId) cancelAnimationFrame(scanFrameId);
    if (cameraStream) cameraStream.getTracks().forEach(track => track.stop());
    cameraStream = null; barcodeDetector = null; visibleCodes.clear(); missingFrames.clear();
    $('#scannerVideo').srcObject = null; $('#scannerPanel').hidden = true;
  }
  $('#scanSaleButton').addEventListener('click', () => startScanner('sale'));
  $('#scanPriceButton').addEventListener('click', () => startScanner('price'));
  $('#closeScanner').addEventListener('click', stopScanner);

  async function start() {
    try {
      const session = await request('/productos/sesion');
      if (session.autenticado) { signedIn(); restoreSaleDraft(); }
    } catch (error) { setMessage($('#loginMessage'), error.message); }
    renderCart();
    renderSelectedClient();
  }
  start();
})();
