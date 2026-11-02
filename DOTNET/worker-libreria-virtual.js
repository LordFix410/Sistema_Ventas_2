const STATUS_PATH = "/__worker_status";
const START_PAGE = "/index.html";
const ASSET_LINKS_PATH = "/.well-known/assetlinks.json";
const ASSET_LINKS = [
  {
    relation: ["delegate_permission/common.handle_all_urls"],
    target: {
      namespace: "android_app",
      package_name: "dev.workers.abdielcarcuz.libreria_virtual.twa",
      sha256_cert_fingerprints: [
        "9E:C1:23:1B:35:C2:AD:90:E0:F4:AD:AD:99:E6:DC:06:65:26:2A:C6:DC:FB:CE:F6:A9:46:38:03:3F:D9:95:F6"
      ]
    }
  }
];

function plainJson(value, status = 200) {
  return new Response(JSON.stringify(value), {
    status,
    headers: {
      "Content-Type": "application/json; charset=UTF-8",
      "Cache-Control": "no-store, no-cache, must-revalidate"
    }
  });
}

function maintenancePage(status = 503) {
  return new Response(`<!doctype html>
<html lang="es"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1,viewport-fit=cover">
<meta name="theme-color" content="#0d2f47"><link rel="icon" href="/favicon.png" type="image/png"><link rel="manifest" href="/manifest.webmanifest">
<title>Servidor en mantenimiento · Librería Digital</title>
<style>
*{box-sizing:border-box}body{margin:0;min-height:100vh;display:grid;place-items:center;padding:20px;background:#edf3f7;color:#183246;font:16px system-ui,Segoe UI,Arial,sans-serif}.card{width:min(100%,440px);padding:28px 22px;border:1px solid #d8e2e9;border-radius:18px;background:#fff;text-align:center;box-shadow:0 10px 35px #173b5515}.logo{width:70px;height:70px;object-fit:contain}.card h1{margin:14px 0 8px;font-size:1.45rem;color:#123b59}.card p{margin:0 0 20px;color:#5c7181;line-height:1.5}.retry{width:100%;min-height:48px;border:0;border-radius:10px;background:#1769aa;color:#fff;font:inherit;font-weight:750;cursor:pointer}.retry:active{transform:scale(.99)}
</style></head><body><main class="card"><img class="logo" src="/favicon.png" alt=""><h1>Servidor en mantenimiento</h1><p>En este momento no podemos conectar con Librería Digital. Inténtalo de nuevo en unos minutos.</p><button class="retry" onclick="location.replace('/')">Volver a intentar</button></main></body></html>`, {
    status,
    headers: {
      "Content-Type": "text/html; charset=UTF-8",
      "Cache-Control": "no-store, no-cache, must-revalidate"
    }
  });
}

function loadingPage() {
  return new Response(`<!doctype html>
<html lang="es"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1,viewport-fit=cover">
<meta name="theme-color" content="#0d2f47"><link rel="icon" href="/favicon.png" type="image/png"><link rel="manifest" href="/manifest.webmanifest">
<title>Librería Digital</title>
<style>
*{box-sizing:border-box}body{margin:0;min-height:100vh;display:grid;place-items:center;padding:22px;background:#edf3f7;color:#183246;font:16px system-ui,Segoe UI,Arial,sans-serif}.card{width:min(100%,440px);padding:28px 22px;border:1px solid #d8e2e9;border-radius:18px;background:#fff;text-align:center;box-shadow:0 10px 35px #173b5515}.logo{width:72px;height:72px;object-fit:contain}.brand{margin:12px 0 26px;color:#123b59;font-size:1.35rem;font-weight:800}.message{min-height:24px;margin-bottom:12px;color:#5c7181}.track{height:14px;overflow:hidden;border-radius:99px;background:#e3edf3}.bar{height:100%;width:1%;border-radius:inherit;background:#1769aa;transition:width .12s linear}.percent{display:block;margin-top:10px;color:#1769aa;font-size:1.05rem;font-weight:800}.maintenance{display:none}.retry{width:100%;min-height:48px;margin-top:7px;border:0;border-radius:10px;background:#1769aa;color:#fff;font:inherit;font-weight:750;cursor:pointer}.card.offline .loading{display:none}.card.offline .maintenance{display:block}.maintenance h1{margin:0 0 8px;font-size:1.4rem;color:#123b59}.maintenance p{margin:0 0 15px;color:#5c7181;line-height:1.5}
</style></head><body><main id="card" class="card">
<div class="loading"><img class="logo" src="/favicon.png" alt=""><div class="brand">Librería Digital</div><div id="message" class="message" role="status">Conectando con el servidor…</div><div class="track" role="progressbar" aria-label="Conectando" aria-valuemin="1" aria-valuemax="100" aria-valuenow="1"><div id="bar" class="bar"></div></div><output id="percent" class="percent">1%</output></div>
<section class="maintenance"><img class="logo" src="/favicon.png" alt=""><h1>Servidor en mantenimiento</h1><p>No pudimos conectar con Librería Digital. Inténtalo de nuevo en unos minutos.</p><button class="retry" onclick="location.reload()">Volver a intentar</button></section>
</main><script>
const card=document.getElementById('card'),bar=document.getElementById('bar'),percent=document.getElementById('percent'),message=document.getElementById('message'),progress=document.querySelector('[role="progressbar"]');
const started=Date.now();let amount=1,done=false;
const paint=()=>{bar.style.width=amount+'%';percent.value=amount+'%';percent.textContent=amount+'%';progress.setAttribute('aria-valuenow',String(amount));};
const ticker=setInterval(()=>{if(amount<94){amount+=1;paint();}},45);
async function check(){if(done)return;try{const response=await fetch('${STATUS_PATH}',{cache:'no-store',headers:{'Accept':'application/json'}});const data=await response.json();if(response.ok&&data.ready){done=true;clearInterval(ticker);message.textContent='Servidor disponible. Abriendo…';const finish=setInterval(()=>{amount=Math.min(100,amount+2);paint();if(amount===100){clearInterval(finish);location.replace('${START_PAGE}');}},25);return;}}catch{}
if(Date.now()-started>15000){done=true;clearInterval(ticker);card.classList.add('offline');return;}message.textContent='Verificando disponibilidad…';setTimeout(check,900);}
paint();check();
</script></body></html>`, {
    headers: {
      "Content-Type": "text/html; charset=UTF-8",
      "Cache-Control": "no-store, no-cache, must-revalidate"
    }
  });
}

