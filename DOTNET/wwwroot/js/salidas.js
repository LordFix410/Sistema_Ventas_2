(() => {
  const $ = selector => document.querySelector(selector);
  const currency = new Intl.NumberFormat('es-GT', { style: 'currency', currency: 'GTQ' });
  const today = () => { const d = new Date(); return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`; };
  const monthStart = () => `${today().slice(0, 7)}-01`;
  let catalog = { gastos: [], bancos: [] };
  let pendingConfirmation = null;

  function message(text, success = false) {
    $('#pageMessage').textContent = text || '';
    $('#pageMessage').classList.toggle('success', success);
  }

  async function api(url, options = {}) {
    const response = await fetch(url, { credentials: 'same-origin', headers: { 'Content-Type': 'application/json', ...(options.headers || {}) }, ...options });
    const data = response.status === 204 ? null : await response.json().catch(() => ({}));
    if (!response.ok) {
      if (response.status === 401 && url !== '/api/productos/sesion') show(false);
      throw new Error(data?.mensaje || data?.detail || data?.title || `No se pudo completar la solicitud (HTTP ${response.status}).`);
    }
    return data;
  }

  function show(signedIn, name = '') {
    $('#loginPanel').hidden = signedIn;
    $('#moduleApp').hidden = !signedIn;
    $('#responsibleName').textContent = name;
  }

  function renderExpenseOptions() {
    const select = $('#expenseSelect');
    select.replaceChildren(new Option('Selecciona el gasto', ''));
    const expenses = [...catalog.gastos].sort((a, b) => a.nombre.localeCompare(b.nombre, 'es', { sensitivity: 'base' }));
    for (const expense of expenses) {
      const option = new Option(expense.saldo > 0 ? expense.nombre : `${expense.nombre} · cubierto`, expense.idPeriodo > 0 ? `period:${expense.idPeriodo}` : `inactive:${expense.id}`);
      option.dataset.name = expense.nombre;
      option.dataset.balance = String(expense.saldo);
      option.dataset.periodId = String(expense.idPeriodo);
      if (expense.saldo <= 0 || expense.idPeriodo <= 0) option.disabled = true;
      select.add(option);
    }
    select.add(new Option('Otros', 'other'));
  }

  function addBankOption(source, expectedName, label, id) {
    const bank = catalog.bancos.find(item => item.nombre.toLocaleUpperCase('es-GT').includes(expectedName));
    const container = $(`#${id}`);
    if (!bank) { container.hidden = true; return; }
    container.hidden = false;
    const input = $('input', container);
    input.dataset.bankId = String(bank.id);
    input.disabled = Number(bank.disponible) <= 0;
    input.title = input.disabled ? `Saldo disponible: ${currency.format(bank.disponible)}` : '';
    $('span', container).textContent = `${label} · ${currency.format(bank.disponible)}`;
  }

  function syncExpense() {
    const selected = $('#expenseSelect').selectedOptions[0];
    const isOther = selected?.value === 'other';
    $('#otherConceptField').hidden = !isOther;
    $('#conceptInput').required = !!isOther;
    $('#conceptInput').disabled = !isOther;
    $('#selectedConcept').hidden = !selected || !selected.dataset.name || isOther;
    $('#selectedConcept').textContent = selected?.dataset.name ? `Concepto: ${selected.dataset.name}` : '';
    const saldo = Number(selected?.dataset.balance || 0);
    $('#expenseSelectHelp').textContent = selected?.dataset.name
      ? (saldo > 0 ? `Pendiente de este gasto: ${currency.format(saldo)}.` : 'Este gasto ya aparece cubierto para el mes.')
      : 'Los gastos están en orden alfabético. Elige Otros para escribir un concepto.';
  }

  function selectedPayer() { return $('[name="origenPago"]:checked')?.value || 'Caja'; }

  function syncPayer() {
    const source = selectedPayer();
    const bank = source === 'BAM' || source === 'Banrural';
    $('#receiptField').hidden = !bank;
    $('#receiptInput').required = bank;
    $('#receiptInput').disabled = !bank;
    const help = {
      Caja: 'La salida de Caja se toma en cuenta en el cierre y queda señalada para reponer.',
      Ganancia: 'Salió del efectivo reservado de Ganancia. El cierre te recordará reponer ese fondo.',
      Capital: 'Salió del efectivo reservado de Capital. El cierre te recordará reponer ese fondo.',
      BAM: 'Se validará y descontará del saldo bancario de BAM · Ganancia.',
      Banrural: 'Se validará y descontará del saldo bancario de Banrural · Capital.'
    };
    $('#expenseHelp').textContent = help[source] || '';
  }

  function sourceLabel(source) {
    return ({ Caja: 'Caja de ventas', Ganancia: 'Ganancia en efectivo', Capital: 'Capital en efectivo', BAM: 'BAM · Ganancia', Banrural: 'Banrural · Capital' })[source] || source;
  }

  function renderHistory(items) {
    const list = $('#movementList');
    list.replaceChildren();
    if (!items.length) {
      const empty = document.createElement('p');
      empty.className = 'empty-state';
      empty.textContent = 'Todavía no hay salidas registradas.';
      list.append(empty);
      return;
    }
    for (const item of items) {
      const row = document.createElement('article');
      row.className = 'closing-audit-item';
      const heading = document.createElement('div');
      heading.className = 'closing-audit-header';
      const title = document.createElement('strong');
      title.textContent = `${item.fecha} · ${item.concepto || 'Salida'}`;
      const amount = document.createElement('strong');
      amount.className = 'closing-audit-total';
      amount.textContent = currency.format(item.monto);
      heading.append(title, amount);
      const detail = document.createElement('span');
      detail.textContent = `${item.banco || 'Origen no disponible'}${item.comprobante ? ` · Ref. ${item.comprobante}` : ''}`;
      const meta = document.createElement('small');
      meta.textContent = `${item.detalle || ''}${item.responsable ? ` · ${item.responsable}` : ''}`;
      row.append(heading, detail, meta);
      list.append(row);
    }
  }

  async function refresh() {
    try {
      await api('/api/finanzas/resumen'); // También asegura que exista el período de gastos del mes.
      catalog = await api('/api/finanzas/operaciones/salidas/catalogo');
      renderExpenseOptions();
      addBankOption('BAM', 'BAM', 'BAM · Ganancia', 'bamOption');
      addBankOption('Banrural', 'BANRURAL', 'Banrural · Capital', 'banruralOption');
      const history = await api('/api/finanzas/operaciones/movimientos?tipo=salidas');
      renderHistory(history.movimientos || []);
      $('#expenseDate').min = monthStart();
      $('#expenseDate').max = today();
      syncExpense();
      syncPayer();
      message('');
    } catch (error) { message(error.message); }
  }

  function prepareConfirmation(form) {
    const values = new FormData(form);
    const selected = $('#expenseSelect').selectedOptions[0];
    const source = values.get('origenPago');
    const isOther = selected?.value === 'other';
    const amount = Number(values.get('monto'));
    const expenseName = isOther ? String(values.get('concepto') || '').trim() : selected?.dataset.name;
    const balance = Number(selected?.dataset.balance || 0);
    if (!Number.isFinite(amount) || amount <= 0) { message('Escribe un monto mayor que cero.'); return null; }
    if (!selected?.value) { message('Selecciona de qué fue el gasto.'); return null; }
    if (!isOther && (!selected.dataset.periodId || Number(selected.dataset.periodId) <= 0 || amount > balance)) { message('Ese gasto no tiene saldo pendiente suficiente. Elige otro gasto u Otros.'); return null; }
    if (isOther && (!expenseName || expenseName.length > 150)) { message('Describe el gasto Otros (máximo 150 caracteres).'); return null; }
    if ((source === 'BAM' || source === 'Banrural') && !String(values.get('comprobante') || '').trim()) { message('Escribe el número de boleta o referencia bancaria.'); return null; }
    const bank = source === 'BAM' || source === 'Banrural';
    const selectedBank = bank ? catalog.bancos.find(item => item.nombre.toLocaleUpperCase('es-GT').includes(source === 'BAM' ? 'BAM' : 'BANRURAL')) : null;
    if (bank && (!selectedBank || Number(selectedBank.disponible) < amount)) { message(`El saldo disponible de ${source} no alcanza para esta salida.`); return null; }
    return {
      amount,
      expenseName,
      source,
      receipt: String(values.get('comprobante') || '').trim(),
      bankId: bank ? selectedBank.id : null,
      periodId: isOther ? 0 : Number(selected.dataset.periodId),
      date: values.get('fecha'),
      payload: {
        origenPago: source,
        idBanco: bank ? selectedBank.id : null,
        idPeriodoGastoPersonal: isOther ? 0 : Number(selected.dataset.periodId),
        monto: amount,
        fecha: values.get('fecha'),
        concepto: isOther ? expenseName : selected.dataset.name,
        comprobante: String(values.get('comprobante') || '').trim()
      }
    };
  }

  function openConfirmation(data) {
    $('#confirmAmount').textContent = currency.format(data.amount);
    $('#confirmExpense').textContent = data.expenseName;
    $('#confirmPayer').textContent = sourceLabel(data.source);
    $('#confirmReceiptRow').hidden = !data.receipt;
    $('#confirmReceipt').textContent = data.receipt;
    const effect = {
      Caja: 'Se registrará como salida de la caja de ventas y aparecerá en el cierre para reponerla.',
      Capital: 'Se registrará desde el efectivo reservado de Capital y el cierre mostrará cuánto reponer.',
      Ganancia: 'Se registrará desde el efectivo reservado de Ganancia y el cierre mostrará cuánto reponer.',
      BAM: 'Se validará el saldo y se descontará directamente de BAM · Ganancia.',
      Banrural: 'Se validará el saldo y se descontará directamente de Banrural · Capital.'
    };
    $('#confirmEffect').textContent = effect[data.source];
    $('#confirmDialog').showModal();
    return data;
  }

  async function save(data) {
    $('#confirmExpenseSave').disabled = true;
    try {
      await api('/api/finanzas/salidas', { method: 'POST', body: JSON.stringify(data.payload) });
      $('#confirmDialog').close();
      $('#expenseForm').reset();
      pendingConfirmation = null;
      $('#expenseDate').value = today();
      syncExpense();
      syncPayer();
      message('Salida registrada.', true);
      await refresh();
    } catch (error) { message(error.message); }
    finally { $('#confirmExpenseSave').disabled = false; }
  }

  $('#loginForm').addEventListener('submit', async event => {
    event.preventDefault();
    try {
      const data = await api('/api/productos/sesion', { method: 'POST', body: JSON.stringify({ clave: new FormData(event.currentTarget).get('clave') }) });
      show(true, data.nombre);
      await refresh();
    } catch (error) { $('#loginMessage').textContent = error.message; }
  });
  $('#logoutButton').addEventListener('click', async () => { try { await api('/api/productos/sesion', { method: 'DELETE' }); } catch {} show(false); });
  $('#refreshButton').addEventListener('click', refresh);
  $('#expenseSelect').addEventListener('change', syncExpense);
  document.querySelectorAll('[name="origenPago"]').forEach(input => input.addEventListener('change', syncPayer));
  $('#expenseForm').addEventListener('submit', event => {
    event.preventDefault();
    const data = prepareConfirmation(event.currentTarget);
    if (data) { pendingConfirmation = data; openConfirmation(data); }
  });
  $('#cancelExpense').addEventListener('click', () => { pendingConfirmation = null; $('#confirmDialog').close(); });
  $('#confirmExpenseSave').addEventListener('click', () => {
    if (pendingConfirmation) save(pendingConfirmation);
  });
  $('#expenseDate').value = today();
  $('#expenseDate').min = monthStart();
  $('#expenseDate').max = today();
  syncExpense();
  syncPayer();
  (async () => {
    try {
      const session = await api('/api/productos/sesion');
      if (session.autenticado) { show(true, session.nombre); await refresh(); }
      else show(false);
    } catch (error) { show(false); $('#loginMessage').textContent = error.message; }
  })();
})();
