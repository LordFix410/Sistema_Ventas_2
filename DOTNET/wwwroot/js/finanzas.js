(() => {
  const $ = selector => document.querySelector(selector);
  const loginPanel = $('#loginPanel');
  const financeApp = $('#financeApp');
  const loginMessage = $('#loginMessage');
  const pageMessage = $('#pageMessage');
  const currency = new Intl.NumberFormat('es-GT', {style: 'currency', currency: 'GTQ'});
  let summary = null;

  function showMessage(element, message, success = false) {
    element.textContent = message || '';
    element.classList.toggle('success', success);
  }
  async function api(url, options = {}) {
    const response = await fetch(url, {credentials: 'same-origin', headers: {'Content-Type': 'application/json', ...(options.headers || {})}, ...options});
    const data = response.status === 204 ? null : await response.json().catch(() => ({}));
    if (!response.ok) {
      if (response.status === 401 && url !== '/api/productos/sesion') setSignedIn(false);
      throw new Error(data?.mensaje || (response.status === 401 ? 'La sesión venció. Inicia sesión otra vez.' : 'No se pudo completar la solicitud.'));
    }
    return data;
  }
  function setSignedIn(signedIn, name = '') {
    loginPanel.hidden = signedIn;
    financeApp.hidden = !signedIn;
    if (signedIn) {
      $('#responsibleName').textContent = name;
      $('#depositResponsible').textContent = name;
    } else {
      $('#loginForm').reset();
      $('#loginForm [name=clave]').focus();
    }
  }
  function localToday() {
    const now = new Date();
    return `${now.getFullYear()}-${String(now.getMonth()+1).padStart(2,'0')}-${String(now.getDate()).padStart(2,'0')}`;
  }
  function localDateTime() {
    const now = new Date();
    return `${localToday()}T${String(now.getHours()).padStart(2,'0')}:${String(now.getMinutes()).padStart(2,'0')}`;
  }
  function updateAllocation() {
    const openingCash = Math.round((Number($('#openingCash').value) || 0) * 100) / 100;
    const closingCash = Math.round((Number($('#closingCash').value) || 0) * 100) / 100;
    const cashOutflows = Math.round((Number($('#cashOutflows').value) || 0) * 100) / 100;
    const sales = Math.max(0, closingCash + cashOutflows - openingCash);
    const allocation = calculateAllocation(sales);
    $('#openingCashPreview').textContent = currency.format(openingCash);
    $('#netSalesPreview').textContent = currency.format(sales);
    $('#capitalPreview').textContent = currency.format(allocation.capital);
    $('#personalPreview').textContent = currency.format(allocation.personal);
  }
  function calculateAllocation(sales) {
    // Trabajar en centavos evita diferencias de precisión binaria de Number.
    // Como sales / 1.61 equivale a centavos / 161, el residuo >= 81
    // representa una fracción mayor que 0.50 y se redondea hacia arriba.
    const salesCents = Math.max(0, Math.round((Number(sales) || 0) * 100));
    const wholeCapital = Math.floor(salesCents / 161);
    const remainder = salesCents % 161;
    const capital = Math.min(wholeCapital + (remainder >= 81 ? 1 : 0), Math.floor(salesCents / 100));
    return {capital, personal: (salesCents - capital * 100) / 100};
  }
  function setProgress(element, value, total) {
    const percent = total > 0 ? Math.max(0, Math.min(100, value * 100 / total)) : 0;
    element.style.width = `${percent}%`;
    return Math.round(percent);
  }
  function renderMovementList(items) {
    const list = $('#movementList');
    list.replaceChildren();
    $('#emptyMovements').hidden = items.length > 0;
    for (const item of items) {
      const row = document.createElement('article'); row.className = 'movement-item';
      const title = document.createElement('strong'); title.textContent = item.tipo;
      const amount = document.createElement('span'); amount.className = 'movement-amount'; amount.textContent = currency.format(item.monto);
      const concept = document.createElement('small'); concept.textContent = item.concepto;
      const meta = document.createElement('div'); meta.className = 'movement-meta';
      const date = document.createElement('small'); date.textContent = item.fecha;
      const responsible = document.createElement('small'); responsible.textContent = item.responsable || 'Responsable no disponible';
      meta.append(date, responsible); row.append(title, amount, concept);
      if (item.capital != null && item.ganancia != null) {
        const split = document.createElement('div'); split.className = 'movement-split';
        const capital = document.createElement('span'); capital.textContent = `Capital ${currency.format(item.capital)}`;
        const gain = document.createElement('span'); gain.textContent = `Ganancia ${currency.format(item.ganancia)}`;
        split.append(capital, gain); row.append(split);
      }
      row.append(meta); list.append(row);
    }
  }
  function render(data) {
    summary = data;
    $('#monthLabel').textContent = new Date(`${data.mes}-01T12:00:00`).toLocaleDateString('es-GT', {month:'long', year:'numeric'});
    $('#monthChip').textContent = new Date(`${data.mes}-01T12:00:00`).toLocaleDateString('es-GT', {month:'long'});
    $('#capitalBalance').textContent = currency.format(data.capital);
    $('#personalBalance').textContent = currency.format(data.personal);
    $('#capitalDeposited').textContent = currency.format(data.depositadoCapital);
    $('#personalDeposited').textContent = currency.format(data.depositadoPersonal);
    const capitalGenerated = Number(data.capitalGeneradoTotal) || 0;
    const depositedCapitalPct = setProgress($('#capitalProgress'), data.depositadoCapital, capitalGenerated);
    $('#capitalHint').textContent = `${depositedCapitalPct}%`;
    $('#obligationTotal').textContent = currency.format(data.obligacionesMes);
    $('#obligationPaid').textContent = currency.format(data.pagadoPersonal);
    $('#obligationPending').textContent = currency.format(Math.max(0, data.obligacionesMes - data.pagadoPersonal));
    $('#nextExpense').textContent = data.proximoGasto === 'Sin pendientes' ? data.proximoGasto : `${data.proximoGasto}${data.fechaProximoGasto ? ` · ${data.fechaProximoGasto}` : ''}`;
    const personalDepositPct = setProgress($('#personalProgress'), data.depositadoPersonal, data.personalGeneradoTotal);
    $('#personalProgressText').textContent = `${personalDepositPct}%`;
    $('#monthCapitalOutflows').textContent = currency.format(data.salidasCapitalMes || 0);
    $('#monthPersonalOutflows').textContent = currency.format(data.salidasPersonalMes || 0);
    $('#monthTotalOutflows').textContent = currency.format((Number(data.salidasCapitalMes) || 0) + (Number(data.salidasPersonalMes) || 0));
    $('#cashOutflows').value = Number(data.salidasCajaPendientes || 0).toFixed(2);
    updateAllocation();
    $('#personalCategory').replaceChildren(new Option('Selecciona un gasto pendiente', ''));
    for (const expense of data.gastos || []) {
      const option = new Option(`${expense.nombre} · pendiente ${currency.format(expense.saldo)}`, String(expense.idPeriodo));
      option.dataset.saldo = String(expense.saldo); $('#personalCategory').add(option);
    }
    renderMovementList(data.movimientos || []);
  }
  function renderClosingAudit(items) {
    const list = $('#closingAuditList');
    list.replaceChildren();
    $('#emptyClosings').hidden = items.length > 0;
    for (const item of items) {
      const row = document.createElement('article'); row.className = 'closing-audit-item';
      const header = document.createElement('div'); header.className = 'closing-audit-header';
      const date = document.createElement('strong'); date.textContent = item.fecha;
      const total = document.createElement('strong'); total.className = 'closing-audit-total'; total.textContent = `Total ${currency.format(item.ventas)}`;
      header.append(date, total);
      const split = document.createElement('div'); split.className = 'closing-audit-split';
      const capital = document.createElement('span'); capital.className = 'audit-capital'; capital.textContent = `Capital ${currency.format(item.capital)}`;
      const gain = document.createElement('span'); gain.className = 'audit-gain'; gain.textContent = `Ganancia ${currency.format(item.ganancia)}`;
      split.append(capital, gain);
      const owner = document.createElement('small'); owner.textContent = item.responsable || 'Responsable no disponible';
      row.append(header, split, owner); list.append(row);
    }
  }
  async function loadClosingAudit() {
    const params = new URLSearchParams();
    if ($('#auditFrom').value) params.set('desde', $('#auditFrom').value);
    if ($('#auditTo').value) params.set('hasta', $('#auditTo').value);
    const suffix = params.toString() ? `?${params}` : '';
    $('#auditMessage').textContent = 'Buscando cierres…';
    try {
      const data = await api(`/api/finanzas/cierres${suffix}`);
      renderClosingAudit(data.cierres || []);
      $('#auditMessage').textContent = `Mostrando ${data.cierres?.length || 0} cierres (máximo 10).`;
    } catch (error) { $('#auditMessage').textContent = error.message; }
  }
  async function loadSummary() {
    showMessage(pageMessage, 'Actualizando resumen…');
    try { render(await api('/api/finanzas/resumen')); await loadClosingAudit(); showMessage(pageMessage, 'Resumen actualizado.', true); }
    catch (error) { showMessage(pageMessage, error.message); }
  }

  ['#openingCash', '#closingCash'].forEach(selector => $(selector).addEventListener('input', updateAllocation));
  $('#loginForm').addEventListener('submit', async event => {
    event.preventDefault(); const formElement = event.currentTarget; showMessage(loginMessage, 'Validando acceso…');
    const clave = new FormData(formElement).get('clave');
    try {
      const result = await api('/api/productos/sesion', {method:'POST', body:JSON.stringify({clave})});
      setSignedIn(true, result.nombre); showMessage(loginMessage, ''); await loadSummary();
    } catch (error) { showMessage(loginMessage, error.message); formElement.querySelector('input').focus(); }
  });
  $('#logoutButton').addEventListener('click', async () => {
    try { await api('/api/productos/sesion', {method:'DELETE'}); } catch {}
    setSignedIn(false); showMessage(loginMessage, 'Sesión cerrada.', true);
  });
  $('#refreshButton').addEventListener('click', loadSummary);
  $('#closingAuditForm').addEventListener('submit', async event => { event.preventDefault(); await loadClosingAudit(); });

  $('#closingForm').addEventListener('submit', async event => {
    event.preventDefault();
    const formElement = event.currentTarget;
    const openingCash = Math.round(Number($('#openingCash').value) * 100) / 100;
    const countedCash = Math.round(Number($('#closingCash').value) * 100) / 100;
    const cashOutflows = Math.round(Number($('#cashOutflows').value) * 100) / 100;
    const sales = countedCash + cashOutflows - openingCash;
    if (sales < 0) { showMessage(pageMessage, 'El efectivo inicial supera el efectivo contado más las salidas. Revisa las cantidades.'); return; }
    const allocation = calculateAllocation(sales);
    const expectedOutflows = Number($('#cashOutflows').value) || 0;
    const closeDate = new Date().toLocaleString('es-GT');
    const confirmed = window.confirm(`Confirma el cierre de ${closeDate}:\n\nEfectivo inicial: ${currency.format(openingCash)}\nEfectivo contado: ${currency.format(countedCash)}\nSalidas pagadas desde caja: ${currency.format(expectedOutflows)}\nTotal vendido: ${currency.format(sales)}\nCapital: ${currency.format(allocation.capital)}\nGanancia: ${currency.format(allocation.personal)}\n\n¿Registrar este cierre?`);
    if (!confirmed) return;
    showMessage(pageMessage, 'Guardando cierre…');
    const submit = $('#saveClosing'); submit.disabled = true;
    try {
      const result = await api('/api/finanzas/cierres', {method:'POST', body:JSON.stringify({efectivoInicial:openingCash, efectivoContado:countedCash, salidasCajaEsperadas:expectedOutflows})});
      showMessage(pageMessage, `Cierre guardado. Ventas ${currency.format(result.ventas)} · Capital ${currency.format(result.capital)} · Ganancia ${currency.format(result.personal)}.`, true);
      $('#closingCash').value = ''; updateAllocation(); await loadSummary();
    } catch (error) { showMessage(pageMessage, error.message); }
    finally { submit.disabled = false; }
  });

  $('#depositForm').addEventListener('submit', async event => {
    event.preventDefault(); const formElement = event.currentTarget; showMessage(pageMessage, 'Guardando depósito…');
    const form = new FormData(formElement);
    try {
      await api('/api/finanzas/depositos', {method:'POST', body:JSON.stringify({cuenta:form.get('cuenta'), monto:Number(form.get('monto')), comprobante:String(form.get('comprobante') || '').trim(), fecha:form.get('fecha')})});
      formElement.reset(); $('#depositDate').value = localToday(); $('#depositResponsible').textContent = $('#responsibleName').textContent;
      showMessage(pageMessage, 'Depósito registrado con tu usuario y comprobante.', true); await loadSummary();
    } catch (error) { showMessage(pageMessage, error.message); }
  });

  function syncExpenseFields() {
    const personal = $('#expenseType').value === 'Personal';
    $('#personalCategoryField').hidden = !personal;
    $('#expenseConceptField').hidden = personal;
    $('#personalCategory').required = personal;
    $('#expenseConceptField input').required = !personal;
    $('#expenseAccount').value = personal ? 'Personal' : 'Capital';
    updateExpenseHelp();
  }
  function updateExpenseHelp() {
    const source = $('#expenseAccount').value === 'Personal' ? 'ganancia personal' : 'capital';
    const purpose = $('#expenseType').value === 'Personal' ? 'se asocia al gasto personal seleccionado' : 'se guarda con el concepto indicado';
    const cash = $('#paidFromCash').checked ? 'y se suma automáticamente a las salidas de caja del cierre' : 'y no reduce el efectivo contado en caja';
    $('#expenseHelp').textContent = `Se descontará de ${source}, ${purpose}, ${cash}.`;
  }
  $('#expenseType').addEventListener('change', syncExpenseFields);
  $('#expenseAccount').addEventListener('change', updateExpenseHelp);
  $('#paidFromCash').addEventListener('change', updateExpenseHelp);
  $('#expenseForm').addEventListener('submit', async event => {
    event.preventDefault(); const formElement = event.currentTarget; showMessage(pageMessage, 'Guardando salida…');
    const form = new FormData(formElement);
    const selected = $('#personalCategory').selectedOptions[0];
    const amount = Number(form.get('monto'));
    if (form.get('tipo') === 'Personal' && selected?.dataset.saldo && amount > Number(selected.dataset.saldo)) {
      showMessage(pageMessage, 'El monto supera el saldo pendiente de ese gasto personal.'); return;
    }
    try {
      await api('/api/finanzas/salidas', {method:'POST', body:JSON.stringify({tipo:form.get('tipo'), cuentaOrigen:form.get('cuentaOrigen'), pagadoDesdeCaja:form.has('pagadoDesdeCaja'), idPeriodoGastoPersonal:Number(form.get('idPeriodoGastoPersonal')) || 0, monto:amount, concepto:String(form.get('concepto') || '').trim(), fecha:form.get('fecha')})});
      formElement.reset(); $('#expenseDate').value = localToday(); syncExpenseFields();
      showMessage(pageMessage, 'Salida registrada y asociada a la cuenta correcta.', true); await loadSummary();
    } catch (error) { showMessage(pageMessage, error.message); }
  });

  async function initialize() {
    const today = localToday(); $('#depositDate').value = today; $('#expenseDate').value = today; updateAllocation();
    syncExpenseFields();
    try {
      const status = await api('/api/productos/sesion');
      if (status.autenticado) { setSignedIn(true, status.nombre || 'Usuario'); await loadSummary(); }
      else setSignedIn(false);
    } catch (error) { setSignedIn(false); showMessage(loginMessage, error.message); }
  }
  initialize();
})();
