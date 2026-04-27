// E.6.a — Helper para forzar descarga de un blob desde Blazor Server.
// Uso desde C#:
//   await JS.InvokeVoidAsync("sgrDownload.fromBase64", "survey.csv", "text/csv", base64);

window.sgrDownload = {
    fromBase64(filename, mimeType, base64) {
        const byteChars = atob(base64);
        const byteNumbers = new Array(byteChars.length);
        for (let i = 0; i < byteChars.length; i++) byteNumbers[i] = byteChars.charCodeAt(i);
        const blob = new Blob([new Uint8Array(byteNumbers)], { type: mimeType });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        // Liberamos el ObjectURL después de un tick para permitir que el browser inicie la descarga.
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    },
};
