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
  function render(data) {
    $('#monthLabel').textContent = new Date(`${data.mes}-01T12:00:00`).toLocaleDateString('es-GT', {month: 'long', year: 'numeric'});
    $('#monthSales').textContent = currency.format(data.ventasMesTotal);
    $('#capitalBalance').textContent = currency.format(data.capital);
    $('#personalBalance').textContent = currency.format(data.personal);
    $('#capitalDeposited').textContent = currency.format(data.depositadoCapital);
    $('#personalDeposited').textContent = currency.format(data.depositadoPersonal);
    $('#capitalCashTotal').textContent = currency.format(data.capitalDepositosMasCaja);
    $('#personalCashTotal').textContent = currency.format(data.gananciaDepositosMasCaja);
    $('#monthCapitalOutflows').textContent = currency.format(data.salidasCapitalMes || 0);
    $('#monthPersonalOutflows').textContent = currency.format(data.salidasPersonalMes || 0);
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
