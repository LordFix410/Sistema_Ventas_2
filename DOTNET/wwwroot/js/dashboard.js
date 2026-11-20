(() => {
  const $ = selector => document.querySelector(selector);
  const loginPanel = $('#loginPanel');
  const dashboardApp = $('#dashboardApp');
  const loginMessage = $('#loginMessage');
  const pageMessage = $('#pageMessage');
  const currency = new Intl.NumberFormat('es-GT', {style: 'currency', currency: 'GTQ'});

  function message(element, text, success = false) {
    element.textContent = text || '';
    element.classList.toggle('success', success);
  }
  async function api(url, options = {}) {
    const response = await fetch(url, {
      credentials: 'same-origin',
      headers: {'Content-Type': 'application/json', ...(options.headers || {})},
      ...options
    });
    const data = response.status === 204 ? null : await response.json().catch(() => ({}));
    if (!response.ok) throw new Error(data?.mensaje || 'No se pudo consultar el resumen.');
    return data;
  }
  function setSignedIn(signedIn, name = '') {
    loginPanel.hidden = signedIn;
    dashboardApp.hidden = !signedIn;
    if (signedIn) $('#responsibleName').textContent = name;
    else $('#loginForm').reset();
  }
  function progress(element, amount, total) {
    const percent = total > 0 ? Math.max(0, Math.min(100, amount * 100 / total)) : 0;
    element.style.width = `${percent}%`;
    return Math.round(percent);
  }
  function render(data) {
    const monthDate = new Date(`${data.mes}-01T12:00:00`);
    $('#monthLabel').textContent = monthDate.toLocaleDateString('es-GT', {month: 'long', year: 'numeric'});
    $('#monthChip').textContent = monthDate.toLocaleDateString('es-GT', {month: 'long'});
    $('#monthSales').textContent = currency.format(data.ventasMesTotal);
    $('#monthCapital').textContent = currency.format(data.capitalGeneradoMes);
    $('#monthPersonal').textContent = currency.format(data.asignadoPersonalMes);
    const sales = Number(data.ventasMesTotal) || 0;
    const capitalPercent = sales > 0 ? Math.round((Number(data.capitalGeneradoMes) || 0) * 100 / sales) : 0;
    const personalPercent = sales > 0 ? Math.round((Number(data.asignadoPersonalMes) || 0) * 100 / sales) : 0;
    $('#capitalPercent').textContent = `${capitalPercent}%`;
    $('#personalPercent').textContent = `${personalPercent}%`;
    progress($('#capitalBar'), capitalPercent, 100);
    progress($('#personalBar'), personalPercent, 100);

    $('#capitalBalance').textContent = currency.format(data.capital);
    $('#personalBalance').textContent = currency.format(data.personal);
    $('#capitalDeposited').textContent = currency.format(data.depositadoCapital);
    $('#personalDeposited').textContent = currency.format(data.depositadoPersonal);
    const capitalDepositPct = progress($('#capitalProgress'), data.depositadoCapital, data.capitalGeneradoTotal);
    const personalDepositPct = progress($('#personalProgress'), data.depositadoPersonal, data.personalGeneradoTotal);
    $('#capitalHint').textContent = `${capitalDepositPct}%`;
    $('#personalProgress').closest('.account-card').querySelector('small').textContent = `${personalDepositPct}%`;

    $('#obligationTotal').textContent = currency.format(data.obligacionesMes);
    $('#obligationPaid').textContent = currency.format(data.pagadoPersonal);
    $('#obligationPending').textContent = currency.format(Math.max(0, data.obligacionesMes - data.pagadoPersonal));
    $('#monthCapitalOutflows').textContent = currency.format(data.salidasCapitalMes || 0);
    $('#monthPersonalOutflows').textContent = currency.format(data.salidasPersonalMes || 0);
    $('#monthTotalOutflows').textContent = currency.format((Number(data.salidasCapitalMes) || 0) + (Number(data.salidasPersonalMes) || 0));
    $('#nextExpense').textContent = data.proximoGasto === 'Sin pendientes'
      ? data.proximoGasto
      : `${data.proximoGasto}${data.fechaProximoGasto ? ` · ${data.fechaProximoGasto}` : ''}`;
  }
  async function loadSummary() {
    message(pageMessage, 'Actualizando resumen…');
    try {
      render(await api('/api/finanzas/resumen'));
      message(pageMessage, 'Resumen actualizado.', true);
    } catch (error) {
      message(pageMessage, error.message);
    }
  }

  $('#loginForm').addEventListener('submit', async event => {
    event.preventDefault();
    const form = event.currentTarget;
    message(loginMessage, 'Validando acceso…');
    try {
      const result = await api('/api/productos/sesion', {
        method: 'POST',
        body: JSON.stringify({clave: new FormData(form).get('clave')})
      });
      setSignedIn(true, result.nombre || 'Usuario');
      await loadSummary();
    } catch (error) {
      message(loginMessage, error.message);
      form.querySelector('input').focus();
    }
  });
  $('#logoutButton').addEventListener('click', async () => {
    try { await api('/api/productos/sesion', {method: 'DELETE'}); } catch {}
    setSignedIn(false);
  });
  $('#refreshButton').addEventListener('click', loadSummary);

  async function initialize() {
    try {
      const status = await api('/api/productos/sesion');
      if (status.autenticado) {
        setSignedIn(true, status.nombre || 'Usuario');
        await loadSummary();
      } else setSignedIn(false);
    } catch (error) {
      setSignedIn(false);
      message(loginMessage, error.message);
    }
  }
  initialize();
})();