async function getTunnelOrigin(env) {
  const stored = await env.Venta_binding.get("url");
  if (!stored) return null;

  let origin;
  try {
    origin = new URL(stored);
  } catch {
    return null;
  }

  if (origin.protocol !== "https:" || !origin.hostname.endsWith(".trycloudflare.com")) return null;
  origin.pathname = "/";
  origin.search = "";
  origin.hash = "";
  return origin;
}

async function isServerReady(origin) {
  if (!origin) return false;
  try {
    const response = await fetch(new Request(origin.toString(), {
      method: "GET",
      headers: { "Cache-Control": "no-cache", "Pragma": "no-cache" }
    }), { signal: AbortSignal.timeout(3500) });
    const ready = response.ok;
    if (response.body) await response.body.cancel().catch(() => {});
    return ready;
  } catch {
    return false;
  }
}

export default {
  async fetch(request, env) {
    const incoming = new URL(request.url);

    // Android Digital Asset Links debe responder aquí directamente desde el dominio del Worker.
    if (incoming.pathname === ASSET_LINKS_PATH) {
      const headers = {
        "Content-Type": "application/json; charset=UTF-8",
        "Cache-Control": "no-store, no-cache, must-revalidate",
        "X-Content-Type-Options": "nosniff"
      };
      if (request.method === "GET") {
        return new Response(JSON.stringify(ASSET_LINKS), { status: 200, headers });
      }
      if (request.method === "HEAD") {
        return new Response(null, { status: 200, headers });
      }
      return new Response("Method Not Allowed", {
        status: 405,
        headers: { ...headers, Allow: "GET, HEAD" }
      });
    }

    if (request.method === "GET" && incoming.pathname === "/") {
      return loadingPage();
    }

    if (request.method === "GET" && incoming.pathname === STATUS_PATH) {
      let origin = null;
      try { origin = await getTunnelOrigin(env); } catch {}
      return plainJson({ ready: await isServerReady(origin) }, origin ? 200 : 503);
    }

    let origin;
    try {
      origin = await getTunnelOrigin(env);
    } catch {
      return maintenancePage();
    }
    if (!origin) return maintenancePage();

    const destination = new URL(origin);
    destination.pathname = incoming.pathname;
    destination.search = incoming.search;

    try {
      const upstream = await fetch(new Request(destination.toString(), request));
      const headers = new Headers(upstream.headers);
      headers.set("Cache-Control", "no-store");

      const location = headers.get("Location");
      if (location) {
        try {
          const redirect = new URL(location, origin);
          if (redirect.hostname === origin.hostname) {
            redirect.protocol = incoming.protocol;
            redirect.host = incoming.host;
            headers.set("Location", redirect.toString());
          }
        } catch {}
      }

      return new Response(upstream.body, {
        status: upstream.status,
        statusText: upstream.statusText,
        headers
      });
    } catch {
      return maintenancePage(502);
    }
  }
};
