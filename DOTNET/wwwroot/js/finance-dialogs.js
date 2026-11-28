(() => {
  const dialog = document.createElement('dialog');
  dialog.className = 'finance-message-dialog';
  dialog.setAttribute('aria-labelledby', 'financeDialogTitle');

  const form = document.createElement('form');
  form.method = 'dialog';
  const eyebrow = document.createElement('p');
  eyebrow.className = 'eyebrow';
  const title = document.createElement('h2');
  title.id = 'financeDialogTitle';
  const message = document.createElement('p');
  message.className = 'finance-dialog-message';
  const details = document.createElement('dl');
  details.className = 'finance-dialog-details';
  const fields = document.createElement('div');
  fields.className = 'finance-dialog-fields';
  const note = document.createElement('p');
  note.className = 'finance-dialog-note';
  const actions = document.createElement('div');
  actions.className = 'finance-dialog-actions';
  const cancel = document.createElement('button');
  cancel.type = 'button';
  cancel.className = 'button button-quiet';
  const accept = document.createElement('button');
  accept.type = 'submit';
  accept.className = 'button button-primary';
  form.append(eyebrow, title, message, details, fields, note, actions);
  actions.append(cancel, accept);
  dialog.append(form);
  document.body.append(dialog);

  let resolveCurrent = null;
  let currentMode = 'confirm';

  function closeWith(value) {
    if (dialog.open) {
      dialog.dataset.result = JSON.stringify(value);
      dialog.close();
    }
  }

  dialog.addEventListener('close', () => {
    if (!resolveCurrent) return;
    let value = null;
    try { value = JSON.parse(dialog.dataset.result || 'null'); } catch {}
    delete dialog.dataset.result;
    const resolve = resolveCurrent;
    resolveCurrent = null;
    resolve(value);
  });
  dialog.addEventListener('cancel', event => {
    event.preventDefault();
    closeWith(null);
  });
  dialog.addEventListener('click', event => {
    if (event.target === dialog) closeWith(null);
  });
  cancel.addEventListener('click', () => closeWith(null));
  form.addEventListener('submit', event => {
    event.preventDefault();
    if (currentMode === 'fields') {
      if (!form.reportValidity()) return;
      const values = {};
      fields.querySelectorAll('input, select, textarea').forEach(input => { values[input.name] = input.value; });
      closeWith(values);
      return;
    }
    closeWith(true);
  });

  function open(options = {}, mode = 'confirm') {
    if (resolveCurrent) closeWith(null);
    currentMode = mode;
    eyebrow.textContent = options.eyebrow || (mode === 'confirm' ? 'REVISA ANTES DE GUARDAR' : 'INFORMACIÓN');
    title.textContent = options.title || 'Confirmar';
    message.textContent = options.message || '';
    message.hidden = !options.message;
    details.replaceChildren();
    for (const item of options.details || []) {
      const row = document.createElement('div');
      const label = document.createElement('dt');
      label.textContent = item.label || '';
      const value = document.createElement('dd');
      value.textContent = item.value ?? '';
      row.append(label, value);
      details.append(row);
    }
    details.hidden = details.childElementCount === 0;
    fields.replaceChildren();
    for (const field of options.fields || []) {
      const label = document.createElement('label');
      label.textContent = field.label || field.name;
      const input = document.createElement(field.multiline ? 'textarea' : 'input');
      input.name = field.name;
      if (!field.multiline) input.type = field.type || 'text';
      input.value = field.value ?? '';
      input.placeholder = field.placeholder || '';
      input.required = Boolean(field.required);
      if (field.min != null) input.min = String(field.min);
      if (field.max != null) input.max = String(field.max);
      if (field.step != null) input.step = String(field.step);
      if (field.inputmode) input.inputMode = field.inputmode;
      label.append(input);
      fields.append(label);
    }
    fields.hidden = fields.childElementCount === 0;
    note.textContent = options.note || '';
    note.hidden = !options.note;
    cancel.hidden = mode === 'alert';
    actions.classList.toggle('single-action', mode === 'alert');
    cancel.textContent = options.cancelText || 'Volver';
    accept.textContent = options.confirmText || (mode === 'alert' ? 'Aceptar' : 'Confirmar');
    accept.className = `button ${options.danger ? 'button-danger-quiet' : 'button-primary'}`;
    return new Promise(resolve => {
      resolveCurrent = resolve;
      dialog.showModal();
      const firstField = fields.querySelector('input, select, textarea');
      if (firstField) requestAnimationFrame(() => firstField.focus());
      else accept.focus();
    });
  }

  window.FinanceDialogs = {
    confirm: options => open(options, 'confirm').then(value => value === true),
    alert: options => open(options, 'alert').then(() => undefined),
    form: options => open(options, 'fields'),
    prompt: async options => {
      const result = await open({
        ...options,
        fields: [{ name: 'value', label: options.label || 'Motivo', type: options.type || 'text', value: options.value || '', placeholder: options.placeholder || '', required: options.required !== false, min: options.min, step: options.step, inputmode: options.inputmode }]
      }, 'fields');
      return result?.value ?? null;
    }
  };
})();
