(() => {
  const $ = selector => document.querySelector(selector);
  const currency = new Intl.NumberFormat('es-GT', { style: 'currency', currency: 'GTQ' });
  let snapshot = null;
  let namedBanksAvailable = false;

  const today = () => {
    const date = new Date();
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
  };

  function message(text, success = false) {
    $('#pageMessage').textContent = text || '';
    $('#pageMessage').classList.toggle('success', success);
  }

  async function api(url, options = {}) {
    const response = await fetch(url, {
      credentials: 'same-origin',
      headers: { 'Content-Type': 'application/json', ...(options.headers || {}) },
      ...options
    });
    const data = await response.json().catch(() => ({}));
    if (!response.ok) {
      if (response.status === 401 && url !== '/api/productos/sesion') show(false);
      throw new Error(data?.mensaje || 'No se pudo completar la solicitud.');
    }
    return data;
  }

  function show(authorized, name = '') {
    $('#loginPanel').hidden = authorized;
    $('#moduleApp').hidden = !authorized;
    $('#responsibleName').textContent = name;
  }

  function addInputCell(row, locationKey, locationName, fund, expected) {
    const cell = document.createElement('td');
    const label = document.createElement('label');
    label.className = 'reconciliation-input-label';
    label.textContent = fund;
    const input = document.createElement('input');
    input.type = 'number';
    input.min = '0';
    input.step = '0.01';
    input.inputMode = 'decimal';
    input.required = true;
    input.dataset.location = locationKey;
    input.dataset.fund = fund;
    input.dataset.expected = String(expected);
    input.placeholder = Number(expected).toFixed(2);
    input.setAttribute('aria-label', `${locationName}: saldo real de ${fund}`);
    input.addEventListener('input', updateTotals);
    label.append(input);
    cell.append(label);
    row.append(cell);
  }

  function addFixedCell(row, fund) {
    const cell = document.createElement('td');
    row.append(cell);
  }

  function addRow(body, name, locationKey, capital, gain, editableFunds) {
    const row = document.createElement('tr');
    const title = document.createElement('td');
    title.className = 'reconciliation-location';
    title.textContent = name;
    row.append(title);
    if (editableFunds.includes('Capital')) addInputCell(row, locationKey, name, 'Capital', capital);
    else addFixedCell(row, 'Capital');
    if (editableFunds.includes('Ganancia')) addInputCell(row, locationKey, name, 'Ganancia', gain);
    else addFixedCell(row, 'Ganancia');
    body.append(row);
  }

  function actualBalances() {
    const banrural = snapshot.bancos.find(bank => /banrural/i.test(bank.nombre));
    const bam = snapshot.bancos.find(bank => /\bbam\b/i.test(bank.nombre));
    const input = (location, fund) => Number($(`#balanceRows input[data-location="${location}"][data-fund="${fund}"]`).value);
    const bankBalances = snapshot.bancos.map(bank => {
      if (bank.idBanco === banrural.idBanco) return { idBanco: bank.idBanco, saldoCapital: input('banrural', 'Capital'), saldoGanancia: bank.ganancia };
      if (bank.idBanco === bam.idBanco) return { idBanco: bank.idBanco, saldoCapital: bank.capital, saldoGanancia: input('bam', 'Ganancia') };
      // Las cuentas adicionales, si existen, permanecen en el saldo que ya registra el sistema.
      return { idBanco: bank.idBanco, saldoCapital: bank.capital, saldoGanancia: bank.ganancia };
    });
    bankBalances.push({
      idBanco: null,
      saldoCapital: input('caja', 'Capital'),
      saldoGanancia: input('caja', 'Ganancia')
    });
    return bankBalances;
  }

  function calculateAdjustments() {
    const actual = actualBalances();
    let capital = 0;
    let gain = 0;
    for (const bank of snapshot.bancos) {
      const balance = actual.find(item => item.idBanco === bank.idBanco);
      capital += balance.saldoCapital - bank.capital;
      gain += balance.saldoGanancia - bank.ganancia;
    }
    const cash = actual.find(item => item.idBanco === null);
    capital += cash.saldoCapital - snapshot.caja.capital;
    gain += cash.saldoGanancia - snapshot.caja.ganancia;
    return { capital, gain };
  }

  function updateTotals() {
    const complete = [...document.querySelectorAll('#balanceRows input')].every(input => input.value !== '');
    if (!complete || !namedBanksAvailable) {
      $('#capitalDifference').textContent = 'Completa los saldos';
      $('#gainDifference').textContent = 'Completa los saldos';
      $('#totalDifference').textContent = 'Completa los saldos';
      return;
    }
    const adjustment = calculateAdjustments();
    $('#capitalDifference').textContent = currency.format(adjustment.capital);
    $('#gainDifference').textContent = currency.format(adjustment.gain);
    $('#totalDifference').textContent = currency.format(adjustment.capital + adjustment.gain);
  }

  function renderHistory(history) {
    const list = $('#reconciliationHistory');
    list.replaceChildren();
    if (!history.length) {
      const empty = document.createElement('p');
      empty.className = 'empty-state';
      empty.textContent = 'Todavía no hay conciliaciones registradas.';
      list.append(empty);
      return;
    }
    for (const record of history) {
      const article = document.createElement('article');
      article.className = 'closing-audit-item';
      const header = document.createElement('div');
      header.className = 'closing-audit-header';
      const title = document.createElement('strong');
      title.textContent = `Conciliación · ${record.fecha}`;
      const amount = document.createElement('strong');
      amount.textContent = `Faltante ${currency.format(record.faltante)} · Sobrante ${currency.format(record.sobrante)}`;
      header.append(title, amount);
      const reason = document.createElement('span');
      reason.textContent = record.motivo;
      const owner = document.createElement('small');
      owner.textContent = `${record.responsable || 'Usuario'} · registrada ${record.registrada}`;
      article.append(header, reason, owner);

      const details = document.createElement('details');
      details.className = 'reconciliation-history-disclosure';
      const summary = document.createElement('summary');
      summary.textContent = 'Ver desglose por banco y efectivo';
      const detailList = document.createElement('div');
      detailList.className = 'reconciliation-history-details';
      for (const detail of (record.detalles || [])) {
        const item = document.createElement('span');
        const adjustment = Number(detail.Ajuste) || 0;
        item.textContent = `${detail.Ubicacion} · ${detail.Fondo}: ${currency.format(Number(detail.Real) || 0)} (${adjustment > 0 ? '+' : ''}${currency.format(adjustment)})`;
        detailList.append(item);
      }
      details.append(summary, detailList);
      article.append(details);
      list.append(article);
    }
  }

  function renderClosings(closings) {
    const list = $('#closuresToMark');
    list.replaceChildren();
    if (!closings.length) {
      const empty = document.createElement('p');
      empty.className = 'empty-state';
      empty.textContent = 'No hay cierres sin boleta vinculada.';
      list.append(empty);
      return;
    }
    for (const closing of closings) {
      const row = document.createElement('label');
      row.className = 'reconciliation-closure-item';
      row.dataset.closureId = String(closing.id);
      row.dataset.date = closing.fecha;
      const checkbox = document.createElement('input');
      checkbox.type = 'checkbox';
      checkbox.checked = Boolean(closing.marcadoComoDepositado);
      checkbox.setAttribute('aria-label', `Marcar cierre del ${closing.fecha} como ya depositado`);
      const info = document.createElement('span');
      info.className = 'reconciliation-closure-info';
      const date = document.createElement('strong');
      date.textContent = new Date(`${closing.fecha}T00:00:00`).toLocaleDateString('es-GT');
      const details = document.createElement('small');
      details.textContent = `Ventas ${currency.format(closing.ventas)} · Capital ${currency.format(closing.capital)} · Ganancia ${currency.format(closing.ganancia)}`;
      info.append(date, details);
      const state = document.createElement('small');
      state.className = 'reconciliation-closure-state';
      state.textContent = checkbox.checked ? 'Marcado como cubierto' : 'Pendiente';
      checkbox.addEventListener('change', () => { state.textContent = checkbox.checked ? 'Marcado como cubierto' : 'Pendiente'; });
      row.append(checkbox, info, state);
      list.append(row);
    }
  }

  function selectedClosingStates(cutoffDate) {
    return [...document.querySelectorAll('#closuresToMark .reconciliation-closure-item')]
      .filter(row => row.dataset.date <= cutoffDate)
      .map(row => ({
        idCierre: Number(row.dataset.closureId),
        fecha: row.dataset.date,
        yaDepositado: row.querySelector('input').checked
      }));
  }

  function render(data) {
    snapshot = data;
    const banrural = data.bancos.find(bank => /banrural/i.test(bank.nombre));
    const bam = data.bancos.find(bank => /\bbam\b/i.test(bank.nombre));
    namedBanksAvailable = Boolean(banrural && bam && banrural.idBanco !== bam.idBanco);
    const body = $('#balanceRows');
    body.replaceChildren();
    if (!namedBanksAvailable) {
      message('No encontré las cuentas Banrural y BAM activas. Revisa sus nombres en la sección de bancos.', false);
      $('#saveReconciliation').disabled = true;
      renderHistory(data.historial);
      return;
    }
    $('#saveReconciliation').disabled = false;
    addRow(body, 'Banrural', 'banrural', banrural.capital, banrural.ganancia, ['Capital']);
    addRow(body, 'BAM', 'bam', bam.capital, bam.ganancia, ['Ganancia']);
    addRow(body, 'Efectivo en caja', 'caja', data.caja.capital, data.caja.ganancia, ['Capital', 'Ganancia']);
    updateTotals();
    renderHistory(data.historial);
    renderClosings(data.cierresSinBoleta || []);
  }

  async function refresh() {
    try {
      render(await api('/api/finanzas/conciliaciones'));
      if (namedBanksAvailable) message('Ingresa solo los cuatro saldos que aparecen.', true);
    } catch (error) {
      message(error.message);
    }
  }

  $('#loginForm').addEventListener('submit', async event => {
    event.preventDefault();
    const loginForm = event.currentTarget;
    try {
      const data = await api('/api/productos/sesion', {
        method: 'POST',
        body: JSON.stringify({ clave: new FormData(loginForm).get('clave') })
      });
      show(true, data.nombre);
      await refresh();
    } catch (error) {
      $('#loginMessage').textContent = error.message;
    }
  });

  $('#logoutButton').addEventListener('click', async () => {
    try { await api('/api/productos/sesion', { method: 'DELETE' }); } catch { /* Cerrar sesión local igualmente. */ }
    show(false);
  });
  $('#refreshButton').addEventListener('click', refresh);

  $('#reconciliationForm').addEventListener('submit', async event => {
    event.preventDefault();
    const reconciliationForm = event.currentTarget;
    if (!snapshot || !namedBanksAvailable || !reconciliationForm.reportValidity()) return;
    const form = new FormData(reconciliationForm);
    const saldos = actualBalances();
    const adjustment = calculateAdjustments();
    const cierres = selectedClosingStates(String(form.get('fecha')));
    const markedAsDeposited = cierres.filter(closing => closing.yaDepositado).length;
    const keptPending = cierres.length - markedAsDeposited;
    const confirmation = [
      `Guardar conciliación del ${form.get('fecha')}?`,
      '',
      `Ajuste Capital: ${currency.format(adjustment.capital)}`,
      `Ajuste Ganancia: ${currency.format(adjustment.gain)}`,
      `Diferencia total: ${currency.format(adjustment.capital + adjustment.gain)}`,
      `Cierres marcados como depositados: ${markedAsDeposited}`,
      `Cierres que seguirán pendientes: ${keptPending}`,
      '',
      'Solo cambiarán los cierres que marcaste. Los demás seguirán disponibles para registrar su depósito.'
    ].join('\n');
    if (!window.confirm(confirmation)) return;

    $('#saveReconciliation').disabled = true;
    try {
      await api('/api/finanzas/conciliaciones', {
        method: 'POST',
        body: JSON.stringify({ fecha: form.get('fecha'), motivo: String(form.get('motivo')).trim(), saldos, cierres })
      });
      message('Conciliación guardada en la auditoría. El historial se conservó.', true);
      reconciliationForm.reset();
      reconciliationForm.querySelector('[name=fecha]').value = today();
      await refresh();
    } catch (error) {
      message(error.message);
    } finally {
      $('#saveReconciliation').disabled = false;
    }
  });

  $('#reconciliationForm [name=fecha]').value = today();
  (async () => {
    try {
      const session = await api('/api/productos/sesion');
      if (session.autenticado) {
        show(true, session.nombre);
        await refresh();
      } else show(false);
    } catch (error) {
      show(false);
      $('#loginMessage').textContent = error.message;
    }
  })();
})();
