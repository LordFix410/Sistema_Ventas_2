(() => {
  const $ = s => document.querySelector(s), currency = new Intl.NumberFormat('es-GT',{style:'currency',currency:'GTQ'});
  let data = {bancos:[],cierres:[],fondosBanco:[]};
  function scrollToRequestedSection(){if(window.location.hash==='#transferencias')requestAnimationFrame(()=>$('#transferencias')?.scrollIntoView({behavior:'smooth',block:'start'}));}
  const today = () => { const d=new Date(); return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`; };
  const message = (s,ok=false) => { $('#pageMessage').textContent=s||''; $('#pageMessage').classList.toggle('success',ok); };
  async function api(url,options={}) { const r=await fetch(url,{credentials:'same-origin',headers:{'Content-Type':'application/json',...(options.headers||{})},...options}); const d=r.status===204?null:await r.json().catch(()=>({})); if(!r.ok){if((r.status===401||r.status===403)&&url!='/api/productos/sesion')showLogin(false);throw new Error(d?.mensaje||'No se pudo completar la solicitud.');} return d; }
  function showLogin(ok,name='') { $('#loginPanel').hidden=ok; $('#moduleApp').hidden=!ok; $('#responsibleName').textContent=name; }
  function selectedIds(){return [...document.querySelectorAll('#pendingClosings input:checked')].map(x=>Number(x.value));}
  function renderClosings(){const body=$('#pendingClosings');body.replaceChildren();$('#noClosings').hidden=data.cierres.length>0; for(const c of data.cierres){const tr=document.createElement('tr');const pick=document.createElement('td'),box=document.createElement('input');box.type='checkbox';box.value=c.id;box.setAttribute('aria-label',`Seleccionar cierre del ${c.fecha}`);pick.append(box);const label=document.createElement('td');label.textContent=`${c.fecha}${c.responsable?` · ${c.responsable}`:''}`;const sale=document.createElement('td');sale.textContent=currency.format(c.ventas);const cap=document.createElement('td');cap.textContent=currency.format(c.capitalNeto);cap.title=`Generó ${currency.format(c.capital)} y tuvo ${currency.format(c.salidasCapital)} en salidas de Capital pagadas en efectivo.`;const gain=document.createElement('td');gain.textContent=currency.format(c.gananciaNeta);gain.title=`Generó ${currency.format(c.ganancia)} y tuvo ${currency.format(c.salidasGanancia)} en salidas de Ganancia pagadas en efectivo.`;tr.append(pick,label,sale,cap,gain);body.append(tr);box.addEventListener('change',updateBatch);}}
  function updateBatch(){const ids=new Set(selectedIds());const picked=data.cierres.filter(c=>ids.has(c.id));$('#batchTotal').textContent=currency.format(picked.reduce((s,c)=>s+c.capitalNeto+c.gananciaNeta,0));$('#batchCapital').textContent=currency.format(picked.reduce((s,c)=>s+c.capitalNeto,0));$('#batchGain').textContent=currency.format(picked.reduce((s,c)=>s+c.gananciaNeta,0));}
  function fillBankSelect(select,placeholder=false,excludedId=null){select.replaceChildren();if(placeholder)select.add(new Option('Selecciona banco',''));for(const b of data.bancos)if(b.id!==excludedId)select.add(new Option(b.nombre,String(b.id)));}
  function renderTransferDestinations(){const origin=Number($('#transferForm [name=idBancoOrigen]').value);const destination=$('#transferForm [name=idBancoDestino]');const previous=Number(destination.value);fillBankSelect(destination,true,origin||null);if(previous&&previous!==origin&&data.bancos.some(b=>b.id===previous))destination.value=String(previous);else if(origin){const firstOther=data.bancos.find(b=>b.id!==origin);if(firstOther)destination.value=String(firstOther.id);}}
  function renderBanks(){const list=$('#bankBalances');list.replaceChildren();if(!data.bancos.length){const p=document.createElement('p');p.className='empty-state';p.textContent='Agrega tus bancos e ingresa el saldo inicial separado entre Capital y Ganancia.';list.append(p);}for(const b of data.bancos){const row=document.createElement('article');row.className='bank-balance-item';const name=document.createElement('strong');name.textContent=b.nombre;const balance=document.createElement('strong');balance.textContent=currency.format(b.saldo);const opening=document.createElement('small');opening.textContent=`Inicio: Capital ${currency.format(b.saldoCapitalInicial)} · Ganancia ${currency.format(b.saldoGananciaInicial)}`;row.append(name,balance,opening);list.append(row);}const banrural=data.bancos.filter(b=>/banrural/i.test(b.nombre));const bam=data.bancos.filter(b=>/\bbam\b/i.test(b.nombre));const depositBank=$('#depositForm [name=idBanco]');fillBankSelect(depositBank,true);if(banrural.length===1)depositBank.value=String(banrural[0].id);const transferOrigin=$('#transferForm [name=idBancoOrigen]');fillBankSelect(transferOrigin,true);if(banrural.length===1)transferOrigin.value=String(banrural[0].id);renderTransferDestinations();if(bam.length===1&&(!banrural.length||bam[0].id!==banrural[0].id))$('#transferForm [name=idBancoDestino]').value=String(bam[0].id);}
  function renderFunds(){const box=$('#pendingFunds');box.replaceChildren();for(const b of data.fondosBanco){const row=document.createElement('div');row.className='pending-fund-item';const name=document.createElement('strong');name.textContent=b.banco;const amounts=document.createElement('span');amounts.textContent=`Saldo actual · Capital ${currency.format(b.capital)} · Ganancia ${currency.format(b.ganancia)}`;row.append(name,amounts);box.append(row);}updateTransferLimit();}
  function updateTransferLimit(){const from=Number($('#transferForm [name=idBancoOrigen]').value);const fund=$('#transferForm [name=fondo]').value;const bank=data.fondosBanco.find(x=>x.idBanco===from);const available=bank?(fund==='Capital'?bank.capital:bank.ganancia):0;const input=$('#transferForm [name=monto]');input.removeAttribute('max');input.placeholder=`Disponible: ${currency.format(available)}`;}
  function renderMovements(items){const list=$('#movementList');list.replaceChildren();if(!items.length){const e=document.createElement('p');e.className='empty-state';e.textContent='Todavía no hay depósitos ni transferencias.';list.append(e);return;}for(const item of items){const row=document.createElement('article');row.className='closing-audit-item';const h=document.createElement('div');h.className='closing-audit-header';const title=document.createElement('strong');title.textContent=`${item.tipo} · ${item.fecha}`;const amount=document.createElement('strong');amount.textContent=currency.format(item.monto);h.append(title,amount);const detail=document.createElement('span');detail.textContent=`${item.banco} · ${item.concepto} · ${item.detalle}`;const owner=document.createElement('small');owner.textContent=item.responsable||'';row.append(h,detail,owner);if(item.tipo==='Depósito'&&!item.anulado){const cancel=document.createElement('button');cancel.type='button';cancel.className='button button-danger-quiet';cancel.textContent='Anular';cancel.addEventListener('click',async()=>{const reason = await FinanceDialogs.prompt({ eyebrow: "AUDITORÍA", title: "Motivo para anular la boleta", message: "Escribe por qué se anula este depósito.", label: "Motivo", placeholder: "Ej. Se ingresó mal el comprobante", confirmText: "Continuar" });
if (!reason?.trim()) return;
const confirmed = await FinanceDialogs.confirm({ title: "Anular depósito", details: [{ label: "Monto", value: currency.format(item.monto) }], note: "La anulación quedará en el historial. Solo se permite antes de que el banco tenga transferencias o salidas registradas.", confirmText: "Anular depósito", danger: true });
if (!confirmed) return;
try{await api(`/api/finanzas/operaciones/depositos/${item.id}/anular`,{method:'POST',body:JSON.stringify({motivo:reason.trim()})});message('Depósito anulado y conservado en auditoría.',true);await refresh();}catch(x){message(x.message);}});row.append(cancel);}list.append(row);}}
  async function refresh(){try{data=await api('/api/finanzas/operaciones/depositos');renderClosings();renderBanks();renderFunds();updateBatch();renderMovements((await api('/api/finanzas/operaciones/movimientos?tipo=depositos')).movimientos||[]);}catch(e){message(e.message);}}
  $('#loginForm').addEventListener('submit',async e=>{e.preventDefault();try{const d=await api('/api/productos/sesion',{method:'POST',body:JSON.stringify({clave:new FormData(e.currentTarget).get('clave'),pagina:'depositos.html'})});showLogin(true,d.nombre);await refresh();scrollToRequestedSection();}catch(x){$('#loginMessage').textContent=x.message;}});
  $('#logoutButton').addEventListener('click',async()=>{try{await api('/api/productos/sesion',{method:'DELETE'});}catch{}showLogin(false);});
  $('#refreshButton').addEventListener('click',refresh);
  $('#transferForm [name=idBancoOrigen]').addEventListener('change',()=>{renderTransferDestinations();updateTransferLimit();});$('#transferForm [name=fondo]').addEventListener('change',updateTransferLimit);
  $('#depositForm').addEventListener('submit',async e=>{e.preventDefault();const formElement=e.currentTarget;const f=new FormData(formElement),ids=selectedIds();if(!ids.length){message('Selecciona al menos un cierre para esta boleta.');return;}const picked=data.cierres.filter(c=>ids.includes(c.id)),total=picked.reduce((s,c)=>s+c.capitalNeto+c.gananciaNeta,0),capital=picked.reduce((s,c)=>s+c.capitalNeto,0),gain=picked.reduce((s,c)=>s+c.gananciaNeta,0);const bank = data.bancos.find(b => b.id === Number(f.get("idBanco")));
const confirmed = await FinanceDialogs.confirm({
  title: "Confirmar depósito",
  details: [
    { label: "Total de la boleta", value: currency.format(total) },
    { label: "Banco receptor", value: bank?.nombre || "Banco seleccionado" },
    { label: "Capital neto", value: currency.format(capital) },
    { label: "Ganancia neta", value: currency.format(gain) },
    { label: "Cierres incluidos", value: String(ids.length) }
  ],
  note: "El historial conservará las ventas y las salidas de cada cierre por separado.",
  confirmText: "Registrar depósito"
});
if (!confirmed) return;$('#saveDeposit').disabled=true;try{const r=await api('/api/finanzas/operaciones/depositos',{method:'POST',body:JSON.stringify({idBanco:Number(f.get('idBanco')),idsCierre:ids,fecha:f.get('fecha'),comprobante:String(f.get('comprobante')).trim()})});message(`Boleta ${currency.format(r.totalDepositado)} registrada · Capital ${currency.format(r.capital)} · Ganancia ${currency.format(r.ganancia)}.`,true);formElement.reset();formElement.querySelector('[name=fecha]').value=today();await refresh();}catch(x){message(x.message);}finally{$('#saveDeposit').disabled=false;}});
  $('#transferForm').addEventListener('submit',async e=>{e.preventDefault();const formElement=e.currentTarget,submitter=e.submitter;const f=new FormData(formElement),amount=Number(f.get('monto')),soloAuditoria=submitter?.id==='saveTransferAudit';const sourceId=Number(f.get('idBancoOrigen')),destinationId=Number(f.get('idBancoDestino'));const src=data.bancos.find(b=>b.id===sourceId),dst=data.bancos.find(b=>b.id===destinationId);if(!src||!dst){message('Selecciona banco de origen y destino.');return;}if(sourceId===destinationId){message('El banco de destino debe ser distinto al de origen.');return;}if(!Number.isFinite(amount)||amount<=0){message('Ingresa un monto mayor que cero.');return;}if(!soloAuditoria){const bank=data.fondosBanco.find(b=>b.idBanco===sourceId),available=bank?(f.get('fondo')==='Capital'?bank.capital:bank.ganancia):0;if(amount-available>0.001){message(`El máximo disponible para ${f.get('fondo')} en ${src.nombre} es ${currency.format(available)}.`);return;}}const confirmed = await FinanceDialogs.confirm({
  title: soloAuditoria ? "Guardar transferencia en historial" : "Confirmar transferencia",
  details: [
    { label: "Monto", value: currency.format(amount) },
    { label: "Fondo", value: String(f.get("fondo")) },
    { label: "Origen", value: src.nombre },
    { label: "Destino", value: dst.nombre },
    { label: "Comprobante", value: String(f.get("comprobante")).trim() }
  ],
  note: soloAuditoria ? "La transferencia ya está reflejada en la conciliación. Solo se guardará evidencia y los saldos no cambiarán." : "El monto se descontará del banco de origen y se sumará al banco de destino.",
  confirmText: soloAuditoria ? "Guardar en historial" : "Registrar transferencia"
});
if (!confirmed) return;$('#saveTransfer').disabled=true;$('#saveTransferAudit').disabled=true;try{await api('/api/finanzas/operaciones/transferencias',{method:'POST',body:JSON.stringify({idBancoOrigen:sourceId,idBancoDestino:destinationId,fondo:f.get('fondo'),monto:amount,fecha:f.get('fecha'),comprobante:String(f.get('comprobante')).trim(),soloAuditoria})});message(soloAuditoria?'Transferencia guardada en el historial; los saldos conciliados no cambiaron.':'Transferencia registrada; saldo descontado del origen y sumado al destino.',true);formElement.reset();formElement.querySelector('[name=fecha]').value=today();await refresh();}catch(x){message(x.message);}finally{$('#saveTransfer').disabled=false;$('#saveTransferAudit').disabled=false;}});
  $('#addBankButton').addEventListener('click', async () => {
    const values = await FinanceDialogs.form({
      eyebrow: "CUENTAS BANCARIAS",
      title: "Agregar banco",
      message: "Registra el saldo inicial real desglosado por fondo.",
      fields: [
        { name: "nombre", label: "Nombre de la cuenta", placeholder: "Ej. Banrural", required: true },
        { name: "capital", label: "Saldo inicial de Capital", type: "number", inputmode: "decimal", min: 0, step: "0.01", value: "0.00", required: true },
        { name: "ganancia", label: "Saldo inicial de Ganancia", type: "number", inputmode: "decimal", min: 0, step: "0.01", value: "0.00", required: true }
      ],
      note: "Si ya conciliaste los saldos, registra aquí únicamente el monto real que corresponda a cada fondo.",
      confirmText: "Guardar banco"
    });
    if (!values) return;
    const saldoCapitalInicial = Number(values.capital), saldoGananciaInicial = Number(values.ganancia);
    if (!Number.isFinite(saldoCapitalInicial) || !Number.isFinite(saldoGananciaInicial) || saldoCapitalInicial < 0 || saldoGananciaInicial < 0) {
      message("Los saldos iniciales no son válidos.");
      return;
    }
    try {
      await api('/api/finanzas/operaciones/bancos', { method: 'POST', body: JSON.stringify({ nombre: values.nombre.trim(), saldoCapitalInicial, saldoGananciaInicial }) });
      message('Cuenta bancaria registrada con saldo inicial desglosado.', true);
      await refresh();
    } catch (e) { message(e.message); }
  });
  (async()=>{for(const id of ['#depositForm [name=fecha]','#transferForm [name=fecha]'])$(id).value=today();try{const s=await api('/api/productos/sesion');if(s.autenticado){showLogin(true,s.nombre);await refresh();scrollToRequestedSection();}else showLogin(false);}catch(e){showLogin(false);$('#loginMessage').textContent=e.message;}})();
})();
