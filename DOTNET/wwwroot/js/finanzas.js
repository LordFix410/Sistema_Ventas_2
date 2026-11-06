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
    const sales = Math.max(0, Number($('#salesAmount').value) || 0);
    const capital = Math.round((sales * .61 + Number.EPSILON) * 100) / 100;
    $('#capitalPreview').textContent = currency.format(capital);
    $('#personalPreview').textContent = currency.format(Math.round((sales - capital + Number.EPSILON) * 100) / 100);
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
      meta.append(date, responsible); row.append(title, amount, concept, meta); list.append(row);
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
    $('#capitalHint').textContent = `${depositedCapitalPct}% del capital acumulado aparece depositado`;
    $('#obligationTotal').textContent = currency.format(data.obligacionesMes);
    $('#obligationPaid').textContent = currency.format(data.pagadoPersonal);
    $('#obligationPending').textContent = currency.format(Math.max(0, data.obligacionesMes - data.pagadoPersonal));
    $('#nextExpense').textContent = data.proximoGasto === 'Sin pendientes' ? data.proximoGasto : `${data.proximoGasto}${data.fechaProximoGasto ? ` · ${data.fechaProximoGasto}` : ''}`;
    const personalPct = setProgress($('#personalProgress'), data.asignadoPersonalMes, data.obligacionesMes);
    $('#personalProgressText').textContent = `${personalPct}% de los gastos previstos del mes cubierto por cierres`;
    $('#personalCategory').replaceChildren(new Option('Selecciona un gasto pendiente', ''));
    for (const expense of data.gastos || []) {
      const option = new Option(`${expense.nombre} · pendiente ${currency.format(expense.saldo)}`, String(expense.idPeriodo));
      option.dataset.saldo = String(expense.saldo); $('#personalCategory').add(option);
    }
    renderMovementList(data.movimientos || []);
  }
  async function loadSummary() {
    showMessage(pageMessage, 'Actualizando resumen…');
    try { render(await api('/api/finanzas/resumen')); showMessage(pageMessage, 'Resumen actualizado.', true); }
    catch (error) { showMessage(pageMessage, error.message); }
  }

  $('#salesAmount').addEventListener('input', updateAllocation);
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

  $('#closingForm').addEventListener('submit', async event => {
    event.preventDefault(); const formElement = event.currentTarget; showMessage(pageMessage, 'Guardando cierre…');
    const submit = $('#saveClosing'); submit.disabled = true;
    try {
      const result = await api('/api/finanzas/cierres', {method:'POST', body:JSON.stringify({fecha:$('#closingDate').value, ventas:Number($('#salesAmount').value)})});
      showMessage(pageMessage, `Cierre guardado. Capital ${currency.format(result.capital)} · Personal ${currency.format(result.personal)}.`, true);
      $('#salesAmount').value = ''; $('#closingDate').value = localDateTime(); updateAllocation(); await loadSummary();
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
    $('#expenseHelp').textContent = personal
      ? 'La salida personal se resta del saldo personal y se aplica al gasto seleccionado.'
      : 'La salida de empresa se resta del capital. Escribe el concepto para dejarlo en auditoría.';
  }
  $('#expenseType').addEventListener('change', syncExpenseFields);
  $('#expenseForm').addEventListener('submit', async event => {
    event.preventDefault(); const formElement = event.currentTarget; showMessage(pageMessage, 'Guardando salida…');
    const form = new FormData(formElement);
    const selected = $('#personalCategory').selectedOptions[0];
    const amount = Number(form.get('monto'));
    if (form.get('tipo') === 'Personal' && selected?.dataset.saldo && amount > Number(selected.dataset.saldo)) {
      showMessage(pageMessage, 'El monto supera el saldo pendiente de ese gasto personal.'); return;
    }
    try {
      await api('/api/finanzas/salidas', {method:'POST', body:JSON.stringify({tipo:form.get('tipo'), idPeriodoGastoPersonal:Number(form.get('idPeriodoGastoPersonal')) || 0, monto:amount, concepto:String(form.get('concepto') || '').trim(), fecha:form.get('fecha')})});
      formElement.reset(); $('#expenseDate').value = localToday(); syncExpenseFields();
      showMessage(pageMessage, 'Salida registrada y asociada a la cuenta correcta.', true); await loadSummary();
    } catch (error) { showMessage(pageMessage, error.message); }
  });

  async function initialize() {
    const today = localToday(); $('#closingDate').value = localDateTime(); $('#depositDate').value = today; $('#expenseDate').value = today;
    syncExpenseFields();
    try {
      const status = await api('/api/productos/sesion');
      if (status.autenticado) { setSignedIn(true, status.nombre || 'Usuario'); await loadSummary(); }
      else setSignedIn(false);
    } catch (error) { setSignedIn(false); showMessage(loginMessage, error.message); }
  }
  initialize();
})();
