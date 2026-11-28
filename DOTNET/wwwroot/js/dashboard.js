(() => {
  const $ = selector => document.querySelector(selector);
  const loginPanel = $('#loginPanel');
  const loginForm = $('#loginForm');
  const dashboardApp = $('#dashboardApp');
  const loginMessage = $('#loginMessage');
  const pageMessage = $('#pageMessage');
  const currency = new Intl.NumberFormat('es-GT', {style: 'currency', currency: 'GTQ'});
  const gainBank = /bam/i;
  const capitalBank = /banrural/i;
  const centTolerance = 0.005;

  const number = value => Number(value || 0);
  const money = value => currency.format(number(value));

  function message(element, text, success = false) {
    element.textContent = text || '';
    element.classList.toggle('success', success);
  }

  function setSignedIn(signedIn, name = '') {
    loginPanel.hidden = signedIn;
    dashboardApp.hidden = !signedIn;
    if (signedIn) $('#responsibleName').textContent = name;
    else loginForm.reset();
  }

  async function api(url, options = {}) {
    const response = await fetch(url, {
      credentials: 'same-origin',
      headers: {'Content-Type': 'application/json', ...(options.headers || {})},
      ...options
    });
    const data = response.status === 204 ? null : await response.json().catch(() => ({}));
    if (!response.ok) {
      const error = data?.mensaje || 'No se pudo consultar el resumen.';
      if ((response.status === 401 || response.status === 403) && url !== '/api/productos/sesion') {
        setSignedIn(false);
        message(loginMessage, error);
      }
      throw new Error(error);
    }
    return data;
  }

  function cell(className, label, value) {
    const box = document.createElement('div');
    box.className = 'cell ' + className + (number(value) === 0 ? ' is-zero' : '');
    const small = document.createElement('small');
    small.textContent = label;
    const bold = document.createElement('b');
    bold.textContent = money(value);
    box.append(small, bold);
    return box;
  }

  function ledgerRow(name, capital, gain, isSum = false) {
    const row = document.createElement('article');
    row.className = 'ledger-row' + (isSum ? ' is-sum' : '');
    const title = document.createElement('h3');
    title.className = 'ln';
    title.textContent = name;
    row.append(title, cell('c', 'Capital', capital), cell('g', 'Ganancia', gain), cell('t', 'Total', number(capital) + number(gain)));
    return row;
  }

  function check(list, kind, text) {
    const item = document.createElement('li');
    item.className = kind;
    const mark = document.createElement('b');
    mark.className = 'mark';
    mark.textContent = kind === 'ok' ? '✓' : kind === 'warn' ? '!' : 'i';
    const span = document.createElement('span');
    span.textContent = text;
    item.append(mark, span);
    list.append(item);
  }

  function render(data) {
    if (data.mes) {
      $('#monthLabel').textContent = new Date(data.mes + '-01T12:00:00')
        .toLocaleDateString('es-GT', {month: 'long', year: 'numeric'});
    }

    const banks = Array.isArray(data.bancos) ? data.bancos : [];
    const sumCapital = banks.reduce((sum, bank) => sum + number(bank.capital), 0);
    const sumGain = banks.reduce((sum, bank) => sum + number(bank.ganancia), 0);
    const totalCapital = number(data.capitalDepositosMasCaja ?? data.capital);
    const totalGain = number(data.gananciaDepositosMasCaja ?? data.personal);
    const pendingCapital = number(data.pendienteDepositoCapital);
    const pendingGain = number(data.pendienteDepositoGanancia);
    const fixedCashCapital = totalCapital - sumCapital - pendingCapital;
    const fixedCashGain = totalGain - sumGain - pendingGain;

    $('#goalCapital').textContent = money(totalCapital);
    $('#goalGain').textContent = money(totalGain);

    const ledger = $('#ledgerList');
    ledger.replaceChildren();
    if (!banks.length) {
      const empty = document.createElement('p');
      empty.className = 'empty-state';
      empty.textContent = 'Todavía no hay bancos registrados.';
      ledger.append(empty);
    } else {
      banks.forEach(bank => ledger.append(ledgerRow(bank.nombre, bank.capital, bank.ganancia)));
    }
    ledger.append(
      ledgerRow('Suma de bancos', sumCapital, sumGain, true),
      ledgerRow('En caja (fijo)', fixedCashCapital, fixedCashGain),
      ledgerRow('Cierres sin depositar', pendingCapital, pendingGain),
      ledgerRow('Total real', totalCapital, totalGain, true)
    );

    const checks = $('#checkList');
    checks.replaceChildren();
    if (banks.length) {
      check(checks, 'info', 'Suma de bancos: ' + money(sumCapital + sumGain) + '. Compárala con los saldos de Banrural y BAM.');
    } else {
      check(checks, 'warn', 'No hay bancos registrados para comparar sus saldos.');
    }

    if (pendingCapital > centTolerance || pendingGain > centTolerance) {
      check(checks, 'warn', money(pendingCapital + pendingGain) + ' en cierres sin depositar (Capital ' + money(pendingCapital) + ' · Ganancia ' + money(pendingGain) + ').');
    } else if (pendingCapital < -centTolerance || pendingGain < -centTolerance) {
      check(checks, 'warn', 'Hay un saldo negativo en cierres pendientes. Revisa esos cierres antes de depositar.');
    } else {
      check(checks, 'ok', 'No hay cierres pendientes de depositar.');
    }

    const hasGainBank = banks.some(bank => gainBank.test(bank.nombre || ''));
    const hasCapitalBank = banks.some(bank => capitalBank.test(bank.nombre || ''));
    const gainOutsideBam = banks.filter(bank => !gainBank.test(bank.nombre || '')).reduce((sum, bank) => sum + number(bank.ganancia), 0);
    const capitalInsideBam = banks.filter(bank => gainBank.test(bank.nombre || '')).reduce((sum, bank) => sum + number(bank.capital), 0);

    if (!hasCapitalBank) check(checks, 'warn', 'No encuentro una cuenta llamada Banrural. Revisa el nombre del banco.');
    if (!hasGainBank) check(checks, 'warn', 'No encuentro una cuenta llamada BAM. Revisa el nombre del banco.');
    if (gainOutsideBam > centTolerance) check(checks, 'warn', 'Falta transferir ' + money(gainOutsideBam) + ' de ganancia desde Banrural hacia BAM.');
    if (capitalInsideBam > centTolerance) check(checks, 'warn', 'BAM tiene ' + money(capitalInsideBam) + ' de capital; verifica que se haya transferido a Banrural.');
    if (hasCapitalBank && hasGainBank && gainOutsideBam <= centTolerance && capitalInsideBam <= centTolerance) {
      check(checks, 'ok', 'La ganancia está en BAM y el capital en Banrural, según los movimientos registrados.');
    }
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

  loginForm.addEventListener('submit', async event => {
    event.preventDefault();
    message(loginMessage, 'Validando acceso…');
    try {
      const result = await api('/api/productos/sesion', {
        method: 'POST',
        body: JSON.stringify({
          clave: new FormData(loginForm).get('clave'),
          pagina: 'dashboard.html'
        })
      });
      setSignedIn(true, result.nombre || 'Usuario');
      await loadSummary();
    } catch (error) {
      message(loginMessage, error.message);
      loginForm.querySelector('input').focus();
    }
  });

  $('#logoutButton').addEventListener('click', async () => {
    try { await api('/api/productos/sesion', {method: 'DELETE'}); } catch {}
    setSignedIn(false);
    message(loginMessage, '');
  });

  $('#refreshButton').addEventListener('click', loadSummary);

  async function initialize() {
    try {
      const status = await api('/api/productos/sesion');
      if (status.autenticado) {
        setSignedIn(true, status.nombre || 'Usuario');
        await loadSummary();
      } else {
        setSignedIn(false);
      }
    } catch (error) {
      setSignedIn(false);
      message(loginMessage, error.message);
    }
  }

  initialize();
})();