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
    const banks = data.bancos || [];
    const bankTotal = banks.reduce((sum, bank) => sum + Number(bank.capital || 0) + Number(bank.ganancia || 0), 0);
    const cashCapital = Number(data.capitalDepositosMasCaja || 0) - Number(data.depositadoCapital || 0);
    const cashGain = Number(data.gananciaDepositosMasCaja || 0) - Number(data.depositadoPersonal || 0);
    const pendingCapital = Number(data.pendienteDepositoCapital || 0);
    const pendingGain = Number(data.pendienteDepositoGanancia || 0);
    $('#bankTotal').textContent = currency.format(bankTotal);
    $('#bankAndCashTotal').textContent = currency.format(bankTotal + cashCapital + cashGain);
    $('#pendingDepositCapital').textContent = currency.format(pendingCapital);
    $('#pendingDepositGain').textContent = currency.format(pendingGain);
    $('#pendingDepositTotal').textContent = currency.format(pendingCapital + pendingGain);
    $('#capitalBalance').textContent = currency.format(data.capital);
    $('#personalBalance').textContent = currency.format(data.personal);
    $('#capitalDeposited').textContent = currency.format(data.depositadoCapital);
    $('#personalDeposited').textContent = currency.format(data.depositadoPersonal);
    $('#capitalCashTotal').textContent = currency.format(data.capitalDepositosMasCaja);
    $('#personalCashTotal').textContent = currency.format(data.gananciaDepositosMasCaja);
    $('#monthCapitalOutflows').textContent = currency.format(data.salidasCapitalMes || 0);
    $('#monthPersonalOutflows').textContent = currency.format(data.salidasPersonalMes || 0);
    const bankList=$('#bankBalanceList');bankList.replaceChildren();
    if(!banks.length){const empty=document.createElement('p');empty.className='empty-state';empty.textContent='Todavía no hay bancos registrados.';bankList.append(empty);}
    for(const bank of banks){const row=document.createElement('article');row.className='bank-balance-item';const name=document.createElement('strong');name.textContent=bank.nombre;const total=document.createElement('strong');total.textContent=currency.format(Number(bank.capital)+Number(bank.ganancia));const split=document.createElement('small');split.textContent=`Capital ${currency.format(bank.capital)} · Ganancia ${currency.format(bank.ganancia)}`;row.append(name,total,split);bankList.append(row);}
    $('#cashCapitalActual').textContent=currency.format(cashCapital);
    $('#cashGainActual').textContent=currency.format(cashGain);
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
