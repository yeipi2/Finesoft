window.downloadFile = function (fileName, base64Content) {
    // Detectar el tipo MIME desde el contenido
    let mimeType = 'application/octet-stream';

    // Si es PDF
    if (fileName.toLowerCase().endsWith('.pdf')) {
        mimeType = 'application/pdf';
    }
    // Si es HTML
    else if (fileName.toLowerCase().endsWith('.html')) {
        mimeType = 'text/html';
    }

    // Crear el enlace de descarga
    const link = document.createElement('a');
    link.download = fileName;
    link.href = `data:${mimeType};base64,${base64Content}`;

    // Agregar al DOM, hacer clic y remover
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};