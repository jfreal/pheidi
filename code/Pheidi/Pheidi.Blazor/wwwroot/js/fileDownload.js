export function downloadFile(base64, mimeType, fileName) {
    const a = document.createElement('a');
    a.href = `data:${mimeType};base64,${base64}`;
    a.download = fileName;
    a.click();
}
