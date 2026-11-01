const toolCards = [...document.querySelectorAll('.tool-card[data-tool]')];
let activeCard = null;
let dragDepth = 0;

function isFileDrag(event) {
  return [...(event.dataTransfer?.types || [])].includes('Files');
}
function setActiveCard(card) {
  if (activeCard === card) return;
  activeCard?.classList.remove('drag-active');
  activeCard = card;
  activeCard?.classList.add('drag-active');
}
function setMessage(card, message) {
  let status = card.querySelector('.tool-drop-status');
  if (!status) {
    status = document.createElement('p');
    status.className = 'tool-drop-status';
    card.append(status);
  }
  status.textContent = message;
}
async function sendFiles(card, files) {
  const accepted = [...files].filter(file => file.type.startsWith('image/') || /\.(zip|rar)$/i.test(file.name));
  if (!accepted.length) {
    setMessage(card, 'Soltá imágenes, ZIP o RAR.');
    return;
  }
  setMessage(card, 'Cargando archivos…');
  try {
    const target = `${card.dataset.tool}.html`;
    location.href = await window.LocalFileTransfer.save(accepted, target);
  } catch (error) {
    setMessage(card, error.message || 'No se pudieron preparar los archivos.');
  }
}

document.addEventListener('dragenter', event => {
  if (!isFileDrag(event)) return;
  event.preventDefault();
  dragDepth++;
  const card = event.target.closest?.('.tool-card[data-tool]');
  if (card) setActiveCard(card);
});
document.addEventListener('dragover', event => {
  if (!isFileDrag(event)) return;
  event.preventDefault();
  event.dataTransfer.dropEffect = 'copy';
  setActiveCard(event.target.closest?.('.tool-card[data-tool]') || null);
});
document.addEventListener('dragleave', event => {
  if (!isFileDrag(event)) return;
  if (event.target === document.documentElement || event.target === document.body) dragDepth = Math.max(0, dragDepth - 1);
  if (!dragDepth) setActiveCard(null);
});
document.addEventListener('drop', event => {
  if (!isFileDrag(event)) return;
  event.preventDefault();
  dragDepth = 0;
  const card = event.target.closest?.('.tool-card[data-tool]') || activeCard;
  setActiveCard(null);
  if (card) sendFiles(card, event.dataTransfer.files);
});

for (const card of toolCards) {
  card.addEventListener('dragover', event => event.preventDefault());
  card.addEventListener('drop', event => {
    event.preventDefault();
    event.stopPropagation();
    sendFiles(card, event.dataTransfer.files);
  });
}
