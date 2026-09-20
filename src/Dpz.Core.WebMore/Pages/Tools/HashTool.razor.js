export function setupFileDropzone(dotNetHelper, dropzoneElement) {
    if (!dropzoneElement || !dotNetHelper) return;

    const arrayBufferToBase64 = (buffer) => {
        let binary = '';
        const bytes = new Uint8Array(buffer);
        const len = bytes.byteLength;
        for (let i = 0; i < len; i++) {
            binary += String.fromCharCode(bytes[i]);
        }
        return btoa(binary);
    };

    const handleFiles = async (files) => {
        if (files && files.length > 0) {
            const file = files[0];
            await dotNetHelper.invokeMethodAsync('HandleDroppedFile', {
                name: file.name,
                size: file.size,
                type: file.type,
                lastModified: file.lastModified
            });
            
            // Read file as stream for hash computation
            const arrayBuffer = await file.arrayBuffer();
            const uint8Array = new Uint8Array(arrayBuffer);
            
            // Send file data in chunks to avoid memory issues
            const chunkSize = 1024 * 1024; // 1MB chunks
            const totalChunks = Math.ceil(uint8Array.length / chunkSize);
            
            for (let i = 0; i < totalChunks; i++) {
                const start = i * chunkSize;
                const end = Math.min(start + chunkSize, uint8Array.length);
                const chunk = uint8Array.slice(start, end);
                const base64Chunk = arrayBufferToBase64(chunk);
                await dotNetHelper.invokeMethodAsync('ProcessFileChunk', base64Chunk, i === totalChunks - 1);
            }
        }
    };

    dropzoneElement.addEventListener('drop', async (e) => {
        e.preventDefault();
        e.stopPropagation();
        const files = e.dataTransfer.files;
        await handleFiles(files);
    });

    dropzoneElement.addEventListener('dragover', (e) => {
        e.preventDefault();
        e.stopPropagation();
    });
}

export function cleanupFileDropzone(dropzoneElement) {
    if (dropzoneElement) {
        dropzoneElement.replaceWith(dropzoneElement.cloneNode(true));
    }
}
