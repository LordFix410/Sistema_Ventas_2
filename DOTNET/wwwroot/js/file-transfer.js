(function () {
  const databaseName = 'libreria-digital-file-transfer';
  function openDatabase() {
    return new Promise((resolve, reject) => {
      const request = indexedDB.open(databaseName, 1);
      request.onupgradeneeded = () => request.result.createObjectStore('transfers', {keyPath: 'id'});
      request.onsuccess = () => resolve(request.result);
      request.onerror = () => reject(request.error || new Error('No se pudo preparar la carga de archivos.'));
    });
  }
  async function save(files, target) {
    const database = await openDatabase();
    const id = `${Date.now()}-${Math.random().toString(36).slice(2)}`;
    await new Promise((resolve, reject) => {
      const transaction = database.transaction('transfers', 'readwrite');
      transaction.objectStore('transfers').put({id, files: [...files]});
      transaction.oncomplete = resolve;
      transaction.onerror = () => reject(transaction.error || new Error('No se pudieron conservar los archivos.'));
      transaction.onabort = () => reject(transaction.error || new Error('No se pudieron conservar los archivos.'));
    });
    database.close();
    return `${target}?transfer=${encodeURIComponent(id)}`;
  }
  async function take() {
    const url = new URL(location.href);
    const id = url.searchParams.get('transfer');
    if (!id) return [];
    url.searchParams.delete('transfer');
    history.replaceState(null, '', url);
    const database = await openDatabase();
    const record = await new Promise((resolve, reject) => {
      const transaction = database.transaction('transfers', 'readwrite');
      const store = transaction.objectStore('transfers');
      const request = store.get(id);
      request.onsuccess = () => {
        const result = request.result;
        store.delete(id);
        resolve(result);
      };
      request.onerror = () => reject(request.error || new Error('No se pudieron recuperar los archivos.'));
    });
    database.close();
    return record?.files || [];
  }
  async function unpackArchive(file) {
    const extension = file.name.toLowerCase().split('.').pop();
    if (extension !== 'zip' && extension !== 'rar') throw new Error(`Formato no compatible: ${file.name}`);
    const form = new FormData();
    form.append('file', file);
    const response = await fetch(`/api/images/unpack-${extension}`, {method: 'POST', body: form});
    const result = await response.json();
    if (!response.ok) throw new Error(result.message || `No se pudo abrir ${file.name}.`);
    return (result.images || []).map(item => {
      const binary = atob(item.data);
      const bytes = new Uint8Array(binary.length);
      for (let index = 0; index < binary.length; index++) bytes[index] = binary.charCodeAt(index);
      return new File([bytes], item.name, {type: item.mimeType});
    });
  }
  window.LocalFileTransfer = {save, take, unpackArchive};
})();
